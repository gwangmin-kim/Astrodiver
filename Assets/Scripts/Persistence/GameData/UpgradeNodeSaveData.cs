using System;
using UnityEngine;

namespace Astrodiver.Persistence.GameData
{
    [Serializable]
    public struct UpgradeNodeSaveData
    {
        public string nodeId;
        [Min(1)] public int level;
    }
}
