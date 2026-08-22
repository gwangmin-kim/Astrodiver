using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds a purely visual rotation to a stage button's Image and draws the
/// connection from its preceding stage while this destination is unlocked.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class StageDestinationVisualUI : BaseMeshEffect
{
    [Header("Visual Rotation")]
    [SerializeField, Min(0f)] private float _rotationSpeedDegrees = 8f;

    [Header("Previous Stage Connection")]
    [SerializeField] private Button _previousStageButton;
    [SerializeField, Min(1f)] private float _connectionWidth = 6f;
    [SerializeField] private Color _connectionColor = new(0.25f, 0.8f, 1f, 0.85f);

    private const string ConnectionName = "Stage Connection";

    private Button _button;
    private RectTransform _rectTransform;
    private Image _connectionLine;
    private float _rotationDegrees;

    protected override void Awake()
    {
        base.Awake();
        _button = GetComponent<Button>();
        _rectTransform = transform as RectTransform;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureConnectionLine();
        RefreshConnection();
        graphic?.SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_connectionLine != null)
        {
            _connectionLine.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (_rotationSpeedDegrees > 0f)
        {
            _rotationDegrees = Mathf.Repeat(
                _rotationDegrees + _rotationSpeedDegrees * Time.unscaledDeltaTime,
                360f);
            graphic?.SetVerticesDirty();
        }
    }

    private void LateUpdate()
    {
        RefreshConnection();
    }

    public override void ModifyMesh(VertexHelper vertexHelper)
    {
        if (!IsActive() || Mathf.Approximately(_rotationDegrees, 0f))
        {
            return;
        }

        Rect rect = graphic.rectTransform.rect;
        Vector2 center = rect.center;
        float radians = _rotationDegrees * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);

        UIVertex vertex = default;
        for (int i = 0; i < vertexHelper.currentVertCount; i++)
        {
            vertexHelper.PopulateUIVertex(ref vertex, i);
            Vector2 offset = (Vector2)vertex.position - center;
            vertex.position = center + new Vector2(
                offset.x * cosine - offset.y * sine,
                offset.x * sine + offset.y * cosine);
            vertexHelper.SetUIVertex(vertex, i);
        }
    }

    private void EnsureConnectionLine()
    {
        if (_connectionLine != null || _previousStageButton == null ||
            _rectTransform == null || _rectTransform.parent is not RectTransform parent)
        {
            return;
        }

        GameObject lineObject = new(ConnectionName, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(parent, false);
        lineObject.transform.SetSiblingIndex(0);

        _connectionLine = lineObject.GetComponent<Image>();
        _connectionLine.raycastTarget = false;
        _connectionLine.color = _connectionColor;
    }

    private void RefreshConnection()
    {
        EnsureConnectionLine();
        if (_connectionLine == null)
        {
            return;
        }

        bool visible = isActiveAndEnabled && _button != null &&
            _button.gameObject.activeSelf && _previousStageButton != null;
        if (_connectionLine.gameObject.activeSelf != visible)
        {
            _connectionLine.gameObject.SetActive(visible);
        }

        if (!visible || _rectTransform == null ||
            _previousStageButton.transform is not RectTransform previousRect ||
            _connectionLine.transform.parent is not RectTransform coordinateSpace)
        {
            return;
        }

        Vector2 start = coordinateSpace.InverseTransformPoint(previousRect.position);
        Vector2 end = coordinateSpace.InverseTransformPoint(_rectTransform.position);
        Vector2 direction = end - start;
        RectTransform lineRect = (RectTransform)_connectionLine.transform;
        lineRect.anchoredPosition = (start + end) * 0.5f;
        lineRect.sizeDelta = new Vector2(direction.magnitude, _connectionWidth);
        lineRect.localRotation = Quaternion.Euler(
            0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        _connectionLine.color = _connectionColor;
    }
}
