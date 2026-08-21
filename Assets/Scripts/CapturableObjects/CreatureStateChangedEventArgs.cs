using System;

public sealed class CreatureStateChangedEventArgs : EventArgs
{
    public CreatureState PreviousState { get; }
    public CreatureState NextState { get; }

    public CreatureStateChangedEventArgs(CreatureState previousState, CreatureState nextState)
    {
        PreviousState = previousState;
        NextState = nextState;
    }
}
