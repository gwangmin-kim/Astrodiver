using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Settings")]
    [SerializeField] private string _playerMapName = "Player";
    [SerializeField] private string _moveActionName = "Move";
    [SerializeField] private string _aimActionName = "Aim";
    [SerializeField] private string _interactActionName = "Interact";
    [SerializeField] private string _jumpActionName = "Jump";
    [SerializeField] private string _captureActionName = "Capture";
    [SerializeField] private string _attackActionName = "Attack";
    [SerializeField] private string _cancelActionName = "Cancel";

    [Header("Mouse Aim Settings")]
    [Tooltip("마우스 사용 시 임계 조준 거리(월드 좌표)")]
    [SerializeField, Range(0.1f, 10f)] private float _mouseAimThreshold = 5f;

    private Camera _mainCamera;
    private InputActionMap _playerMap;
    private InputAction _moveAction;
    private InputAction _aimAction;
    private InputAction _interactAction;
    private InputAction _jumpAction;
    private InputAction _captureAction;
    private InputAction _attackAction;
    private InputAction _cancelAction;

    public Vector2 MoveInput { get; private set; }
    public Vector2 AimInput { get; private set; }
    public bool InteractHeld { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool CaptureHeld { get; private set; }
    public bool AttackHeld { get; private set; }
    public bool CancelHeld { get; private set; }
    public bool InputEnabled { get; private set; } = true;

    public event Action InteractStarted;
    public event Action InteractEnded;
    public event Action JumpStarted;
    public event Action JumpEnded;
    public event Action CaptureStarted;
    public event Action CaptureEnded;
    public event Action AttackStarted;
    public event Action AttackEnded;
    public event Action CancelStarted;
    public event Action CancelEnded;

    private void Awake()
    {
        if (InputSystem.actions == null)
        {
            Debug.LogWarning("InputSystem.actions is null");
            return;
        }

        _playerMap = InputSystem.actions.FindActionMap(_playerMapName);
        _playerMap?.Enable();

        _moveAction = FindAction(_moveActionName);
        _aimAction = FindAction(_aimActionName);
        _interactAction = FindAction(_interactActionName);
        _jumpAction = FindAction(_jumpActionName);
        _captureAction = FindAction(_captureActionName);
        _attackAction = FindAction(_attackActionName);
        _cancelAction = FindAction(_cancelActionName);

        BindButton(_interactAction, OnInteractStarted, OnInteractEnded);
        BindButton(_jumpAction, OnJumpStarted, OnJumpEnded);
        BindButton(_captureAction, OnCaptureStarted, OnCaptureEnded);
        BindButton(_attackAction, OnAttackStarted, OnAttackEnded);
        BindButton(_cancelAction, OnCancelStarted, OnCancelEnded);
        _mainCamera = Camera.main;
    }

    private void OnDestroy()
    {
        UnbindButton(_interactAction, OnInteractStarted, OnInteractEnded);
        UnbindButton(_jumpAction, OnJumpStarted, OnJumpEnded);
        UnbindButton(_captureAction, OnCaptureStarted, OnCaptureEnded);
        UnbindButton(_attackAction, OnAttackStarted, OnAttackEnded);
        UnbindButton(_cancelAction, OnCancelStarted, OnCancelEnded);
    }

    private void Update()
    {
        if (!InputEnabled)
        {
            MoveInput = Vector2.zero;
            AimInput = Vector2.zero;
            return;
        }

        MoveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
        AimInput = _aimAction == null ? Vector2.zero : GetAimInput();
    }

    public void SetInputEnabled(bool enabled)
    {
        if (InputEnabled == enabled)
        {
            return;
        }

        InputEnabled = enabled;
        if (!enabled)
        {
            EndHeldActions();
            _playerMap?.Disable();
            ResetValueInputs();
            return;
        }

        _playerMap?.Enable();
    }

    public void ResetInputState()
    {
        EndHeldActions();
        ResetValueInputs();
    }

    private InputAction FindAction(string actionName)
    {
        InputAction action = _playerMap?.FindAction(actionName, false);
        if (action == null)
        {
            Debug.LogError(
                $"PlayerInputHandler: Input action '{_playerMapName}/{actionName}' was not found.",
                this);
        }

        return action;
    }

    private static void BindButton(
        InputAction action,
        Action<InputAction.CallbackContext> started,
        Action<InputAction.CallbackContext> canceled)
    {
        if (action == null) return;
        action.started += started;
        action.canceled += canceled;
    }

    private static void UnbindButton(
        InputAction action,
        Action<InputAction.CallbackContext> started,
        Action<InputAction.CallbackContext> canceled)
    {
        if (action == null) return;
        action.started -= started;
        action.canceled -= canceled;
    }

    private void OnInteractStarted(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;
        InteractHeld = true;
        InteractStarted?.Invoke();
    }

    private void OnInteractEnded(InputAction.CallbackContext context)
    {
        if (!InteractHeld) return;
        InteractHeld = false;
        InteractEnded?.Invoke();
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;
        JumpHeld = true;
        JumpStarted?.Invoke();
    }

    private void OnJumpEnded(InputAction.CallbackContext context)
    {
        if (!JumpHeld) return;
        JumpHeld = false;
        JumpEnded?.Invoke();
    }

    private void OnCaptureStarted(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;
        CaptureHeld = true;
        CaptureStarted?.Invoke();
    }

    private void OnCaptureEnded(InputAction.CallbackContext context)
    {
        if (!CaptureHeld) return;
        CaptureHeld = false;
        CaptureEnded?.Invoke();
    }

    private void OnAttackStarted(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;
        AttackHeld = true;
        AttackStarted?.Invoke();
    }

    private void OnAttackEnded(InputAction.CallbackContext context)
    {
        if (!AttackHeld) return;
        AttackHeld = false;
        AttackEnded?.Invoke();
    }

    private void OnCancelStarted(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;
        CancelHeld = true;
        CancelStarted?.Invoke();
    }

    private void OnCancelEnded(InputAction.CallbackContext context)
    {
        if (!CancelHeld) return;
        CancelHeld = false;
        CancelEnded?.Invoke();
    }

    private void EndHeldActions()
    {
        OnInteractEnded(default);
        OnJumpEnded(default);
        OnCaptureEnded(default);
        OnAttackEnded(default);
        OnCancelEnded(default);
    }

    private void ResetValueInputs()
    {
        MoveInput = Vector2.zero;
        AimInput = Vector2.zero;
    }

    private Vector2 GetAimInput()
    {
        if (_aimAction.activeControl == null) return default;
        if (_aimAction.activeControl.device is not Pointer)
        {
            return _aimAction.ReadValue<Vector2>();
        }

        Vector2 mouseScreenPosition = _aimAction.ReadValue<Vector2>();
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

        Vector2 diff =
            (Vector2)mouseWorldPosition - (Vector2)transform.position;

        float distance = diff.magnitude;
        return distance > _mouseAimThreshold
            ? diff.normalized
            : diff / _mouseAimThreshold;
    }
}
