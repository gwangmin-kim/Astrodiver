using UnityEngine;

[CreateAssetMenu(
    fileName = "GameDataDefaults",
    menuName = "Astrodiver/Save System/Game Data Defaults")]
public sealed class GameDataDefaults : ScriptableObject
{
    [SerializeField] private GameSaveData _data = new();
    [SerializeField] private PlayerStatsRuntimeData _playerStats = new();
    [SerializeField] private EquipmentRuntimeData _equipment = new();
    [SerializeField] private InventoryRuntimeData _inventory = new();

    public GameSaveData CreateSaveData()
    {
        _data ??= new GameSaveData();
        _data.RepairAfterLoad();

        string json = JsonUtility.ToJson(_data);
        GameSaveData copy = JsonUtility.FromJson<GameSaveData>(json) ?? new GameSaveData();
        copy.RepairAfterLoad();
        return copy;
    }

    public GameRuntimeData CreateRuntimeData()
    {
        return new GameRuntimeData(
            CreatePlayerStats(),
            CreateEquipment(),
            CreateInventory());
    }

    public PlayerStatsRuntimeData CreatePlayerStats()
    {
        _playerStats ??= new PlayerStatsRuntimeData();
        return Clone(_playerStats) ?? new PlayerStatsRuntimeData();
    }

    public EquipmentRuntimeData CreateEquipment()
    {
        _equipment ??= new EquipmentRuntimeData();
        return Clone(_equipment) ?? new EquipmentRuntimeData();
    }

    public InventoryRuntimeData CreateInventory()
    {
        _inventory ??= new InventoryRuntimeData();
        return Clone(_inventory) ?? new InventoryRuntimeData();
    }

    private void OnValidate()
    {
        _data ??= new GameSaveData();
        _playerStats ??= new PlayerStatsRuntimeData();
        _equipment ??= new EquipmentRuntimeData();
        _inventory ??= new InventoryRuntimeData();
        _data.RepairAfterLoad();
    }

    private static T Clone<T>(T value)
    {
        return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    }
}
