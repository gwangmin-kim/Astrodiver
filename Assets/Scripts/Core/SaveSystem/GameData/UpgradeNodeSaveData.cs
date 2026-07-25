using System;
using UnityEngine;

[Serializable]
public struct UpgradeNodeSaveData
{
    public string nodeId;
    [Min(1)] public int level;
}
