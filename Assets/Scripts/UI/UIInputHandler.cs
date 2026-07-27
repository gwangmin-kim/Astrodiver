using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class UIInputHandler : MonoBehaviour
{
    [Header("Input Action Settings")]
    [SerializeField] private string _uiMapName = "UI";
    [SerializeField] private string _cancelActionName = "Cancel";
    [SerializeField] private string _pointActionName = "Point";
    [SerializeField] private string _clickActionName = "Click";
    [SerializeField] private string _rightClickActionName = "RightClick";
    [SerializeField] private string _middleClickActionName = "MiddleClick";
    [SerializeField] private string _scrollWheelActionName = "ScrollWheel";

    // 입력 액션
    private InputAction _cancelAction;
    private InputAction _pointAction;
    private InputAction _clickAction;
    private InputAction _rightClickAction;
    private InputAction _middleClickAction;
    private InputAction _scrollWheelAction;

    // 캐싱된 입력값
    public Vector2 PointerPosition { get; private set; }
    public Vector2 PointerDelta { get; private set; }
    public Vector2 ScrollDelta { get; private set; }
    public bool RightClickHeld { get; private set; }
    public bool MiddleClickHeld { get; private set; }

    public event Action CancelPressed;
    public event Action ClickPressed;

    public bool InputEnabled { get; private set; } = false;

    private void Start()
    {
        _cancelAction = FindAction(_cancelActionName);
        _pointAction = FindAction(_pointActionName);
        _clickAction = FindAction(_clickActionName);
        _rightClickAction = FindAction(_rightClickActionName);
        _middleClickAction = FindAction(_middleClickActionName);
        _scrollWheelAction = FindAction(_scrollWheelActionName);

        // UI 조작은 인게임 플레이 중엔 사용하지 않음
        SetInputEnabled(false);
    }

    private InputAction FindAction(string actionName)
    {
        InputAction action = InputSystem.actions.FindAction($"{_uiMapName}/{actionName}");

        if (action != null) action.Enable();
        else Debug.LogError($"전역 Input Actions에서 '{actionName}' 액션을 찾을 수 없습니다.", this);

        return action;
    }

    public void SetInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
        ResetInputState();

        InputActionMap map = InputSystem.actions.FindActionMap(_uiMapName);
        if (map == null) return;

        if (enabled) map.Enable();
        else map.Disable();
    }

    private void ResetInputState()
    {
        PointerPosition = _pointAction?.ReadValue<Vector2>() ?? Vector2.zero;
        PointerDelta = Vector2.zero;
        ScrollDelta = Vector2.zero;
        RightClickHeld = false;
        MiddleClickHeld = false;
    }

    private void Update()
    {
        if (!InputEnabled) return;

        if (_pointAction != null)
        {
            Vector2 nextPointer = _pointAction.ReadValue<Vector2>();
            PointerDelta = nextPointer - PointerPosition;
            PointerPosition = nextPointer;
        }

        ScrollDelta = _scrollWheelAction?.ReadValue<Vector2>() ?? Vector2.zero;

        RightClickHeld = _rightClickAction?.IsPressed() ?? false;
        MiddleClickHeld = _middleClickAction?.IsPressed() ?? false;

        if (_cancelAction?.WasPressedThisFrame() ?? false) CancelPressed?.Invoke();
        if (_clickAction?.WasPressedThisFrame() ?? false) ClickPressed?.Invoke();
    }
}
