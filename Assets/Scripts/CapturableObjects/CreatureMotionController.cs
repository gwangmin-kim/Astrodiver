using System;
using UnityEngine;

public enum CreatureFacingDirection
{
    Left = -1,
    Right = 1
}
[RequireComponent(typeof(CreatureBrain))]


[RequireComponent(typeof(CreatureController))]

public class CreatureMotionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CreatureBrain _brain;
    [SerializeField] private CreatureController _creatureController;

    [Header("Movement Stats")]
    [SerializeField, Min(0f)] private float _moveSpeed = 1f;
    [SerializeField, Min(0f)] private float _chaseSpeed = 1.2f;
    [SerializeField, Min(0f)] private float _runawaySpeed = 1.4f;
    [SerializeField, Min(0f)] private float _tryEscapeSpeed = 1.3f;
    [SerializeField, Min(0.01f)] private float _velocitySmoothTime = 0.2f;
    [SerializeField, Range(0f, 1f)] private float _capturedNetFollowRatio = 0.5f;
    [SerializeField, Min(0f)] private float _capturedRepulsionPadding = 0.1f;
    [SerializeField, Min(0f)] private float _capturedRepulsionSpeed = 2f;

    [Header("Visual")]
    [SerializeField] private Transform _rootVisualTransform;
    [SerializeField] private CreatureFacingDirection _baseFacingDirection = CreatureFacingDirection.Right;

    private Vector2 _targetVelocity;
    private Vector2 _currentVelocity;
    private Vector2 _smoothVelocity;
    private Vector2 _moveDirection;
    private Vector2 _escapeFallbackDirection;
    private Vector3 _baseVisualScale;

    public Vector2 TargetVelocity => _targetVelocity;
    public event EventHandler<CreatureTargetVelocityChangedEventArgs> TargetVelocityChanged;

    private void Initialize()
    {
        if (_brain == null) _brain = GetComponent<CreatureBrain>();
        if (_creatureController == null) _creatureController = GetComponent<CreatureController>();
        if (_rootVisualTransform == null) _rootVisualTransform = transform.Find("body");
        if (_rootVisualTransform != null) _baseVisualScale = _rootVisualTransform.localScale;
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnValidate()
    {
        Initialize();
    }

    private void OnEnable()
    {
        _brain.StateChanged += OnStateChanged;
        ConfigureState(_brain.CurrentState);
    }

    private void OnDisable()
    {
        if (_brain != null) _brain.StateChanged -= OnStateChanged;
        _currentVelocity = _smoothVelocity = _targetVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (IsCapturedState(_brain.CurrentState)) return;

        switch (_brain.CurrentState)
        {
            case CreatureState.Idle:
                SetTargetVelocity(Vector2.zero);
                break;
            case CreatureState.Move:
                SetTargetVelocity(_moveDirection * _moveSpeed);
                break;
            case CreatureState.Chase:
                SetTargetVelocity(GetPlayerDirection() * _chaseSpeed);
                break;
            case CreatureState.Runaway:
                SetTargetVelocity(-GetPlayerDirection() * _runawaySpeed);
                break;
        }
        ApplyMotion(Time.deltaTime);
    }

    public void UpdateCapturedMotion(CaptureMotionContext context, float deltaTime)
    {
        if (!IsCapturedState(_brain.CurrentState) || deltaTime <= 0f) return;

        Vector2 velocity = context.netVelocity * _capturedNetFollowRatio + CalculateRepulsionVelocity(context);
        if (_brain.CurrentState == CreatureState.CapturedTryEscape)
        {
            Vector2 outward = (Vector2)transform.position - context.netCenter;
            velocity += (outward.sqrMagnitude > Mathf.Epsilon ? outward.normalized : _escapeFallbackDirection) * _tryEscapeSpeed;
        }
        SetTargetVelocity(velocity);
        ApplyMotion(deltaTime);
    }

    public void ClampCapturedPosition(Vector2 position) => transform.position = position;

    private void OnStateChanged(object sender, CreatureStateChangedEventArgs args) => ConfigureState(args.NextState);

    private void ConfigureState(CreatureState state)
    {
        if (state == CreatureState.Move) _moveDirection = RandomDirection();
        if (state == CreatureState.CapturedTryEscape) _escapeFallbackDirection = RandomDirection();
        if (state == CreatureState.Idle || state == CreatureState.CapturedIdle) SetTargetVelocity(Vector2.zero);
    }

    private Vector2 CalculateRepulsionVelocity(CaptureMotionContext context)
    {
        Vector2 result = Vector2.zero;
        Vector2 position = transform.position;
        for (int i = 0; i < context.targets.Count; i++)
        {
            CapturedTargetSnapshot other = context.targets[i];
            if (other.target == _creatureController) continue;
            float minDistance = _creatureController.Radius + other.radius + _capturedRepulsionPadding;
            Vector2 offset = position - other.position;
            float distance = offset.magnitude;
            if (distance >= minDistance) continue;
            Vector2 direction = distance > Mathf.Epsilon ? offset / distance : RandomDirection();
            result += direction * ((minDistance - distance) * _capturedRepulsionSpeed);
        }
        return result;
    }

    private Vector2 GetPlayerDirection()
    {
        if (PlayerContext.Instance == null) return Vector2.zero;
        Vector2 offset = PlayerContext.Instance.transform.position - transform.position;
        return offset.sqrMagnitude > Mathf.Epsilon ? offset.normalized : Vector2.zero;
    }

    private void ApplyMotion(float deltaTime)
    {
        _currentVelocity = Vector2.SmoothDamp(_currentVelocity, _targetVelocity, ref _smoothVelocity, _velocitySmoothTime, Mathf.Infinity, deltaTime);
        transform.position += (Vector3)(_currentVelocity * deltaTime);
        UpdateVisualFacing(_currentVelocity.x);
    }

    private void SetTargetVelocity(Vector2 velocity)
    {
        if (_targetVelocity == velocity) return;
        Vector2 previous = _targetVelocity;
        _targetVelocity = velocity;
        TargetVelocityChanged?.Invoke(this, new CreatureTargetVelocityChangedEventArgs(previous, velocity));
    }

    private void UpdateVisualFacing(float horizontalDirection)
    {
        if (_rootVisualTransform == null || Mathf.Abs(horizontalDirection) < 0.001f) return;
        bool baseDirection = (horizontalDirection > 0f) == (_baseFacingDirection == CreatureFacingDirection.Right);
        Vector3 scale = _rootVisualTransform.localScale;
        scale.x = Mathf.Abs(_baseVisualScale.x) * (baseDirection ? 1f : -1f);
        _rootVisualTransform.localScale = scale;
    }

    private static bool IsCapturedState(CreatureState state) => state == CreatureState.CapturedIdle || state == CreatureState.CapturedTryEscape;
    private static Vector2 RandomDirection()
    {
        Vector2 direction = UnityEngine.Random.insideUnitCircle;
        return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
    }
}

public sealed class CreatureTargetVelocityChangedEventArgs : EventArgs
{
    public Vector2 PreviousTargetVelocity { get; }
    public Vector2 TargetVelocity { get; }
    public CreatureTargetVelocityChangedEventArgs(Vector2 previous, Vector2 next) { PreviousTargetVelocity = previous; TargetVelocity = next; }
}
