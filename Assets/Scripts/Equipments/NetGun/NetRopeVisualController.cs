using UnityEngine;

/// <summary>
/// Draws the ropes between a launched net and its firing point, and keeps the
/// net's local down direction facing the firing point.
/// </summary>
public sealed class NetRopeVisualController : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private Transform _ropePoints;
    [SerializeField] private SpriteRenderer _netSprite;

    [Header("Rope Appearance")]
    [SerializeField] private Material _ropeMaterial;
    [SerializeField, Min(0.001f)] private float _ropeWidth = 0.035f;
    [SerializeField] private Color _ropeColor = Color.white;
    [SerializeField] private int _sortingOrderOffset = -1;

    [Header("Orientation")]
    [SerializeField, Min(0.001f)] private float _rotationSmoothTime = 0.12f;

    private Transform _shootOrigin;
    private Transform[] _points;
    private LineRenderer[] _ropeLines;
    private float _rotationVelocity;

    private void Awake()
    {
        if (_ropePoints == null) _ropePoints = transform.Find("RopePoints");
        if (_netSprite == null) _netSprite = GetComponentInChildren<SpriteRenderer>();
        CreateRopeLines();
        SetRopesVisible(false);
    }

    private void OnDisable()
    {
        _shootOrigin = null;
        _rotationVelocity = 0f;
        SetRopesVisible(false);
    }

    private void LateUpdate()
    {
        if (_shootOrigin == null || _ropeLines == null) return;

        UpdateOrientation();
        UpdateRopePositions();
    }

    public void SetShootOrigin(Transform shootOrigin)
    {
        _shootOrigin = shootOrigin;
        _rotationVelocity = 0f;
        SetRopesVisible(_shootOrigin != null);
    }

    public void ClearShootOrigin()
    {
        _shootOrigin = null;
        _rotationVelocity = 0f;
        SetRopesVisible(false);
    }

    private void CreateRopeLines()
    {
        if (_ropePoints == null || _ropePoints.childCount == 0) return;

        int pointCount = _ropePoints.childCount;
        _points = new Transform[pointCount];
        _ropeLines = new LineRenderer[pointCount];

        Material material = _ropeMaterial != null
            ? _ropeMaterial
            : _netSprite != null ? _netSprite.sharedMaterial : null;

        int sortingLayerId = _netSprite != null ? _netSprite.sortingLayerID : 0;
        int sortingOrder = (_netSprite != null ? _netSprite.sortingOrder : 0) + _sortingOrderOffset;

        for (int i = 0; i < pointCount; i++)
        {
            _points[i] = _ropePoints.GetChild(i);

            GameObject ropeObject = new($"Rope {i + 1:00}");
            ropeObject.layer = gameObject.layer;
            ropeObject.transform.SetParent(transform, false);

            LineRenderer ropeLine = ropeObject.AddComponent<LineRenderer>();
            ropeLine.useWorldSpace = true;
            ropeLine.positionCount = 2;
            ropeLine.startWidth = _ropeWidth;
            ropeLine.endWidth = _ropeWidth;
            ropeLine.startColor = _ropeColor;
            ropeLine.endColor = _ropeColor;
            ropeLine.numCapVertices = 2;
            ropeLine.alignment = LineAlignment.View;
            ropeLine.sortingLayerID = sortingLayerId;
            ropeLine.sortingOrder = sortingOrder;
            if (material != null) ropeLine.sharedMaterial = material;

            _ropeLines[i] = ropeLine;
        }
    }

    private void UpdateOrientation()
    {
        Vector2 awayFromGun = (Vector2)transform.position - (Vector2)_shootOrigin.position;
        if (awayFromGun.sqrMagnitude <= Mathf.Epsilon) return;

        float targetAngle = Mathf.Atan2(awayFromGun.y, awayFromGun.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = transform.eulerAngles.z;
        float smoothedAngle = Mathf.SmoothDampAngle(
            currentAngle,
            targetAngle,
            ref _rotationVelocity,
            _rotationSmoothTime);

        transform.rotation = Quaternion.Euler(0f, 0f, smoothedAngle);
    }

    private void UpdateRopePositions()
    {
        Vector3 shootPosition = _shootOrigin.position;
        for (int i = 0; i < _ropeLines.Length; i++)
        {
            if (_ropeLines[i] == null || _points[i] == null) continue;
            _ropeLines[i].SetPosition(0, shootPosition);
            _ropeLines[i].SetPosition(1, _points[i].position);
        }
    }

    private void SetRopesVisible(bool visible)
    {
        if (_ropeLines == null) return;
        for (int i = 0; i < _ropeLines.Length; i++)
        {
            if (_ropeLines[i] != null) _ropeLines[i].enabled = visible;
        }
    }
}
