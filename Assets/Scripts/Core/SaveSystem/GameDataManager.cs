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

    public GameSaveData Data { get; private set; }
    public GameDefinitionRegistry Definitions { get; private set; }
    public bool HasUnsavedChanges => _isDirty;
    public string SaveFilePath => System.IO.Path.Combine(
        Application.persistentDataPath,
        SaveFileName);

    public event Action<GameSaveData> DataLoaded;
    public event Action<GameSaveData> DataSaved;

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

        loadedData.Normalize();
        Data = loadedData;
        _isDirty = false;
        DataLoaded?.Invoke(Data);
    }

    public bool SaveNow()
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

    public InventorySaveData GetOrInitializeInventory(InventorySaveData fallback)
    {
        if (!Data.inventory.initialized)
        {
            Data.inventory = fallback ?? new InventorySaveData();
            Data.inventory.initialized = true;
            Data.inventory.Normalize();
            MarkDirty();
        }

        return Data.inventory;
    }

    public void SetInventory(InventorySaveData inventory)
    {
        Data.inventory = inventory ?? new InventorySaveData();
        Data.inventory.initialized = true;
        Data.inventory.Normalize();
        MarkDirty();
    }

    public PlayerMovementData GetOrInitializeMovement(PlayerMovementData fallback)
    {
        if (!Data.playerStats.movementInitialized)
        {
            Data.playerStats.movement = fallback;
            Data.playerStats.movementInitialized = true;
            MarkDirty();
        }

        return Data.playerStats.movement;
    }

    public void SetMovement(PlayerMovementData value)
    {
        Data.playerStats.movement = value;
        Data.playerStats.movementInitialized = true;
        MarkDirty();
    }

    public BatteryData GetOrInitializeBattery(BatteryData fallback)
    {
        if (!Data.playerStats.batteryInitialized)
        {
            Data.playerStats.battery = fallback;
            Data.playerStats.batteryInitialized = true;
            MarkDirty();
        }

        return Data.playerStats.battery;
    }

    public void SetBattery(BatteryData value)
    {
        Data.playerStats.battery = value;
        Data.playerStats.batteryInitialized = true;
        MarkDirty();
    }

    public NetGunData GetOrInitializeNetGun(NetGunData fallback)
    {
        if (!Data.equipment.netGunInitialized)
        {
            Data.equipment.netGun = fallback;
            Data.equipment.netGunInitialized = true;
            MarkDirty();
        }

        return Data.equipment.netGun;
    }

    public void SetNetGun(NetGunData value)
    {
        Data.equipment.netGun = value;
        Data.equipment.netGunInitialized = true;
        MarkDirty();
    }

    public PlasmaGunData GetOrInitializePlasmaGun(PlasmaGunData fallback)
    {
        if (!Data.equipment.plasmaGunInitialized)
        {
            Data.equipment.plasmaGun = fallback;
            Data.equipment.plasmaGunInitialized = true;
            MarkDirty();
        }

        return Data.equipment.plasmaGun;
    }

    public void SetPlasmaGun(PlasmaGunData value)
    {
        Data.equipment.plasmaGun = value;
        Data.equipment.plasmaGunInitialized = true;
        MarkDirty();
    }

    public bool UnlockUpgrade(string upgradeId)
    {
        return AddUniqueProgressId(Data.unlockedUpgradeIds, upgradeId);
    }

    public bool IsUpgradeUnlocked(string upgradeId)
    {
        return ContainsProgressId(Data.unlockedUpgradeIds, upgradeId);
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
}
