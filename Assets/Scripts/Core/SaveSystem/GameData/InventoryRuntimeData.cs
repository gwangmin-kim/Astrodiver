using UnityEngine;

[System.Serializable]
public sealed class InventoryRuntimeData
{
    [SerializeField][Min(1)] private int _creatureSlotCapacity = 1;
    [SerializeField][Min(1)] private int _creatureMaxStackCount = 10;
    [SerializeField][Range(0f, 1f)] private float _timeoutInventoryLossRatio = 1f;

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

    public float TimeoutInventoryLossRatio
    {
        get => Mathf.Clamp01(_timeoutInventoryLossRatio);
        internal set => _timeoutInventoryLossRatio = Mathf.Clamp01(value);
    }
}
