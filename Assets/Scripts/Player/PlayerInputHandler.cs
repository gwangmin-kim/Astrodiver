using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Settings")]
    [SerializeField] private string _playerMapName = "Player";
    [SerializeField] private string _moveActionName = "Move";
    [SerializeField] private string _aimActionName = "Aim";
    [SerializeField] private string _captureActionName = "Capture"; // 좌클릭/Right Button
    [SerializeField] private string _attackActionName = "Attack"; // 우클릭/Right Trigger

    [Header("Mouse Aim Settings")]
    [Tooltip("마우스 사용 시 임계 조준 거리\n마우스 포인터가 이 거리(월드 좌표 기준)를 넘어가면 벡터 크기가 1이 됨")]
    [SerializeField] private float _mouseAimThreshold = 5f;

    [Header("Input Buffer Settings")]
    [Tooltip("Capture 입력이 캐싱되어 유지되는 시간(초)")]
    [SerializeField] private float _captureBufferTime = 0.15f;
    private float _captureBufferTimer = 0f; // 버퍼링 타이머

    private Camera _mainCamera; // Camera.main을 매 프레임 호출하면 성능에 좋지 않으므로 캐싱

    // 캐싱된 입력값
    // 값 타입
    public Vector2 MoveInput { get; private set; }
    public Vector2 AimInput { get; private set; }

    // 버튼 타입
    public bool CaptureInput { get; private set; }
    public bool GetCaptureInput() // 인풋 버퍼링으로 인한 오작동 방지를 위한 소비 함수
    {
        if (CaptureInput)
        {
            CaptureInput = false;
            _captureBufferTimer = 0f;
            return true;
        }
        else return false;
    }
    public bool AttackInput { get; private set; } // Pass Through 타입으로 별도의 소비 함수 필요 없음
    public Action pressAttackEvent;
    public Action releaseAttackEvent;

    // 입력 액션
    private InputAction _moveAction;
    private InputAction _aimAction;
    private InputAction _captureAction;
    private InputAction _attackAction;

    private void Start()
    {
        _moveAction = BindAction(_moveActionName);
        _aimAction = BindAction(_aimActionName);
        _captureAction = BindAction(_captureActionName);
        _attackAction = BindAction(_attackActionName);

        _mainCamera = Camera.main;
    }

    private InputAction BindAction(string actionName)
    {
        InputAction action = InputSystem.actions.FindAction($"{_playerMapName}/{actionName}");

        if (action != null) action.Enable();
        else Debug.LogError($"전역 Input Actions에서 '{actionName}' 액션을 찾을 수 없습니다.");

        return action;
    }

    private void Update()
    {
        if (_moveAction != null)
        {
            MoveInput = _moveAction.ReadValue<Vector2>();
        }

        if (_aimAction != null)
        {
            AimInput = GetAimInput();
        }

        if (_captureAction != null)
        {
            // 입력 버퍼링
            if (_captureAction.WasPressedThisFrame())
            {
                _captureBufferTimer = _captureBufferTime;
            }

            if (_captureBufferTimer > 0f)
            {
                CaptureInput = true;
                _captureBufferTimer -= Time.deltaTime;
            }
            else CaptureInput = false;
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
            Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(
                new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, 0f)); // 마우스의 월드 좌표

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
