using Astrodiver.Player.Actions;
using Astrodiver.ResourceFragments;
using System;

namespace Astrodiver.Persistence.GameData
{
    [Serializable]
    public sealed class PlayerStatsRuntimeData
    {
        public PlayerMovementData movement;
        public BatteryData battery;
        public MagnetData magnet;
    }
}
