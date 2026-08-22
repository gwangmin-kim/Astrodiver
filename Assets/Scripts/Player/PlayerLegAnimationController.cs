using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public sealed class PlayerLegAnimationController : MonoBehaviour
{
    private const float MinimumFrameInterval = 0.01f;

    private enum LegPose
    {
        Stand,
        Fly,
    }

    [Header("Required Components")]
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("Leg Objects")]
    [Tooltip("왼쪽 다리의 Stand 자세 GameObject입니다.")]
    [SerializeField] private GameObject _leftStandLeg;
    [Tooltip("왼쪽 다리의 Fly 자세 GameObject입니다.")]
    [SerializeField] private GameObject _leftFlyLeg;
    [Tooltip("오른쪽 다리의 Stand 자세 GameObject입니다.")]
    [SerializeField] private GameObject _rightStandLeg;
    [Tooltip("오른쪽 다리의 Fly 자세 GameObject입니다.")]
    [SerializeField] private GameObject _rightFlyLeg;

    [Header("Animation")]
    [Tooltip("활성화하면 X축 이동 입력만 애니메이션 판정에 사용합니다.")]
    [SerializeField] private bool _isHorizontal;
    [Tooltip("자세를 교체하는 간격입니다.")]
    [SerializeField][Min(MinimumFrameInterval)] private float _frameInterval = 0.3f;
    [Tooltip("정지 중 표시할 기본 자세입니다.")]
    [SerializeField] private LegPose _defaultPose = LegPose.Stand;

    private float _frameTimer;
    private bool _walkingFrameUsesLeftStand;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        _frameTimer = 0f;
        _walkingFrameUsesLeftStand = false;
        SetDefaultPose();
    }

    private void Update()
    {
        Vector2 moveInput = _inputHandler.MoveInput;
        bool isMoving = _isHorizontal
            ? Mathf.Abs(moveInput.x) > Mathf.Epsilon
            : moveInput.sqrMagnitude > Mathf.Epsilon;

        if (!isMoving)
        {
            _frameTimer = 0f;
            _walkingFrameUsesLeftStand = false;
            SetDefaultPose();
            return;
        }

        _frameTimer += Time.deltaTime;
        float frameInterval = Mathf.Max(MinimumFrameInterval, _frameInterval);
        if (_frameTimer < frameInterval)
        {
            return;
        }

        _frameTimer %= frameInterval;
        _walkingFrameUsesLeftStand = !_walkingFrameUsesLeftStand;
        SetWalkingPose(_walkingFrameUsesLeftStand);
    }

    private void OnValidate()
    {
        _frameInterval = Mathf.Max(MinimumFrameInterval, _frameInterval);
        ResolveReferences();
    }

    private void SetDefaultPose()
    {
        bool useStand = _defaultPose == LegPose.Stand;
        SetActive(_leftStandLeg, useStand);
        SetActive(_leftFlyLeg, !useStand);
        SetActive(_rightStandLeg, useStand);
        SetActive(_rightFlyLeg, !useStand);
    }

    private void SetWalkingPose(bool leftUsesStand)
    {
        SetActive(_leftStandLeg, leftUsesStand);
        SetActive(_leftFlyLeg, !leftUsesStand);
        SetActive(_rightStandLeg, !leftUsesStand);
        SetActive(_rightFlyLeg, leftUsesStand);
    }

    private static void SetActive(GameObject legObject, bool isActive)
    {
        if (legObject != null && legObject.activeSelf != isActive)
        {
            legObject.SetActive(isActive);
        }
    }

    private void ResolveReferences()
    {
        if (_inputHandler == null)
        {
            _inputHandler = GetComponent<PlayerInputHandler>();
        }
    }
}
