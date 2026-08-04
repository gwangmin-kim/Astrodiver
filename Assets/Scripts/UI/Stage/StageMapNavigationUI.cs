using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class StageMapNavigationUI : MonoBehaviour
{
    private const int DefaultButtonBoundsPadding = 300;

    [Header("References")]
    [SerializeField] private UIInputHandler _input;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _mapContent;

    [Header("Pan")]
    [SerializeField, Min(0f)] private float _panSensitivity = 1f;

    [Header("Automatic Pan Bounds")]
    [SerializeField] private RectOffset _buttonBoundsPadding;

    [Header("Editor Visualization")]
    [SerializeField] private bool _showPanBoundsGizmo = true;
    [SerializeField] private Color _panBoundsGizmoColor = new(0.2f, 0.85f, 1f, 0.9f);

    [Header("Zoom")]
    [SerializeField, Range(0.01f, 1f)] private float _minZoom = 0.3f;
    [SerializeField, Range(1f, 3f)] private float _maxZoom = 2.5f;
    [SerializeField, Min(0.0001f)] private float _zoomSensitivity = 0.1f;

    private Canvas _canvas;
    private bool _dragging;
    private bool _previousRightClickHeld;
    private bool _previousMiddleClickHeld;
    private bool _hasStarted;

    private readonly Vector3[] _worldCorners = new Vector3[4];
    private readonly Vector3[] _viewportWorldCorners = new Vector3[4];
    private readonly List<Button> _buttons = new();
    private bool _missingButtonsWarningLogged;

    private Camera EventCamera =>
        _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;

    private void Awake()
    {
        EnsureButtonBoundsPadding();
        _canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        EnsureButtonBoundsPadding();
        ResetInteraction();
        RefreshButtonTargets();
        if (_hasStarted)
        {
            _input?.SetInputEnabled(true);
            ClampPanPosition();
        }
    }

    private void Start()
    {
        _hasStarted = true;
        _input?.SetInputEnabled(true);
        Canvas.ForceUpdateCanvases();
        RefreshButtonTargets();
        SetZoom(_mapContent.localScale.x);
        ClampPanPosition();
    }

    private void OnDisable()
    {
        ResetInteraction();
        if (_hasStarted && _input != null)
        {
            _input.SetInputEnabled(false);
        }
    }

    private void Update()
    {
        if (_input == null || !_input.InputEnabled)
        {
            return;
        }

        bool rightClickHeld = _input.RightClickHeld;
        bool middleClickHeld = _input.MiddleClickHeld;
        bool dragPressedThisFrame =
            rightClickHeld && !_previousRightClickHeld ||
            middleClickHeld && !_previousMiddleClickHeld;
        bool dragHeld = rightClickHeld || middleClickHeld;

        if (dragPressedThisFrame)
        {
            _dragging = IsPointerInsideViewport();
        }
        else if (!dragHeld)
        {
            _dragging = false;
        }

        if (_dragging)
        {
            Pan(_input.PointerDelta);
        }

        if (_input.ScrollDelta.y != 0f && IsPointerInsideViewport())
        {
            ZoomAtPointer(_input.ScrollDelta.y);
        }

        _previousRightClickHeld = rightClickHeld;
        _previousMiddleClickHeld = middleClickHeld;
    }

    private void Pan(Vector2 screenDelta)
    {
        RectTransform parent = _mapContent.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        Vector2 currentScreenPosition = _input.PointerPosition;
        Vector2 previousScreenPosition = currentScreenPosition - screenDelta;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                previousScreenPosition,
                EventCamera,
                out Vector2 previousLocal) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                currentScreenPosition,
                EventCamera,
                out Vector2 currentLocal))
        {
            return;
        }

        _mapContent.anchoredPosition +=
            (currentLocal - previousLocal) * _panSensitivity;
        ClampPanPosition();
    }

    private void ZoomAtPointer(float scrollAmount)
    {
        float currentZoom = _mapContent.localScale.x;
        float zoomMultiplier = Mathf.Exp(scrollAmount * _zoomSensitivity);
        float targetZoom = Mathf.Clamp(
            currentZoom * zoomMultiplier,
            Mathf.Min(_minZoom, _maxZoom),
            Mathf.Max(_minZoom, _maxZoom));

        if (Mathf.Approximately(currentZoom, targetZoom) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapContent,
                _input.PointerPosition,
                EventCamera,
                out Vector2 localPointer))
        {
            return;
        }

        SetZoom(targetZoom);
        _mapContent.anchoredPosition -=
            localPointer * (targetZoom - currentZoom);
        ClampPanPosition();
    }

    private void SetZoom(float zoom)
    {
        float clampedZoom = Mathf.Clamp(
            zoom,
            Mathf.Min(_minZoom, _maxZoom),
            Mathf.Max(_minZoom, _maxZoom));
        _mapContent.localScale = new Vector3(clampedZoom, clampedZoom, 1f);
    }

    private bool IsPointerInsideViewport()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            _viewport,
            _input.PointerPosition,
            EventCamera);
    }

    private void ResetInteraction()
    {
        _dragging = false;
        _previousRightClickHeld = false;
        _previousMiddleClickHeld = false;
    }

    private void ClampPanPosition()
    {
        if (!TryGetPanPositionBounds(out Vector2 min, out Vector2 max))
        {
            return;
        }

        Vector2 position = _mapContent.anchoredPosition;
        position.x = Mathf.Clamp(position.x, min.x, max.x);
        position.y = Mathf.Clamp(position.y, min.y, max.y);
        _mapContent.anchoredPosition = position;
    }

    private bool TryGetPanPositionBounds(out Vector2 min, out Vector2 max)
    {
        min = default;
        max = default;

        if (_mapContent == null || _viewport == null ||
            _mapContent.parent is not RectTransform parent ||
            !TryGetButtonBounds(out Rect buttonBounds) ||
            !TryGetRectBoundsInParent(_viewport, parent, out Rect viewportBounds))
        {
            return false;
        }

        Vector2 contentScale = _mapContent.localScale;
        if (contentScale.x <= 0f || contentScale.y <= 0f)
        {
            return false;
        }

        Vector2 anchorPosition = GetAnchorPosition(parent);
        min = new Vector2(
            viewportBounds.xMax - anchorPosition.x - buttonBounds.xMax * contentScale.x,
            viewportBounds.yMax - anchorPosition.y - buttonBounds.yMax * contentScale.y);
        max = new Vector2(
            viewportBounds.xMin - anchorPosition.x - buttonBounds.xMin * contentScale.x,
            viewportBounds.yMin - anchorPosition.y - buttonBounds.yMin * contentScale.y);

        CollapseInvertedRange(ref min.x, ref max.x);
        CollapseInvertedRange(ref min.y, ref max.y);
        return true;
    }

    private bool TryGetButtonBounds(out Rect bounds)
    {
        bounds = default;
        if (_mapContent == null)
        {
            return false;
        }

        if (_buttons.Count == 0)
        {
            if (!_missingButtonsWarningLogged)
            {
                Debug.LogWarning(
                    "StageMapNavigationUI: No buttons were found under MapContent; pan is disabled.",
                    this);
                _missingButtonsWarningLogged = true;
            }

            return false;
        }

        bool hasBounds = false;
        Vector2 min = default;
        Vector2 max = default;
        foreach (Button button in _buttons)
        {
            if (button == null || button.transform is not RectTransform buttonRect)
            {
                continue;
            }

            buttonRect.GetWorldCorners(_worldCorners);
            for (int i = 0; i < _worldCorners.Length; i++)
            {
                Vector2 point = (Vector2)_mapContent.InverseTransformPoint(_worldCorners[i]);
                if (!hasBounds)
                {
                    min = point;
                    max = point;
                    hasBounds = true;
                    continue;
                }

                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
        }

        if (!hasBounds)
        {
            return false;
        }

        int left = Mathf.Max(0, _buttonBoundsPadding.left);
        int right = Mathf.Max(0, _buttonBoundsPadding.right);
        int top = Mathf.Max(0, _buttonBoundsPadding.top);
        int bottom = Mathf.Max(0, _buttonBoundsPadding.bottom);
        bounds = Rect.MinMaxRect(
            min.x - left,
            min.y - bottom,
            max.x + right,
            max.y + top);
        return true;
    }

    [ContextMenu("Refresh Automatic Pan Bounds")]
    public void RefreshButtonTargets()
    {
        _buttons.Clear();
        _missingButtonsWarningLogged = false;

        if (_mapContent != null)
        {
            _mapContent.GetComponentsInChildren(true, _buttons);
        }

        ClampPanPosition();
    }

    private bool TryGetRectBoundsInParent(
        RectTransform rect,
        RectTransform parent,
        out Rect bounds)
    {
        rect.GetWorldCorners(_viewportWorldCorners);

        Vector2 min = (Vector2)parent.InverseTransformPoint(_viewportWorldCorners[0]);
        Vector2 max = min;
        for (int i = 1; i < _viewportWorldCorners.Length; i++)
        {
            Vector2 point = (Vector2)parent.InverseTransformPoint(_viewportWorldCorners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return true;
    }

    private Vector2 GetAnchorPosition(RectTransform parent)
    {
        Vector2 normalizedAnchor =
            (_mapContent.anchorMin + _mapContent.anchorMax) * 0.5f;
        return new Vector2(
            Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, normalizedAnchor.x),
            Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, normalizedAnchor.y));
    }

    private static void CollapseInvertedRange(ref float min, ref float max)
    {
        if (min <= max)
        {
            return;
        }

        min = max = (min + max) * 0.5f;
    }

    private void EnsureButtonBoundsPadding()
    {
        _buttonBoundsPadding ??= new RectOffset(
            DefaultButtonBoundsPadding,
            DefaultButtonBoundsPadding,
            DefaultButtonBoundsPadding,
            DefaultButtonBoundsPadding);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureButtonBoundsPadding();
    }

    private void OnDrawGizmos()
    {
        if (!_showPanBoundsGizmo || _mapContent == null ||
            !TryGetPanPositionBounds(out Vector2 min, out Vector2 max))
        {
            return;
        }

        RectTransform parent = _mapContent.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        Vector2 anchorPosition = GetAnchorPosition(parent);

        Vector3 bottomLeft = parent.TransformPoint(anchorPosition + new Vector2(min.x, min.y));
        Vector3 topLeft = parent.TransformPoint(anchorPosition + new Vector2(min.x, max.y));
        Vector3 topRight = parent.TransformPoint(anchorPosition + new Vector2(max.x, max.y));
        Vector3 bottomRight = parent.TransformPoint(anchorPosition + new Vector2(max.x, min.y));

        Gizmos.color = _panBoundsGizmoColor;
        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
    }
#endif
}
