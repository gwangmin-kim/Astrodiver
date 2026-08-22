using System;
using Unity.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [Header("Definitions")]
    [SerializeField] private GameDefinitionCatalog _definitionCatalog;

    [SerializeField][ReadOnly] private GameDataDefaults _defaults;
    private const string SaveFileName = "player-save.json";

    private bool _isDirty;
    private bool _isSaveSuspended;

    private sealed class PreparedGameData
    {
        public GameSaveData saveData;
        public GameRuntimeData runtimeData;
        public bool hasDerivedChanges;
    }

    public GameSaveData SaveData { get; private set; }
    public GameRuntimeData RuntimeData { get; private set; }
    public GameDefinitionRegistry Definitions { get; private set; }
    public UpgradeService Upgrades { get; private set; }
    public bool IsInitialized => SaveData != null && RuntimeData != null;
    public bool HasUnsavedChanges => _isDirty;
    public bool IsSaveSuspended => _isSaveSuspended;
    public string SaveFilePath => System.IO.Path.Combine(
        Application.persistentDataPath,
        SaveFileName);
    public bool HasSaveData => System.IO.File.Exists(SaveFilePath) ||
        System.IO.File.Exists(SaveFilePath + ".bak");

    public event Action<GameSaveData> DataLoaded;
    public event Action<GameSaveData> DataSaved;
    public event Action<GameSaveData> DataChanged;
    public event Action<GameRuntimeData> RuntimeDataChanged;
    /// <summary>Raised once when an event is first added to the saved progress history.</summary>
    public event Action<GameProgressEventId> ProgressEventCompleted;
    public event Action SaveSuspensionEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_definitionCatalog == null || _defaults == null)
        {
            Debug.LogError("GameDefinitionCatalog and GameDataDefaults must both be assigned.", this);
            enabled = false;
            return;
        }

        Definitions = new GameDefinitionRegistry(_definitionCatalog);
        Upgrades = new UpgradeService(this);

#if UNITY_EDITOR
        InitializeForEditorScenePlay();
#endif
    }

#if UNITY_EDITOR
    private void InitializeForEditorScenePlay()
    {
        if (IsInitialized)
        {
            return;
        }

        if (HasSaveData)
        {
            if (TryLoadSavedGame(out string loadError))
            {
                return;
            }

            Debug.LogWarning(
                $"Editor scene-start save load failed. Using default in-memory data. {loadError}",
                this);
        }

        GameSaveData defaultData = _defaults.CreateSaveData();
        if (!TryPrepareData(
                defaultData,
                out PreparedGameData prepared,
                out string prepareError))
        {
            Debug.LogError(
                $"Could not initialize editor scene-start data. {prepareError}",
                this);
            return;
        }

        CommitPreparedData(prepared, false, false);
    }
#endif

    public bool TryStartNewGame(out string error)
    {
        if (!CanInitializeGameData(out error))
        {
            return false;
        }

        GameSaveData candidate = _defaults.CreateSaveData();

        if (!TryPrepareData(candidate, out PreparedGameData prepared, out error))
        {
            return false;
        }

        if (!GameDataFileStore.TrySaveNewGame(SaveFilePath, prepared.saveData, out error))
        {
            return false;
        }

        CommitPreparedData(prepared, false, true);
        error = null;
        return true;
    }

    public bool TryLoadSavedGame(out string error)
    {
        if (!CanInitializeGameData(out error))
        {
            return false;
        }

        if (!GameDataFileStore.TryLoad(
                SaveFilePath,
                out GameSaveData loadedData,
                out error))
        {
            return false;
        }

        if (!TryPrepareData(loadedData, out PreparedGameData prepared, out error))
        {
            return false;
        }

        CommitPreparedData(prepared, prepared.hasDerivedChanges, true);
        error = null;
        return true;
    }

    private bool CanInitializeGameData(out string error)
    {
        if (!enabled || _definitionCatalog == null || _defaults == null ||
            Definitions == null || Upgrades == null)
        {
            error = "Game data services are not configured.";
            return false;
        }

        if (_isSaveSuspended)
        {
            error = "Cannot replace game data while an exploration session is active.";
            return false;
        }

        error = null;
        return true;
    }

    public bool SaveNow()
    {
        if (_isSaveSuspended)
        {
            Debug.LogWarning("Saving is suspended until the exploration session ends.", this);
            return false;
        }

        return SaveNowCore();
    }

    internal bool SaveExplorationResult()
    {
        return SaveNowCore();
    }

    internal bool BeginSaveSuspension()
    {
        if (_isSaveSuspended)
        {
            return false;
        }

        _isSaveSuspended = true;
        return true;
    }

    internal void EndSaveSuspension()
    {
        if (!_isSaveSuspended)
        {
            return;
        }

        _isSaveSuspended = false;
        SaveSuspensionEnded?.Invoke();
    }

    private bool SaveNowCore()
    {
        if (SaveData == null)
        {
            return false;
        }

        if (!GameDataFileStore.TrySave(SaveFilePath, SaveData, out string error))
        {
            Debug.LogError($"Could not save game data. {error}", this);
            return false;
        }

        _isDirty = false;
        DataSaved?.Invoke(SaveData);
        return true;
    }

    public void MarkDirty()
    {
        _isDirty = true;
    }

    public PlayerMovementData GetMovement()
    {
        return RequireRuntimeData().PlayerStats.movement;
    }

    public BatteryData GetBattery()
    {
        return RequireRuntimeData().PlayerStats.battery;
    }

    public MagnetData GetMagnet()
    {
        return RequireRuntimeData().PlayerStats.magnet;
    }

    public NetGunData GetNetGun()
    {
        return RequireRuntimeData().Equipment.netGun;
    }

    public PlasmaGunData GetPlasmaGun()
    {
        return RequireRuntimeData().Equipment.plasmaGun;
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        return GetUpgradeLevel(SaveData, upgradeId);
    }

    private static int GetUpgradeLevel(GameSaveData data, string upgradeId)
    {
        string normalizedId = upgradeId?.Trim();
        if (data == null || string.IsNullOrEmpty(normalizedId))
        {
            return 0;
        }

        for (int i = 0; i < data.upgradeNodes.Count; i++)
        {
            UpgradeNodeSaveData entry = data.upgradeNodes[i];
            if (string.Equals(entry.nodeId, normalizedId, StringComparison.Ordinal))
            {
                return Mathf.Max(0, entry.level);
            }
        }

        return 0;
    }

    public bool IsUpgradeUnlocked(string upgradeId)
    {
        return GetUpgradeLevel(upgradeId) > 0;
    }

    internal void SetUpgradeLevel(string upgradeId, int level)
    {
        string normalizedId = upgradeId?.Trim();
        if (string.IsNullOrEmpty(normalizedId))
        {
            return;
        }

        int normalizedLevel = Mathf.Max(0, level);
        for (int i = 0; i < SaveData.upgradeNodes.Count; i++)
        {
            if (!string.Equals(SaveData.upgradeNodes[i].nodeId, normalizedId, StringComparison.Ordinal))
            {
                continue;
            }

            if (normalizedLevel == 0)
            {
                SaveData.upgradeNodes.RemoveAt(i);
            }
            else
            {
                SaveData.upgradeNodes[i] = new UpgradeNodeSaveData
                {
                    nodeId = normalizedId,
                    level = normalizedLevel
                };
            }

            MarkDirty();
            return;
        }

        if (normalizedLevel > 0)
        {
            SaveData.upgradeNodes.Add(new UpgradeNodeSaveData
            {
                nodeId = normalizedId,
                level = normalizedLevel
            });
            MarkDirty();
        }
    }

    internal void RestoreTransactionSnapshot(GameSaveData snapshot, bool wasDirty)
    {
        _isDirty = wasDirty;
        ReplaceData(snapshot);
    }

    internal void RestoreDirtyState(bool wasDirty)
    {
        _isDirty = wasDirty;
    }

    public bool CompleteEvent(GameProgressEventId eventId)
    {
        if (!TryCompleteEvent(SaveData, eventId))
        {
            return false;
        }

        MarkDirty();
        ProgressEventCompleted?.Invoke(eventId);
        return true;
    }

    private static bool TryCompleteEvent(GameSaveData data, GameProgressEventId eventId)
    {
        if (data == null ||
            eventId == GameProgressEventId.None ||
            !Enum.IsDefined(typeof(GameProgressEventId), eventId) ||
            data.completedEvents.Contains(eventId))
        {
            return false;
        }

        data.completedEvents.Add(eventId);
        return true;
    }

    public bool IsEventCompleted(GameProgressEventId eventId)
    {
        return SaveData != null &&
               eventId != GameProgressEventId.None &&
               SaveData.completedEvents.Contains(eventId);
    }

    private void ReplaceData(GameSaveData data)
    {
        if (!TryPrepareData(data ?? new GameSaveData(), out PreparedGameData prepared, out string error))
        {
            Debug.LogError($"Could not rebuild runtime stats. {error}", this);
            return;
        }

        CommitPreparedData(prepared, _isDirty, false);
    }

    internal bool RebuildRuntimeData(out string error)
    {
        if (!TryPrepareData(SaveData, out PreparedGameData prepared, out error))
        {
            return false;
        }

        RuntimeData = prepared.runtimeData;
        _isDirty |= prepared.hasDerivedChanges;
        RuntimeDataChanged?.Invoke(RuntimeData);
        return true;
    }

    /// <summary>
    /// 세이브 데이터를 불러와 런타임 데이터를 준비
    /// </summary>
    private bool TryPrepareData(
        GameSaveData data,
        out PreparedGameData prepared,
        out string error)
    {
        prepared = null;
        if (data == null)
        {
            error = "Game save data is null.";
            return false;
        }

        if (_defaults == null || _definitionCatalog == null)
        {
            error = "Game runtime data sources are not configured.";
            return false;
        }

        data.RepairAfterLoad();
        if (!data.TryValidate(out error))
        {
            error = $"Game save data validation failed: {error}";
            return false;
        }

        GameRuntimeData rebuiltRuntimeData = _defaults.CreateRuntimeData();

        bool hasDerivedChanges = false;

        UpgradeEffectContext effectContext = new(rebuiltRuntimeData);

        for (int definitionIndex = 0;
             definitionIndex < _definitionCatalog.Upgrades.Count;
             definitionIndex++)
        {
            UpgradeNodeDefinition node = _definitionCatalog.Upgrades[definitionIndex];
            if (node == null)
            {
                continue;
            }

            int savedLevel = GetUpgradeLevel(data, node.Id);
            int appliedLevel = Mathf.Min(savedLevel, node.MaxLevel);
            if (savedLevel > node.MaxLevel)
            {
                Debug.LogWarning(
                    $"Saved level {savedLevel} for upgrade '{node.Id}' exceeds its current " +
                    $"maximum level {node.MaxLevel}. Only {appliedLevel} levels will be applied.",
                    node);
            }

            for (int level = 0; level < appliedLevel; level++)
            {
                for (int effectIndex = 0; effectIndex < node.Effects.Count; effectIndex++)
                {
                    UpgradeEffect effect = node.Effects[effectIndex];
                    string effectError = null;
                    if (effect != null &&
                        effect.TryApply(effectContext, out effectError))
                    {
                        continue;
                    }

                    error = effect != null
                        ? $"Upgrade '{node.Id}' effect {effectIndex} failed: {effectError}"
                        : $"Upgrade '{node.Id}' effect {effectIndex} is null.";
                    return false;
                }
            }
        }

        hasDerivedChanges |= ReconcilePersistentInvariants(
            data,
            rebuiltRuntimeData);

        prepared = new PreparedGameData
        {
            saveData = data,
            runtimeData = rebuiltRuntimeData,
            hasDerivedChanges = hasDerivedChanges
        };
        error = null;
        return true;
    }

    private static bool ReconcilePersistentInvariants(
        GameSaveData data,
        GameRuntimeData runtimeData)
    {
        return runtimeData.Facilities.ResourceChestUnlocked &&
               data.inventory.TransferAllResourcesTo(data.resourceChest);
    }

    /// <summary>
    /// 준비된 데이터를 실제 매니저 상태에 적용
    /// </summary>
    private void CommitPreparedData(
        PreparedGameData prepared,
        bool isDirty,
        bool notifyLoaded)
    {
        SaveData = prepared.saveData;
        RuntimeData = prepared.runtimeData;
        _isSaveSuspended = false;
        _isDirty = isDirty;

        DataChanged?.Invoke(SaveData);
        RuntimeDataChanged?.Invoke(RuntimeData);
        if (notifyLoaded)
        {
            DataLoaded?.Invoke(SaveData);
        }
    }

    private GameRuntimeData RequireRuntimeData()
    {
        if (RuntimeData == null)
        {
            throw new InvalidOperationException(
                "Game data must be initialized before entering gameplay.");
        }

        return RuntimeData;
    }

}
