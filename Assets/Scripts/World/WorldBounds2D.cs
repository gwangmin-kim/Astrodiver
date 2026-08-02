using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
public sealed class WorldBounds2D : MonoBehaviour
{
    private const float MinimumSize = 0.01f;

    [Header("Local AABB")]
    [SerializeField] private Vector2 _min = new(-30f, -20f);
    [SerializeField] private Vector2 _max = new(30f, 20f);

    public Vector2 LocalMin => Vector2.Min(_min, _max);
    public Vector2 LocalMax => Vector2.Max(_min, _max);
    public Vector2 LocalCenter => (LocalMin + LocalMax) * 0.5f;
    public Vector2 LocalSize => LocalMax - LocalMin;

    public Vector2 WorldMin
    {
        get
        {
            Vector2 first = transform.TransformPoint(LocalMin);
            Vector2 second = transform.TransformPoint(LocalMax);
            return Vector2.Min(first, second);
        }
    }

    public Vector2 WorldMax
    {
        get
        {
            Vector2 first = transform.TransformPoint(LocalMin);
            Vector2 second = transform.TransformPoint(LocalMax);
            return Vector2.Max(first, second);
        }
    }

    public PolygonCollider2D BoundaryCollider
    {
        get
        {
            PolygonCollider2D boundaryCollider =
                GetComponent<PolygonCollider2D>();
            SynchronizeCollider(boundaryCollider);
            return boundaryCollider;
        }
    }

    public Vector2 ClampPoint(Vector2 worldPoint)
    {
        Vector2 min = WorldMin;
        Vector2 max = WorldMax;
        return new Vector2(
            Mathf.Clamp(worldPoint.x, min.x, max.x),
            Mathf.Clamp(worldPoint.y, min.y, max.y));
    }

    public Vector2 ClampPoint(
        Vector2 worldPoint,
        Vector2 pointBoundsMin,
        Vector2 pointBoundsMax)
    {
        Vector2 worldMin = WorldMin;
        Vector2 worldMax = WorldMax;
        Vector2 normalizedBoundsMin =
            Vector2.Min(pointBoundsMin, pointBoundsMax);
        Vector2 normalizedBoundsMax =
            Vector2.Max(pointBoundsMin, pointBoundsMax);
        Vector2 allowedMin = worldMin - normalizedBoundsMin;
        Vector2 allowedMax = worldMax - normalizedBoundsMax;

        if (allowedMin.x > allowedMax.x)
        {
            allowedMin.x = allowedMax.x = (worldMin.x + worldMax.x) * 0.5f;
        }

        if (allowedMin.y > allowedMax.y)
        {
            allowedMin.y = allowedMax.y = (worldMin.y + worldMax.y) * 0.5f;
        }

        return new Vector2(
            Mathf.Clamp(worldPoint.x, allowedMin.x, allowedMax.x),
            Mathf.Clamp(worldPoint.y, allowedMin.y, allowedMax.y));
    }

    public void SetLocalBounds(Vector2 min, Vector2 max)
    {
        _min = Vector2.Min(min, max);
        _max = Vector2.Max(min, max);
        EnsureMinimumSize();
        SynchronizeCollider(GetComponent<PolygonCollider2D>());
    }

    private void Awake()
    {
        SynchronizeCollider(GetComponent<PolygonCollider2D>());
    }

    private void Reset()
    {
        EnsureMinimumSize();
        SynchronizeCollider(GetComponent<PolygonCollider2D>());
    }

    private void OnValidate()
    {
        Vector2 min = Vector2.Min(_min, _max);
        Vector2 max = Vector2.Max(_min, _max);
        _min = min;
        _max = max;
        EnsureMinimumSize();
        SynchronizeCollider(GetComponent<PolygonCollider2D>());
    }

    private void EnsureMinimumSize()
    {
        Vector2 size = _max - _min;
        if (size.x < MinimumSize)
        {
            _max.x = _min.x + MinimumSize;
        }

        if (size.y < MinimumSize)
        {
            _max.y = _min.y + MinimumSize;
        }
    }

    private void SynchronizeCollider(PolygonCollider2D boundaryCollider)
    {
        if (boundaryCollider == null)
        {
            return;
        }

        boundaryCollider.isTrigger = true;
        boundaryCollider.excludeLayers = Physics2D.AllLayers;
        Vector2 min = LocalMin;
        Vector2 max = LocalMax;
        boundaryCollider.pathCount = 1;
        boundaryCollider.SetPath(
            0,
            new[]
            {
                new Vector2(min.x, min.y),
                new Vector2(min.x, max.y),
                new Vector2(max.x, max.y),
                new Vector2(max.x, min.y)
            });
    }
}
