using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("Movement")]
    [SerializeField] private PlayerMovementData _movementData;
    private Vector2 _currentVelocity;
    private Vector2 _smoothDampVelocity; // SmoothDamp 내부 계산용 변수

    private void Awake()
    {
        if (_rigidbody == null) GetComponent<Rigidbody2D>();
        if (_inputHandler == null) GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        Vector2 moveInput = _inputHandler.MoveInput;
        Move(moveInput, Time.deltaTime);
    }

    private void Move(Vector2 moveInput, float deltaTime)
    {
        Vector2 targetVelocity = _movementData.moveSpeed * moveInput;
        bool isMoving = moveInput.sqrMagnitude > Mathf.Epsilon;
        float dampingTime = isMoving ? _movementData.moveDampingTime : _movementData.stopDampingTime;

        _currentVelocity = Vector2.SmoothDamp(
            _currentVelocity,
            targetVelocity,
            ref _smoothDampVelocity,
            dampingTime,
            Mathf.Infinity,
            deltaTime
        );

        _rigidbody.linearVelocity = _currentVelocity;
    }
}

[System.Serializable]
public struct PlayerMovementData
{
    [Header("Speed Settings")]
    [Tooltip("플레이어의 기본 이동 속도")]
    [Min(0.1f)] public float moveSpeed;

    [Header("Inertia Settings")]
    [Tooltip("이동을 시작할 때 속도를 부드럽게 증가시키는 지연 시간")]
    [Min(0.01f)] public float moveDampingTime;
    [Tooltip("이동을 멈출 때 속도를 부드럽게 감소시키는 지연 시간")]
    [Min(0.01f)] public float stopDampingTime;
}
