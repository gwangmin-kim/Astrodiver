using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovementController : MonoBehaviour
{
    private const float MinimumBoundsSize = 0.01f;
    private const float DefaultMaximumFallSpeed = 12f;

    [Header("Required Components")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField, Min(0.01f)] private float _maximumFallSpeed = DefaultMaximumFallSpeed;

    [Header("Stage Bounds")]
    [Tooltip("비어 있으면 현재 씬의 WorldBounds2D를 자동으로 사용합니다.")]
    [SerializeField] private WorldBounds2D _movementBounds;

    [Header("Player Local Bounds")]
    [Tooltip("플레이어 피벗 기준 로컬 AABB의 좌하단 점입니다.")]
    [SerializeField] private Vector2 _playerBoundsMin = new(-0.4f, -0.9f);
    [Tooltip("플레이어 피벗 기준 로컬 AABB의 우상단 점입니다.")]
    [SerializeField] private Vector2 _playerBoundsMax = new(0.4f, 0.9f);

    private Vector2 _dampingVelocity;
    private PlayerMovementData _data;

    private void Awake()
    {
        ResolveReferences();

    }

    private void Start()
    {
        RefreshMovementData();
    }

    private void OnValidate()
    {
        NormalizePlayerBounds();
        _maximumFallSpeed = Mathf.Max(0.01f, _maximumFallSpeed);
        ResolveReferences();
    }

    public void InitializeForStage(float gravityScale)
    {
        _rigidbody.gravityScale = gravityScale;
    }

    private void FixedUpdate()
    {
        if (_rigidbody == null || !_rigidbody.simulated) return;

        RefreshMovementData();
        float dt = Time.fixedDeltaTime;
        ClampToMovementBounds();

        Vector2 velocity = _rigidbody.linearVelocity;
        Vector2 moveInput = _inputHandler != null && _inputHandler.InputEnabled
            ? _inputHandler.MoveInput
            : Vector2.zero;
        float inputX = Mathf.Clamp(moveInput.x, -1f, 1f);
        float dampingTime = Mathf.Abs(inputX) > Mathf.Epsilon
            ? _data.moveDampingTime
            : _data.stopDampingTime;
        float horizontal = Mathf.SmoothDamp(
            velocity.x,
            _data.MoveSpeed * inputX,
            ref _dampingVelocity.x,
            dampingTime,
            Mathf.Infinity,
            dt);

        bool thrusting = _inputHandler != null &&
                         _inputHandler.InputEnabled &&
                         _inputHandler.JumpHeld;
        float vertical = velocity.y;
        float gravityAcceleration = Physics2D.gravity.y * _rigidbody.gravityScale;
        if (thrusting)
        {
            float desiredNextVertical = Mathf.Min(
                vertical + (gravityAcceleration + _data.RiseAcceleration) * dt,
                _data.RiseSpeed);
            vertical = desiredNextVertical - gravityAcceleration * dt;
        }
        else
        {
            float naturalNextVertical = vertical + gravityAcceleration * dt;
            if (naturalNextVertical < -_maximumFallSpeed)
            {
                vertical = -_maximumFallSpeed - gravityAcceleration * dt;
            }
        }

        _rigidbody.linearVelocity = new Vector2(horizontal, vertical);
        ClampToMovementBounds();
    }

    private void ClampToMovementBounds()
    {
        if (_movementBounds == null) return;

        Vector2 position = _rigidbody.position;
        GetPlayerBoundsWorldOffsets(
            out Vector2 playerBoundsMin,
            out Vector2 playerBoundsMax);
        Vector2 clampedPosition = _movementBounds.ClampPoint(
            position,
            playerBoundsMin,
            playerBoundsMax);
        bool clampX = !Mathf.Approximately(position.x, clampedPosition.x);
        bool clampY = !Mathf.Approximately(position.y, clampedPosition.y);
        if (!clampX && !clampY) return;

        _rigidbody.position = clampedPosition;
        Vector2 velocity = _rigidbody.linearVelocity;
        if (clampX)
        {
            velocity.x = 0f;
            _dampingVelocity.x = 0f;
        }

        if (clampY)
        {
            velocity.y = 0f;
            _dampingVelocity.y = 0f;
        }

        _rigidbody.linearVelocity = velocity;
    }

    private void RefreshMovementData()
    {
        if (GameDataManager.Instance != null)
        {
            _data = GameDataManager.Instance.GetMovement();
        }
    }

    private void GetPlayerBoundsWorldOffsets(
        out Vector2 boundsMin,
        out Vector2 boundsMax)
    {
        Vector2 localMin = Vector2.Min(_playerBoundsMin, _playerBoundsMax);
        Vector2 localMax = Vector2.Max(_playerBoundsMin, _playerBoundsMax);
        Vector2 bottomLeft = transform.TransformVector(new Vector3(localMin.x, localMin.y));
        Vector2 topLeft = transform.TransformVector(new Vector3(localMin.x, localMax.y));
        Vector2 topRight = transform.TransformVector(new Vector3(localMax.x, localMax.y));
        Vector2 bottomRight = transform.TransformVector(new Vector3(localMax.x, localMin.y));
        boundsMin = Vector2.Min(Vector2.Min(bottomLeft, topLeft), Vector2.Min(topRight, bottomRight));
        boundsMax = Vector2.Max(Vector2.Max(bottomLeft, topLeft), Vector2.Max(topRight, bottomRight));
    }

    private void NormalizePlayerBounds()
    {
        Vector2 min = Vector2.Min(_playerBoundsMin, _playerBoundsMax);
        Vector2 max = Vector2.Max(_playerBoundsMin, _playerBoundsMax);
        _playerBoundsMin = min;
        _playerBoundsMax = max;
        if (_playerBoundsMax.x - _playerBoundsMin.x < MinimumBoundsSize)
            _playerBoundsMax.x = _playerBoundsMin.x + MinimumBoundsSize;
        if (_playerBoundsMax.y - _playerBoundsMin.y < MinimumBoundsSize)
            _playerBoundsMax.y = _playerBoundsMin.y + MinimumBoundsSize;
    }

    private void ResolveReferences()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
        if (_movementBounds == null) _movementBounds = FindAnyObjectByType<WorldBounds2D>();
    }
}

[System.Serializable]
public struct PlayerMovementData
{
    [Header("Speed Settings")]
    [Tooltip("플레이어의 기본 이동 속도")]
    [Min(0.1f)] public float baseMoveSpeed;
    [Tooltip("기본 이동 속도에 적용되는 비율 (1 = 100%)")]
    [Min(0f)] public float moveSpeedRatio;

    [Header("Session Ascent")]
    [Tooltip("상승 목표 속도의 고정 베이스")]
    [Min(0f)] public float baseRiseSpeed;
    [Tooltip("상승 목표 속도 베이스에 적용되는 업그레이드 비율")]
    [Min(0f)] public float riseSpeedRatio;
    [Tooltip("상승 추진 가속도의 고정 베이스")]
    [Min(0f)] public float baseRiseAcceleration;
    [Tooltip("상승 추진 가속도 베이스에 적용되는 업그레이드 비율")]
    [Min(0f)] public float riseAccelerationRatio;

    public float MoveSpeed => Mathf.Max(0f, baseMoveSpeed * moveSpeedRatio);
    public float RiseSpeed => Mathf.Max(0f, baseRiseSpeed * riseSpeedRatio);
    public float RiseAcceleration =>
        Mathf.Max(0f, baseRiseAcceleration * riseAccelerationRatio);

    [Header("Inertia Settings")]
    [Tooltip("이동을 시작할 때 속도를 부드럽게 증가시키는 지연 시간")]
    [Min(0.01f)] public float moveDampingTime;
    [Tooltip("이동을 멈출 때 속도를 부드럽게 감소시키는 지연 시간")]
    [Min(0.01f)] public float stopDampingTime;
}
