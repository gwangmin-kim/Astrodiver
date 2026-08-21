using UnityEngine;

/// <summary>
/// Simple autonomous brain: alternates between Idle and Move, and remains idle while captured.
/// </summary>
public class BasicCreatureBrain : CreatureBrain
{
    [SerializeField, Min(0.01f)] private float _minimumStateDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float _maximumStateDuration = 3f;
    [SerializeField] private CreatureState _initialState = CreatureState.Idle;

    private float _nextStateChangeTime;

    private void OnValidate()
    {
        _maximumStateDuration = Mathf.Max(_minimumStateDuration, _maximumStateDuration);
    }

    private void OnEnable()
    {
        InitializeState(_initialState);
    }

    private void Update()
    {
        if (CurrentState != CreatureState.Idle && CurrentState != CreatureState.Move) return;
        if (Time.time < _nextStateChangeTime) return;

        SetState(CurrentState == CreatureState.Idle ? CreatureState.Move : CreatureState.Idle);
    }

    protected override void OnStateEntered(CreatureState state)
    {
        if (state == CreatureState.Idle || state == CreatureState.Move)
        {
            _nextStateChangeTime = Time.time + Random.Range(_minimumStateDuration, _maximumStateDuration);
        }
    }
}
