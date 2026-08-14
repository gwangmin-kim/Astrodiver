using UnityEngine;

[DisallowMultipleComponent]
public sealed class UpgradeTooltipPositioner : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _slotGap = 18f;
    [SerializeField, Min(0f)] private float _screenPadding = 16f;

    private readonly Vector3[] _worldCorners = new Vector3[4];
    private RectTransform _target;

    public void SetTarget(RectTransform target)
    {
        _target = target;
        UpdatePosition();
    }

    public void ClearTarget()
    {
        _target = null;
    }

    private void LateUpdate()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        RectTransform tooltip = (RectTransform)transform;
        RectTransform container = transform.parent as RectTransform;
        if (_target == null || container == null)
        {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        _target.GetWorldCorners(_worldCorners);

        float slotMinX = float.PositiveInfinity;
        float slotMaxX = float.NegativeInfinity;
        float slotMinY = float.PositiveInfinity;
        float slotMaxY = float.NegativeInfinity;
        for (int i = 0; i < _worldCorners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                _worldCorners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    container,
                    screen,
                    eventCamera,
                    out Vector2 local))
            {
                continue;
            }

            slotMinX = Mathf.Min(slotMinX, local.x);
            slotMaxX = Mathf.Max(slotMaxX, local.x);
            slotMinY = Mathf.Min(slotMinY, local.y);
            slotMaxY = Mathf.Max(slotMaxY, local.y);
        }

        if (float.IsInfinity(slotMinX))
        {
            return;
        }

        bool placeRight = container.rect.xMax - slotMaxX >=
            slotMinX - container.rect.xMin;
        Vector2 pivot = new(placeRight ? 0f : 1f, 0.5f);
        tooltip.pivot = pivot;

        Vector2 position = new(
            placeRight ? slotMaxX + _slotGap : slotMinX - _slotGap,
            (slotMinY + slotMaxY) * 0.5f);
        Vector2 size = tooltip.rect.size;
        float minX = position.x - size.x * pivot.x;
        float maxX = minX + size.x;
        float minY = position.y - size.y * pivot.y;
        float maxY = minY + size.y;
        float allowedMinX = container.rect.xMin + _screenPadding;
        float allowedMaxX = container.rect.xMax - _screenPadding;
        float allowedMinY = container.rect.yMin + _screenPadding;
        float allowedMaxY = container.rect.yMax - _screenPadding;

        if (minX < allowedMinX) position.x += allowedMinX - minX;
        if (maxX > allowedMaxX) position.x -= maxX - allowedMaxX;
        if (minY < allowedMinY) position.y += allowedMinY - minY;
        if (maxY > allowedMaxY) position.y -= maxY - allowedMaxY;
        tooltip.anchoredPosition = position;
    }
}
