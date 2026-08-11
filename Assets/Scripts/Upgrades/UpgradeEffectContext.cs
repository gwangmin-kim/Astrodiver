using System;

public sealed class UpgradeEffectContext
{
    private readonly Func<GameProgressEventId, bool> _completeEvent;

    public UpgradeEffectContext(
        GameRuntimeData runtimeData,
        Func<GameProgressEventId, bool> completeEvent)
    {
        RuntimeData = runtimeData ?? throw new ArgumentNullException(nameof(runtimeData));
        _completeEvent = completeEvent ?? throw new ArgumentNullException(nameof(completeEvent));
    }

    public GameRuntimeData RuntimeData { get; }

    public bool CompleteEvent(GameProgressEventId eventId)
    {
        return _completeEvent(eventId);
    }
}
