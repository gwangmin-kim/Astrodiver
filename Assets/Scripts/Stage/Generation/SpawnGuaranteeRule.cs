using UnityEngine;

internal sealed class SpawnGuaranteeRule
{
    private readonly RectInt _air;
    private readonly RectInt _floor;
    private readonly bool _guaranteeFloor;
    private readonly int _airTransitionCells;
    private readonly int _floorTransitionCells;
    private readonly float _density;

    public SpawnGuaranteeRule(RectInt air, RectInt floor, bool guaranteeFloor,
        int airTransitionCells, int floorTransitionCells, float density)
    {
        _air = air;
        _floor = floor;
        _guaranteeFloor = guaranteeFloor;
        _airTransitionCells = airTransitionCells;
        _floorTransitionCells = floorTransitionCells;
        _density = density;
    }

    public static bool TryGetFloorBounds(RectInt air, bool guaranteeFloor,
        int width, int thickness, out RectInt floor)
    {
        floor = default;
        if (!guaranteeFloor)
            return true;
        if (width < 1 || thickness < 1)
            return false;

        long floorMinX = (long)System.Math.Floor(
            (air.xMin + (double)air.xMax - width) * 0.5d);
        long floorMinY = (long)air.yMin - thickness;
        if (floorMinX < int.MinValue || floorMinY < int.MinValue ||
            floorMinX + width > int.MaxValue)
            return false;

        floor = new RectInt((int)floorMinX, (int)floorMinY, width, thickness);
        return true;
    }

    public void Apply(StageGenerationBuffer buffer)
    {
        BoundsInt bounds = buffer.Bounds;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                int index = buffer.Index(x, y);
                if (_air.Contains(new Vector2Int(x, y)))
                {
                    buffer.Solid[index] = false;
                }
                else if (_guaranteeFloor && _floor.Contains(new Vector2Int(x, y)))
                {
                    buffer.Solid[index] = true;
                }
                else if (y >= _air.yMin && _airTransitionCells > 0)
                {
                    float distance = DistanceToRect(x, y, _air);
                    if (distance < _airTransitionCells)
                    {
                        float t = SmoothDistance(distance, _airTransitionCells);
                        buffer.Solid[index] = StageGenerationBuffer.IsSolid(
                            buffer.Field[index], Mathf.Lerp(0f, _density, t));
                    }
                }
                else if (_guaranteeFloor && y < _air.yMin && _floorTransitionCells > 0)
                {
                    float distance = DistanceToRect(x, y, _floor);
                    if (distance < _floorTransitionCells)
                    {
                        float t = SmoothDistance(distance, _floorTransitionCells);
                        buffer.Solid[index] = StageGenerationBuffer.IsSolid(
                            buffer.Field[index], Mathf.Lerp(1f, _density, t));
                    }
                }
            }
        }
    }

    private static float SmoothDistance(float distance, int width)
    {
        float t = Mathf.Clamp01(distance / width);
        return t * t * (3f - 2f * t);
    }

    private static float DistanceToRect(int x, int y, RectInt rect)
    {
        float centerX = x + 0.5f;
        float centerY = y + 0.5f;
        float dx = Mathf.Max(rect.xMin - centerX, 0f, centerX - rect.xMax);
        float dy = Mathf.Max(rect.yMin - centerY, 0f, centerY - rect.yMax);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
