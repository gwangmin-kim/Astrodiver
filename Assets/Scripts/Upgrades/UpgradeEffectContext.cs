using Astrodiver.Persistence.GameData;
using System;

namespace Astrodiver.Upgrades
{
    public sealed class UpgradeEffectContext
    {
        public UpgradeEffectContext(GameRuntimeData runtimeData)
        {
            RuntimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
        }

        public GameRuntimeData RuntimeData { get; }
    }
}
