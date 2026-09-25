using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class HubPlayerMovementController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("Movement")]
    [SerializeField] private HubPlayerMovementData _data;
    [SerializeField][Min(0f)] private float _inputThreshold;
    [SerializeField][Range(0.1f, 1f)] private float _groundNormalThreshold;

    // 관성
    private float _currentHorizontalVelocity;
    private float _smoothDampVelocity; // SmoothDamp 내부 계산용 변수

    // 점프 판정
    private bool _isGrounded = true;

    [Header("Body Orientation Settings")]
    [SerializeField] private Transform _bodyTransform;

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void OnDisable()
    {
        _isGrounded = false;
    }

    private void OnEnable()
    {
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
        _inputHandler.JumpStarted += OnJumpStarted;
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
        {
            _inputHandler.JumpStarted -= OnJumpStarted;
        }
    }

    private void Update()
    {
        Vector2 moveInput = _inputHandler.MoveInput;
        Move(moveInput, Time.deltaTime);
        SetHeadingDirection(moveInput);
    }

    private void OnJumpStarted()
    {
        if (!_isGrounded)
        {
            return;
        }

        _isGrounded = false;
        Jump();
    }

    private void Move(Vector2 moveInput, float deltaTime)
    {
        int horizontalDirection = (Mathf.Abs(moveInput.x) > _inputThreshold)
                                  ? moveInput.x > 0f ? 1 : -1
                                  : 0;
        float targetHorizontalVelocity = horizontalDirection * _data.moveSpeed;

        _currentHorizontalVelocity = Mathf.SmoothDamp(
           _currentHorizontalVelocity,
           targetHorizontalVelocity,
           ref _smoothDampVelocity,
           _data.dampingTime,
           Mathf.Infinity,
           deltaTime
       );

        _rigidbody.linearVelocityX = _currentHorizontalVelocity;
    }

    private void Jump()
    {
        float gravity = Physics2D.gravity.y * _rigidbody.gravityScale;
        if (gravity >= 0f)
        {
            Debug.LogError(
                "HubPlayerMovementController: Jump requires downward gravity.",
                this);
            return;
        }

        float verticalVelocity = Mathf.Sqrt(-2 * gravity * _data.jumpHeight);
        _rigidbody.linearVelocityY = verticalVelocity;
    }

    private void SetHeadingDirection(Vector2 moveInput)
    {
        if (Mathf.Abs(moveInput.x) <= _inputThreshold) return;

        float scaleX = moveInput.x > 0f ? 1 : -1;
        Vector3 nextScale = new(scaleX, 1f, 1f);
        _bodyTransform.localScale = nextScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > _groundNormalThreshold)
            {
                _isGrounded = true;
                return;
            }
        }
    }

}

[System.Serializable]
public struct HubPlayerMovementData
{
    [Header("Speed Settings")]
    [Tooltip("플레이어의 기본 이동 속도")]
    [Min(0.1f)] public float moveSpeed;

    [Header("Inertia Settings")]
    [Tooltip("이동 시 속도를 부드럽게 변화시키는 지연 시간")]
    [Min(0.01f)] public float dampingTime;

    [Header("Jump")]
    [Tooltip("점프 높이")]
    [Min(0.1f)] public float jumpHeight;
}
