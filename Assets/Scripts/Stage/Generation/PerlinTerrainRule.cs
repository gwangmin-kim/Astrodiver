using UnityEngine;

internal sealed class PerlinTerrainRule
{
    private readonly float _horizontalScale;
    private readonly float _verticalScale;
    private readonly NoiseLayerParameters _broadLayer;
    private readonly NoiseLayerParameters _detailLayer;
    private readonly float _offsetX;
    private readonly float _offsetY;
    private readonly Vector2 _broadOffset;
    private readonly Vector2 _detailOffset;

    public PerlinTerrainRule(float horizontalScale, float verticalScale,
        StageGenerator.NoiseLayer broadLayer, StageGenerator.NoiseLayer detailLayer, int seed)
    {
        _horizontalScale = horizontalScale;
        _verticalScale = verticalScale;
        _broadLayer = new NoiseLayerParameters(broadLayer);
        _detailLayer = new NoiseLayerParameters(detailLayer);
        System.Random random = new(seed);
        _offsetX = random.Next(-100000, 100001);
        _offsetY = random.Next(-100000, 100001);
        _broadOffset = new Vector2(random.Next(-100000, 100001), random.Next(-100000, 100001));
        _detailOffset = new Vector2(random.Next(-100000, 100001), random.Next(-100000, 100001));
    }

    public void Apply(StageGenerationBuffer buffer)
    {
        BoundsInt bounds = buffer.Bounds;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                float sampleX = x + 0.5f;
                float sampleY = y + 0.5f;
                float baseNoise = Mathf.PerlinNoise(
                    sampleX / _horizontalScale + _offsetX,
                    sampleY / _verticalScale + _offsetY);
                buffer.Field[buffer.Index(x, y)] = Mathf.Clamp01(baseNoise +
                    SampleLayer(_broadLayer, sampleX, sampleY, _broadOffset) +
                    SampleLayer(_detailLayer, sampleX, sampleY, _detailOffset));
            }
        }
    }

    private static float SampleLayer(NoiseLayerParameters layer,
        float x, float y, Vector2 offset)
    {
        if (!layer.IsActive)
            return 0f;

        float noise = Mathf.PerlinNoise(
            x / layer.HorizontalScale + offset.x,
            y / layer.VerticalScale + offset.y);
        return layer.Amplitude * (noise - 0.5f);
    }

    private readonly struct NoiseLayerParameters
    {
        public NoiseLayerParameters(StageGenerator.NoiseLayer layer)
        {
            IsActive = layer.IsActive;
            HorizontalScale = layer.HorizontalScale;
            VerticalScale = layer.VerticalScale;
            Amplitude = layer.Amplitude;
        }

        public bool IsActive { get; }
        public float HorizontalScale { get; }
        public float VerticalScale { get; }
        public float Amplitude { get; }
    }
}
