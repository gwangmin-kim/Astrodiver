using UnityEngine;

[CreateAssetMenu(
    fileName = "GameDataDefaults",
    menuName = "Astrodiver/Save System/Game Data Defaults")]
public sealed class GameDataDefaults : ScriptableObject
{
    [SerializeField] private GameSaveData _data = new();
    [SerializeField] private PlayerStatsSaveData _playerStats = new();
    [SerializeField] private EquipmentSaveData _equipment = new();
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

    public PlayerStatsSaveData CreatePlayerStats()
    {
        _playerStats ??= new PlayerStatsSaveData();
        return Clone(_playerStats) ?? new PlayerStatsSaveData();
    }

    public EquipmentSaveData CreateEquipment()
    {
        _equipment ??= new EquipmentSaveData();
        return Clone(_equipment) ?? new EquipmentSaveData();
    }

    public InventoryRuntimeData CreateInventory()
    {
        _inventory ??= new InventoryRuntimeData();
        return Clone(_inventory) ?? new InventoryRuntimeData();
    }

    private void OnValidate()
    {
        _data ??= new GameSaveData();
        _playerStats ??= new PlayerStatsSaveData();
        _equipment ??= new EquipmentSaveData();
        _inventory ??= new InventoryRuntimeData();
        _data.RepairAfterLoad();
    }

    private static T Clone<T>(T value)
    {
        return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    }
}
