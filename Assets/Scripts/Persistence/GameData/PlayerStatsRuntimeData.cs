using Astrodiver.Player.Actions;
using Astrodiver.ResourceFragments;

namespace Astrodiver.Persistence.GameData
{
    using System;

    [Serializable]
    public sealed class PlayerStatsRuntimeData
    {
        public PlayerMovementData movement;
        public BatteryData battery;
        public MagnetData magnet;
    }
}
