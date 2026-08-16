using System;
using UnityEngine;

[Serializable]
public sealed class FacilityRuntimeData
{
    [SerializeField] private bool _resourceChestUnlocked;

    public bool ResourceChestUnlocked
    {
        get => _resourceChestUnlocked;
        internal set => _resourceChestUnlocked = value;
    }
}
