using System;
using UnityEngine;

[Serializable]
public sealed class FacilityRuntimeData
{
    [SerializeField] private bool _resourceChestUnlocked;
    [SerializeField] private bool _worktableUnlocked;
    [SerializeField, Min(1)] private int _worktableSlotCapacity = 1;
    [SerializeField, Min(0.01f)] private float _worktableTransferSpeedMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float _worktableProcessSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float _worktableYieldMultiplier = 1f;

    public bool ResourceChestUnlocked
    {
        get => _resourceChestUnlocked;
        internal set => _resourceChestUnlocked = value;
    }

    public bool WorktableUnlocked
    {
        get => _worktableUnlocked;
        internal set => _worktableUnlocked = value;
    }

    public int WorktableSlotCapacity
    {
        get => Mathf.Max(1, _worktableSlotCapacity);
        internal set => _worktableSlotCapacity = Mathf.Max(1, value);
    }

    public float WorktableTransferSpeedMultiplier
    {
        get => Mathf.Max(0.01f, _worktableTransferSpeedMultiplier);
        internal set => _worktableTransferSpeedMultiplier = Mathf.Max(0.01f, value);
    }

    public float WorktableProcessSpeedMultiplier
    {
        get => Mathf.Max(0.01f, _worktableProcessSpeedMultiplier);
        internal set => _worktableProcessSpeedMultiplier = Mathf.Max(0.01f, value);
    }

    public float WorktableYieldMultiplier
    {
        get => Mathf.Max(0f, _worktableYieldMultiplier);
        internal set => _worktableYieldMultiplier = Mathf.Max(0f, value);
    }
}
