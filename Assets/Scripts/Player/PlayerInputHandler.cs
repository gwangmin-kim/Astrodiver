using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Settings")]
    [SerializeField] private string _playerMapName = "Player";
    [SerializeField] private string _moveActionName = "Move"; // WASD/Left Stick
    [SerializeField] private string _aimActionName = "Aim"; // 마우스/Right Stick
    [SerializeField] private string _interactActionName = "Interact"; // E/B(우측 버튼)
    [SerializeField] private string _dashActionName = "Dash"; // 스페이스바/A(하단 버튼)
    [SerializeField] private string _captureActionName = "Capture"; // 좌클릭/Right Button
    [SerializeField] private string _attackActionName = "Attack"; // 우클릭/Right Trigger
    [SerializeField] private string _cancelActionName = "Cancel"; // ESC/Start Button

    [Header("Mouse Aim Settings")]
    [Tooltip("마우스 사용 시 임계 조준 거리\n"
            + "마우스 포인터가 이 거리(월드 좌표 기준)를 넘어가면 벡터 크기가 1이 됨")]
    [SerializeField][Range(0.1f, 10f)] private float _mouseAimThreshold;

    [Header("Input Buffer Settings")]
    [Tooltip("버튼 입력이 캐싱되어 유지되는 시간(초)")]
    [SerializeField][Range(0f, 1f)] private float _inputBufferTime;
    // 버퍼링 타이머
    private float _interactBufferTimer = 0f;
    private float _dashBufferTimer = 0f;

    private Camera _mainCamera; // Camera.main을 매 프레임 호출하면 성능에 좋지 않으므로 캐싱

    // 입력 액션
    private InputAction _moveAction;
    private InputAction _aimAction;
    private InputAction _interactAction;
    private InputAction _dashAction;
    private InputAction _captureAction;
    private InputAction _attackAction;
    private InputAction _cancelAction;

    // 캐싱된 입력값
    // 값 타입
    public Vector2 MoveInput { get; private set; }
    public Vector2 AimInput { get; private set; }

    // 버튼 타입
    public bool InteractInput { get; private set; }
    public bool ConsumeInteractInput() // 인풋 버퍼링으로 인한 오작동 방지를 위한 소비 함수
    {
        if (InteractInput)
        {
            InteractInput = false;
            _interactBufferTimer = 0f;
            return true;
        }
        else return false;
    }
    public bool DashInput { get; private set; }
    public bool ConsumeDashInput() // 인풋 버퍼링으로 인한 오작동 방지를 위한 소비 함수
    {
        if (DashInput)
        {
            DashInput = false;
            _dashBufferTimer = 0f;
            return true;
        }
        else return false;
    }
    public bool CaptureInput { get; private set; }// Pass Through 타입으로 별도의 소비 함수 필요 없음
    public Action pressCaptureEvent;
    public Action releaseCaptureEvent;
    public bool AttackInput { get; private set; } // Pass Through 타입으로 별도의 소비 함수 필요 없음
    public Action pressAttackEvent;
    public Action releaseAttackEvent;
    public event Action CancelPressed;

    public bool InputEnabled { get; private set; } = true;

    private void Start()
    {
        _moveAction = FindAction(_moveActionName);
        _aimAction = FindAction(_aimActionName);
        _interactAction = FindAction(_interactActionName);
        _dashAction = FindAction(_dashActionName);
        _captureAction = FindAction(_captureActionName);
        _attackAction = FindAction(_attackActionName);
        _cancelAction = FindAction(_cancelActionName);

        _mainCamera = Camera.main;
    }

    public void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
        ResetInputState();

        InputActionMap map = InputSystem.actions.FindActionMap(_playerMapName);
        if (map == null) return;

        if (enabled)
        {
            map.Enable();
        }
        else
        {
            map.Disable();
            _cancelAction?.Enable();
        }
    }

    public void ResetInputState()
    {
        MoveInput = Vector2.zero;
        AimInput = Vector2.zero;
        InteractInput = false;
        DashInput = false;
        CaptureInput = false;
        AttackInput = false;
        _interactBufferTimer = 0f;
        _dashBufferTimer = 0f;
    }

    private InputAction FindAction(string actionName)
    {
        InputAction action = InputSystem.actions.FindAction($"{_playerMapName}/{actionName}");

        if (action != null) action.Enable();
        else Debug.LogError($"전역 Input Actions에서 '{actionName}' 액션을 찾을 수 없습니다.", this);

        return action;
    }

    private void Update()
    {
        if (_cancelAction?.WasPressedThisFrame() ?? false)
        {
            CancelPressed?.Invoke();
            return;
        }

        if (!InputEnabled) return;

        MoveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

        if (_aimAction != null)
        {
            AimInput = GetAimInput();
        }

        if (_interactAction != null)
        {
            // 입력 버퍼링
            if (_interactAction.WasPressedThisFrame())
            {
                _interactBufferTimer = _inputBufferTime;
            }

            if (_interactBufferTimer > 0f)
            {
                InteractInput = true;
                _interactBufferTimer -= Time.deltaTime;
            }
            else InteractInput = false;
        }

        if (_dashAction != null)
        {
            // 입력 버퍼링
            if (_dashAction.WasPressedThisFrame())
            {
                _dashBufferTimer = _inputBufferTime;
            }

            if (_dashBufferTimer > 0f)
            {
                DashInput = true;
                _dashBufferTimer -= Time.deltaTime;
            }
            else DashInput = false;
        }

        if (_captureAction != null)
        {
            if (_captureAction.WasPressedThisFrame())
                pressCaptureEvent?.Invoke();

            if (_captureAction.WasReleasedThisFrame())
                releaseCaptureEvent?.Invoke();

            CaptureInput = _captureAction.IsPressed();
        }

        if (_attackAction != null)
        {
            if (_attackAction.WasPressedThisFrame())
                pressAttackEvent?.Invoke();

            if (_attackAction.WasReleasedThisFrame())
                releaseAttackEvent?.Invoke();

            AttackInput = _attackAction.IsPressed();
        }
    }

    private Vector2 GetAimInput()
    {
        // 아무런 입력이 활성화되지 않았다면 스킵
        if (_aimAction.activeControl == null) return default;

        // 현재 입력을 주고 있는 디바이스가 마우스(포인터류)인지 확인
        bool isMouse = _aimAction.activeControl.device is Pointer;

        if (isMouse)
        {
            Vector2 mouseScreenPosition = _aimAction.ReadValue<Vector2>(); // 마우스의 화면 좌표
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return default;
            }

            // Camera가 저해상도 Render Texture를 출력하면 Screen 좌표계와
            // Camera 픽셀 좌표계의 크기가 달라진다. 화면 좌표를 Viewport 좌표로
            // 정규화한 뒤 변환해야 RawImage로 확대된 화면과 조준 방향이 일치한다.
            Vector2 mouseViewportPosition = new(
                mouseScreenPosition.x / Screen.width,
                mouseScreenPosition.y / Screen.height);

            float playerPlaneDistance =
                transform.position.z - _mainCamera.transform.position.z;
            Vector3 mouseWorldPosition = _mainCamera.ViewportToWorldPoint(
                new Vector3(
                    mouseViewportPosition.x,
                    mouseViewportPosition.y,
                    playerPlaneDistance));

            Vector2 diff = (Vector2)mouseWorldPosition - (Vector2)transform.position;
            float distance = diff.magnitude;

            // 임계 거리 기반 정규화 벡터 반환
            return (distance > _mouseAimThreshold) ? diff.normalized : diff / _mouseAimThreshold;
        }
        else
        {
            // 컨트롤러의 경우 스틱의 기울기 값을 그대로 사용
            return _aimAction.ReadValue<Vector2>();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, _mouseAimThreshold);
        Gizmos.color = Color.darkRed;
        Gizmos.DrawSphere(transform.position + (Vector3)AimInput, 0.05f);
    }
#endif
}
