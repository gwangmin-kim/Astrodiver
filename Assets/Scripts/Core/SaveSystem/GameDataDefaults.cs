using UnityEngine;

[CreateAssetMenu(
    fileName = "GameDataDefaults",
    menuName = "Astrodiver/Save System/Game Data Defaults")]
public sealed class GameDataDefaults : ScriptableObject
{
    [SerializeField] private GameSaveData _data = new();

    public int CreatureSlotCount =>
        Mathf.Max(1, _data?.inventory?.CreatureSlots?.Count ?? 0);

    public GameSaveData CreateSaveData()
    {
        _data ??= new GameSaveData();
        _data.RepairAfterLoad(CreatureSlotCount);

        string json = JsonUtility.ToJson(_data);
        GameSaveData copy = JsonUtility.FromJson<GameSaveData>(json) ?? new GameSaveData();
        copy.RepairAfterLoad(CreatureSlotCount);
        return copy;
    }

    private void OnValidate()
    {
        _data ??= new GameSaveData();
        _data.RepairAfterLoad(CreatureSlotCount);
    }
}
