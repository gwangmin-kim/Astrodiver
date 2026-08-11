using System;
using UnityEngine;

[Serializable]
public sealed class ProgressEventUpgradeEffect : UpgradeEffect
{
    [SerializeField] private GameProgressEventId _eventId;

    public ProgressEventUpgradeEffect()
    {
    }

    public ProgressEventUpgradeEffect(GameProgressEventId eventId)
    {
        _eventId = eventId;
    }

    public GameProgressEventId EventId => _eventId;

    public override bool TryValidate(out string error)
    {
        if (_eventId == GameProgressEventId.None ||
            !Enum.IsDefined(typeof(GameProgressEventId), _eventId))
        {
            error = "A progress event effect requires a valid event id.";
            return false;
        }

        error = null;
        return true;
    }

    public override bool TryApply(UpgradeEffectContext context, out string error)
    {
        if (context == null)
        {
            error = "Upgrade effect context is null.";
            return false;
        }

        if (!TryValidate(out error))
        {
            return false;
        }

        context.CompleteEvent(_eventId);
        error = null;
        return true;
    }
}
