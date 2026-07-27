using System;
using UnityEngine;

[Serializable]
public sealed class InventoryRuntimeData
{
    [SerializeField][Min(1)] private int _creatureSlotCapacity = 1;

    public int CreatureSlotCapacity
    {
        get => Mathf.Max(1, _creatureSlotCapacity);
        internal set => _creatureSlotCapacity = Mathf.Max(1, value);
    }
}
