using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class UpgradeTreeNavigationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIInputHandler _input;
    [SerializeField] private RectTransform _viewport;
    [SerializeField] private RectTransform _treeContent;

    [Header("Pan")]
    [SerializeField, Min(0f)] private float _panSensitivity = 1f;
    [SerializeField] private Vector2 _minPanPosition = new(-600f, -350f);
    [SerializeField] private Vector2 _maxPanPosition = new(600f, 350f);

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

    private Camera EventCamera =>
        _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        ResetInteraction();
        if (_hasStarted)
        {
            _input.SetInputEnabled(true);
        }
    }

    private void Start()
    {
        _hasStarted = true;
        _input.SetInputEnabled(true);
        SetZoom(_treeContent.localScale.x);
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
        RectTransform parent = _treeContent.parent as RectTransform;
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

        _treeContent.anchoredPosition +=
            (currentLocal - previousLocal) * _panSensitivity;
        ClampPanPosition();
    }

    private void ZoomAtPointer(float scrollAmount)
    {
        float currentZoom = _treeContent.localScale.x;
        float zoomMultiplier = Mathf.Exp(scrollAmount * _zoomSensitivity);
        float targetZoom = Mathf.Clamp(
            currentZoom * zoomMultiplier,
            Mathf.Min(_minZoom, _maxZoom),
            Mathf.Max(_minZoom, _maxZoom));

        if (Mathf.Approximately(currentZoom, targetZoom) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _treeContent,
                _input.PointerPosition,
                EventCamera,
                out Vector2 localPointer))
        {
            return;
        }

        SetZoom(targetZoom);
        _treeContent.anchoredPosition -=
            localPointer * (targetZoom - currentZoom);
        ClampPanPosition();
    }

    private void SetZoom(float zoom)
    {
        float clampedZoom = Mathf.Clamp(
            zoom,
            Mathf.Min(_minZoom, _maxZoom),
            Mathf.Max(_minZoom, _maxZoom));
        _treeContent.localScale = new Vector3(clampedZoom, clampedZoom, 1f);
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
        Vector2 min = Vector2.Min(_minPanPosition, _maxPanPosition);
        Vector2 max = Vector2.Max(_minPanPosition, _maxPanPosition);
        Vector2 position = _treeContent.anchoredPosition;
        position.x = Mathf.Clamp(position.x, min.x, max.x);
        position.y = Mathf.Clamp(position.y, min.y, max.y);
        _treeContent.anchoredPosition = position;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!_showPanBoundsGizmo || _treeContent == null)
        {
            return;
        }

        RectTransform parent = _treeContent.parent as RectTransform;
        if (parent == null)
        {
            return;
        }

        Vector2 min = Vector2.Min(_minPanPosition, _maxPanPosition);
        Vector2 max = Vector2.Max(_minPanPosition, _maxPanPosition);
        Vector2 normalizedAnchor =
            (_treeContent.anchorMin + _treeContent.anchorMax) * 0.5f;
        Vector2 anchorPosition = new(
            Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, normalizedAnchor.x),
            Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, normalizedAnchor.y));

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
