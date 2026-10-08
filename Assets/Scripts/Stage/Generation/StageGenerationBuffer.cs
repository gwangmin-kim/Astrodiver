using UnityEngine;

internal sealed class StageGenerationBuffer
{
    public StageGenerationBuffer(BoundsInt bounds)
    {
        Bounds = bounds;
        CellCount = bounds.size.x * bounds.size.y;
        Field = new float[CellCount];
        Solid = new bool[CellCount];
    }

    public BoundsInt Bounds { get; }
    public int CellCount { get; }
    public float[] Field { get; }
    public bool[] Solid { get; }

    public int Index(int x, int y) =>
        (y - Bounds.yMin) * Bounds.size.x + x - Bounds.xMin;

    public void Classify(float density)
    {
        for (int i = 0; i < CellCount; i++)
            Solid[i] = IsSolid(Field[i], density);
    }

    public static bool IsSolid(float field, float threshold) =>
        threshold >= 1f || (threshold > 0f && field < threshold);
}
