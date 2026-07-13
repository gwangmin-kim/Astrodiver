using System.Collections;
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
    [SerializeField][Min(0f)] private float _coyoteTime;

    // 관성
    private float _currentHorizontalVelocity;
    private float _smoothDampVelocity; // SmoothDamp 내부 계산용 변수

    // 점프 판정
    private bool _isGrounded = true;
    private Coroutine _ungroundedCoroutine;

    [Header("Body Orientation Settings")]
    [SerializeField] private Transform _bodyTransform;

    private void Awake()
    {
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void OnDisable()
    {
        CancelUngroundedReservation();
    }

    private void Update()
    {
        Vector2 moveInput = _inputHandler.MoveInput;
        Move(moveInput, Time.deltaTime);
        SetHeadingDirection(moveInput);

        bool jumpInput = _inputHandler.DashInput;
        if (jumpInput && _isGrounded)
        {
            _inputHandler.ConsumeDashInput();
            Jump();
        }
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
        float verticalVelocity = Mathf.Sqrt(-2 * gravity * _data.jumpHeight);
        _rigidbody.linearVelocityY = verticalVelocity;
        _isGrounded = false;
    }

    private void SetHeadingDirection(Vector2 moveInput)
    {
        if (Mathf.Abs(moveInput.x) <= _inputThreshold) return;

        float scaleX = moveInput.x > 0f ? 1 : -1;
        Vector3 nextScale = new(scaleX, 1f, 1f);
        _bodyTransform.localScale = nextScale;
    }

    private IEnumerator ReserveUngrounded(float second)
    {
        yield return new WaitForSeconds(second);
        _isGrounded = false;
        _ungroundedCoroutine = null;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > _groundNormalThreshold)
            {
                _isGrounded = true;
                CancelUngroundedReservation();
                return;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!isActiveAndEnabled || !_isGrounded) return;

        CancelUngroundedReservation();
        _ungroundedCoroutine = StartCoroutine(ReserveUngrounded(_coyoteTime));
    }

    private void CancelUngroundedReservation()
    {
        if (_ungroundedCoroutine == null) return;

        StopCoroutine(_ungroundedCoroutine);
        _ungroundedCoroutine = null;
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
