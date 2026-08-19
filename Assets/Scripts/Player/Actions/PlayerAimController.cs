using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAimController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("Body Orientation Settings")]
    [SerializeField] private Transform _bodyTransform;

    [Header("Hand Movement Settings")]
    [SerializeField] private Transform _handTransform;
    [SerializeField] private Vector2 _handOffset;
    [SerializeField] private float _handRange;
    [SerializeField] private float _handDampingTime;

    // SmoothDamp 내부 계산용 변수
    private Vector2 _smoothDampPositionVelocity;
    private float _smoothDampAngleVelocity;

    private void Awake()
    {
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        Vector2 aimInput = _inputHandler.AimInput;
        SetHeadingDirection(aimInput);
        SetHandTransform(aimInput, Time.deltaTime);
    }

    private void SetHeadingDirection(Vector2 aimInput)
    {
        float scaleX = (aimInput.x >= 0f) ? 1f : -1f;
        Vector3 nextScale = new(scaleX, 1f, 1f);
        _bodyTransform.localScale = nextScale;
        _handTransform.localScale = nextScale;
    }

    private void SetHandTransform(Vector2 aimInput, float deltaTime)
    {
        Vector2 handOrigin = (Vector2)transform.position + _handOffset;
        Vector2 targetPosition = handOrigin + _handRange * aimInput;

        Vector2 nextPosition = Vector2.SmoothDamp(
            _handTransform.position,
            targetPosition,
            ref _smoothDampPositionVelocity,
            _handDampingTime,
            Mathf.Infinity,
            deltaTime
        );

        float targetAngle = Mathf.Atan2(aimInput.y, aimInput.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = _handTransform.eulerAngles.z;

        float nextAngle = Mathf.SmoothDampAngle(
            currentAngle,
            targetAngle,
            ref _smoothDampAngleVelocity,
            _handDampingTime,
            Mathf.Infinity,
            deltaTime
        );
        Quaternion nextRotation = Quaternion.Euler(0f, 0f, nextAngle);

        _handTransform.SetPositionAndRotation(nextPosition, nextRotation);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.skyBlue;
        Gizmos.DrawWireSphere(transform.position + (Vector3)_handOffset, _handRange);
    }
#endif
}
