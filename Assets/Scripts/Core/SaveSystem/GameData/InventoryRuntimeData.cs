using UnityEngine;

[System.Serializable]
public sealed class InventoryRuntimeData
{
    [SerializeField][Min(1)] private int _creatureSlotCapacity = 1;
    [SerializeField][Min(1)] private int _creatureMaxStackCount = 10;

    public int CreatureSlotCapacity
    {
        get => Mathf.Max(1, _creatureSlotCapacity);
        internal set => _creatureSlotCapacity = Mathf.Max(1, value);
    }

    public int CreatureMaxStackCount
    {

        get => Mathf.Max(1, _creatureMaxStackCount);
        internal set => _creatureMaxStackCount = Mathf.Max(1, value);
    }
}
