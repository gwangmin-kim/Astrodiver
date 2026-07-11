using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerCameraController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("Camera Target")]
    [SerializeField] private Transform _cameraTarget;

    [Header("Tracking Settings")]
    [Tooltip("카메라가 조준 방향으로 치우쳐질 수 있는 최대 거리")]
    [SerializeField] private float _maxRange;
    [Tooltip("조준 방향으로 타겟을 부드럽게 이동시키는 시간")]
    [SerializeField] private float _dampingTime;
    private Vector2 _smoothDampVelocity; // SmoothDamp 내부 계산용 변수

    private void Awake()
    {
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        Vector2 aimInput = _inputHandler.AimInput;
        SetTargetPosition(aimInput, Time.deltaTime);
    }

    private void SetTargetPosition(Vector2 aimInput, float deltaTime)
    {
        Vector2 offset = _maxRange * aimInput;
        Vector2 targetPosition = (Vector2)transform.position + offset;

        _cameraTarget.position = Vector2.SmoothDamp(
            _cameraTarget.position,
            targetPosition,
            ref _smoothDampVelocity,
            _dampingTime,
            Mathf.Infinity,
            deltaTime
        );
    }
}
