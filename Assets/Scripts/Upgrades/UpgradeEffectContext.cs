using System;

public sealed class UpgradeEffectContext
{
    public UpgradeEffectContext(GameRuntimeData runtimeData)
    {
        RuntimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
    }

    public GameRuntimeData RuntimeData { get; }
}
