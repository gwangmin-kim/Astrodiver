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

    public GameSaveData Data { get; private set; }
    public PlayerStatsSaveData PlayerStats { get; private set; }
    public EquipmentSaveData Equipment { get; private set; }
    public InventoryRuntimeData Inventory { get; private set; }
    public UpgradeRuntimeData RuntimeData { get; private set; }
    public GameDefinitionRegistry Definitions { get; private set; }
    public UpgradeService Upgrades { get; private set; }
    public bool HasUnsavedChanges => _isDirty;
    public string SaveFilePath => System.IO.Path.Combine(
        Application.persistentDataPath,
        SaveFileName);

    public event Action<GameSaveData> DataLoaded;
    public event Action<GameSaveData> DataSaved;
    public event Action<GameSaveData> DataChanged;
    public event Action<UpgradeRuntimeData> RuntimeDataChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_definitionCatalog == null)
        {
            Debug.LogError("GameDefinitionCatalog is not assigned.", this);
            enabled = false;
            return;
        }

        Definitions = new GameDefinitionRegistry(_definitionCatalog);
        Upgrades = new UpgradeService(this);
        Load();
    }

    public void Load()
    {
        if (!GameDataFileStore.TryLoad(SaveFilePath, out GameSaveData loadedData, out string error))
        {
            loadedData = _defaults != null ? _defaults.CreateSaveData() : new GameSaveData();

            if (!string.Equals(error, "Save file does not exist.", StringComparison.Ordinal))
            {
                Debug.LogWarning($"Could not load game data. Defaults will be used. {error}", this);
            }
        }

        loadedData.RepairAfterLoad();
        _isDirty = false;
        Data = loadedData;
        if (!RebuildRuntimeData(out string rebuildError))
        {
            Debug.LogError($"Could not rebuild runtime stats. {rebuildError}", this);
        }

        DataChanged?.Invoke(Data);
        DataLoaded?.Invoke(Data);
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
        _isSaveSuspended = false;
    }

    private bool SaveNowCore()
    {
        if (Data == null)
        {
            return false;
        }

        if (!GameDataFileStore.TrySave(SaveFilePath, Data, out string error))
        {
            Debug.LogError($"Could not save game data. {error}", this);
            return false;
        }

        _isDirty = false;
        DataSaved?.Invoke(Data);
        return true;
    }

    public void MarkDirty()
    {
        _isDirty = true;
    }

    public PlayerMovementData GetOrInitializeMovement(PlayerMovementData fallback)
    {
        EnsureRuntimeData();
        if (!PlayerStats.movementInitialized)
        {
            PlayerStats.movement = fallback;
            PlayerStats.movementInitialized = true;
        }

        return PlayerStats.movement;
    }

    public void SetMovement(PlayerMovementData value)
    {
        EnsureRuntimeData();
        PlayerStats.movement = value;
        PlayerStats.movementInitialized = true;
    }

    public BatteryData GetOrInitializeBattery(BatteryData fallback)
    {
        EnsureRuntimeData();
        if (!PlayerStats.batteryInitialized)
        {
            PlayerStats.battery = fallback;
            PlayerStats.batteryInitialized = true;
        }

        return PlayerStats.battery;
    }

    public void SetBattery(BatteryData value)
    {
        EnsureRuntimeData();
        PlayerStats.battery = value;
        PlayerStats.batteryInitialized = true;
    }

    public MagnetData GetOrInitializeMagnet(MagnetData fallback)
    {
        EnsureRuntimeData();
        if (!PlayerStats.magnetInitialized)
        {
            PlayerStats.magnet = fallback;
            PlayerStats.magnetInitialized = true;
        }

        return PlayerStats.magnet;
    }

    public void SetMagnet(MagnetData value)
    {
        EnsureRuntimeData();
        PlayerStats.magnet = value;
        PlayerStats.magnetInitialized = true;
    }

    public NetGunData GetOrInitializeNetGun(NetGunData fallback)
    {
        EnsureRuntimeData();
        if (!Equipment.netGunInitialized)
        {
            Equipment.netGun = fallback;
            Equipment.netGunInitialized = true;
        }

        return Equipment.netGun;
    }

    public void SetNetGun(NetGunData value)
    {
        EnsureRuntimeData();
        Equipment.netGun = value;
        Equipment.netGunInitialized = true;
    }

    public PlasmaGunData GetOrInitializePlasmaGun(PlasmaGunData fallback)
    {
        EnsureRuntimeData();
        if (!Equipment.plasmaGunInitialized)
        {
            Equipment.plasmaGun = fallback;
            Equipment.plasmaGunInitialized = true;
        }

        return Equipment.plasmaGun;
    }

    public void SetPlasmaGun(PlasmaGunData value)
    {
        EnsureRuntimeData();
        Equipment.plasmaGun = value;
        Equipment.plasmaGunInitialized = true;
    }

    public int GetUpgradeLevel(string upgradeId)
    {
        string normalizedId = upgradeId?.Trim();
        if (string.IsNullOrEmpty(normalizedId))
        {
            return 0;
        }

        for (int i = 0; i < Data.upgradeNodes.Count; i++)
        {
            UpgradeNodeSaveData entry = Data.upgradeNodes[i];
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
        for (int i = 0; i < Data.upgradeNodes.Count; i++)
        {
            if (!string.Equals(Data.upgradeNodes[i].nodeId, normalizedId, StringComparison.Ordinal))
            {
                continue;
            }

            if (normalizedLevel == 0)
            {
                Data.upgradeNodes.RemoveAt(i);
            }
            else
            {
                Data.upgradeNodes[i] = new UpgradeNodeSaveData
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
            Data.upgradeNodes.Add(new UpgradeNodeSaveData
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

    public bool CompleteEvent(string eventId)
    {
        return AddUniqueProgressId(Data.completedEventIds, eventId);
    }

    public bool IsEventCompleted(string eventId)
    {
        return ContainsProgressId(Data.completedEventIds, eventId);
    }

    private bool AddUniqueProgressId(
        System.Collections.Generic.List<string> ids,
        string id)
    {
        string normalizedId = id?.Trim();
        if (string.IsNullOrEmpty(normalizedId) || ids.Contains(normalizedId))
        {
            return false;
        }

        ids.Add(normalizedId);
        ids.Sort(StringComparer.Ordinal);
        MarkDirty();
        return true;
    }

    private static bool ContainsProgressId(
        System.Collections.Generic.List<string> ids,
        string id)
    {
        string normalizedId = id?.Trim();
        return !string.IsNullOrEmpty(normalizedId) && ids.Contains(normalizedId);
    }

    private void ReplaceData(GameSaveData data)
    {
        Data = data ?? new GameSaveData();
        if (!RebuildRuntimeData(out string error))
        {
            Debug.LogError($"Could not rebuild runtime stats. {error}", this);
        }

        DataChanged?.Invoke(Data);
    }

    internal bool RebuildRuntimeData(out string error)
    {
        PlayerStatsSaveData rebuiltPlayerStats = _defaults != null
            ? _defaults.CreatePlayerStats()
            : new PlayerStatsSaveData();
        EquipmentSaveData rebuiltEquipment = _defaults != null
            ? _defaults.CreateEquipment()
            : new EquipmentSaveData();
        InventoryRuntimeData rebuiltInventory = _defaults != null
            ? _defaults.CreateInventory()
            : new InventoryRuntimeData();
        UpgradeRuntimeData rebuiltRuntimeData =
            new(rebuiltPlayerStats, rebuiltEquipment, rebuiltInventory);

        if (Data == null)
        {
            error = "Game save data is null.";
            return false;
        }

        for (int definitionIndex = 0;
             definitionIndex < _definitionCatalog.Upgrades.Count;
             definitionIndex++)
        {
            UpgradeNodeDefinition node = _definitionCatalog.Upgrades[definitionIndex];
            if (node == null)
            {
                continue;
            }

            int savedLevel = GetUpgradeLevel(node.Id);
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
                        effect.TryApply(rebuiltRuntimeData, out effectError))
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

        PlayerStats = rebuiltPlayerStats;
        Equipment = rebuiltEquipment;
        Inventory = rebuiltInventory;
        RuntimeData = rebuiltRuntimeData;
        RuntimeDataChanged?.Invoke(RuntimeData);
        error = null;
        return true;
    }

    private void EnsureRuntimeData()
    {
        if (RuntimeData != null)
        {
            return;
        }

        PlayerStats ??= new PlayerStatsSaveData();
        Equipment ??= new EquipmentSaveData();
        Inventory ??= new InventoryRuntimeData();
        RuntimeData = new UpgradeRuntimeData(PlayerStats, Equipment, Inventory);
    }

}
