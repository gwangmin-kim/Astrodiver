using System;
using UnityEngine;

/// <summary>
/// Base state machine for a creature. Derived brains decide when to transition states.
/// </summary>
public abstract class CreatureBrain : MonoBehaviour
{
    public CreatureState CurrentState { get; private set; }
    public event EventHandler<CreatureStateChangedEventArgs> StateChanged;
    public event Action<CaptureReleaseReason> CaptureReleaseRequested;

    protected void InitializeState(CreatureState initialState)
    {
        CurrentState = initialState;
        OnStateEntered(initialState);
    }

    public virtual void NotifyCaptureStarted(NetCaptureContext context)
    {
        SetState(CreatureState.CapturedIdle);
    }

    public virtual void NotifyCaptureReleased(CaptureReleaseReason reason)
    {
        SetState(CreatureState.Idle);
    }

    protected void SetState(CreatureState nextState)
    {
        if (nextState == CurrentState) return;

        CreatureState previousState = CurrentState;
        CurrentState = nextState;
        OnStateEntered(nextState);
        StateChanged?.Invoke(this, new CreatureStateChangedEventArgs(previousState, nextState));
    }

    protected void RequestCaptureRelease(CaptureReleaseReason reason)
    {
        CaptureReleaseRequested?.Invoke(reason);
    }

    protected virtual void OnStateEntered(CreatureState state) { }
}
