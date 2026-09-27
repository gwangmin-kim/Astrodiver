using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-1100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(StageMap))]
public sealed class PerlinStageGenerator : MonoBehaviour
{
    private const int MaxCellCount = 1000000;

    [Serializable]
    private sealed class NoiseLayer
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField, Min(0.001f)] private float _horizontalScale;
        [SerializeField, Min(0.001f)] private float _verticalScale;
        [SerializeField, Range(0f, 1f)] private float _amplitude;

        public NoiseLayer(float horizontalScale, float verticalScale)
        {
            _horizontalScale = horizontalScale;
            _verticalScale = verticalScale;
        }

        public bool IsActive => _enabled && _amplitude > 0f;

        public bool IsValid => !_enabled ||
            (IsFinitePositive(_horizontalScale) &&
             IsFinitePositive(_verticalScale) &&
             IsFinite(_amplitude) &&
             _amplitude >= 0f && _amplitude <= 1f);

        public float Sample(float x, float y, Vector2 offset)
        {
            if (!IsActive)
                return 0f;

            float noise = Mathf.PerlinNoise(
                x / _horizontalScale + offset.x,
                y / _verticalScale + offset.y);
            return _amplitude * (noise - 0.5f);
        }
    }

    [SerializeField] private WorldBounds2D _worldBounds;
    [SerializeField] private MiningTileDefinition _terrainTile;
    [Header("Base Noise")]
    [SerializeField, Min(0.001f)] private float _horizontalScale = 12f;
    [SerializeField, Min(0.001f)] private float _verticalScale = 12f;
    [SerializeField, Range(0f, 1f)] private float _density = 0.5f;
    [Header("Additional Noise")]
    [SerializeField] private NoiseLayer _broadLayer = new(24f, 16f);
    [SerializeField] private NoiseLayer _detailLayer = new(4f, 3f);
    [Header("Seed")]
    [SerializeField] private bool _useFixedSeed;
    [SerializeField] private int _fixedSeed = 12345;

    private void Awake()
    {
        StageMap stageMap = GetComponent<StageMap>();
        if (stageMap == null || _worldBounds == null || _terrainTile == null)
        {
            Debug.LogError("PerlinStageGenerator: StageMap, WorldBounds2D, and terrain tile are required.", this);
            return;
        }

        if (!stageMap.TryValidate(out string mapError))
        {
            Debug.LogError($"PerlinStageGenerator: {mapError}", this);
            return;
        }

        if (!IsFinitePositive(_horizontalScale) || !IsFinitePositive(_verticalScale) ||
            float.IsNaN(_density) || _density < 0f || _density > 1f)
        {
            Debug.LogError("PerlinStageGenerator: Scales must be finite and positive; density must be within 0..1.", this);
            return;
        }

        if (_broadLayer == null || !_broadLayer.IsValid ||
            _detailLayer == null || !_detailLayer.IsValid)
        {
            Debug.LogError("PerlinStageGenerator: Enabled noise layers need finite positive scales and amplitude within 0..1.", this);
            return;
        }

        Grid grid = stageMap.Grid;
        if (grid.cellLayout != GridLayout.CellLayout.Rectangle ||
            grid.cellSwizzle != GridLayout.CellSwizzle.XYZ ||
            grid.cellGap != Vector3.zero ||
            _worldBounds.transform.rotation != Quaternion.identity)
        {
            Debug.LogError("PerlinStageGenerator: Only axis-aligned world bounds and a unit rectangular Grid are supported.", this);
            return;
        }

        Vector2 worldMin = _worldBounds.WorldMin;
        Vector2 worldMax = _worldBounds.WorldMax;
        if (!IsFinite(worldMin.x) || !IsFinite(worldMin.y) ||
            !IsFinite(worldMax.x) || !IsFinite(worldMax.y))
        {
            Debug.LogError("PerlinStageGenerator: World bounds must be finite.", this);
            return;
        }

        long expandedMinX = (long)Mathf.FloorToInt(worldMin.x) - 1;
        long expandedMinY = (long)Mathf.FloorToInt(worldMin.y) - 1;
        long expandedMaxX = (long)Mathf.CeilToInt(worldMax.x) + 1;
        long expandedMaxY = (long)Mathf.CeilToInt(worldMax.y) + 1;
        long width = expandedMaxX - expandedMinX;
        long height = expandedMaxY - expandedMinY;
        if (expandedMinX < int.MinValue || expandedMinY < int.MinValue ||
            expandedMaxX > int.MaxValue || expandedMaxY > int.MaxValue ||
            width <= 0 || height <= 0 ||
            width > MaxCellCount || height > MaxCellCount ||
            width * height > MaxCellCount)
        {
            Debug.LogError($"PerlinStageGenerator: Cell area must contain 1..{MaxCellCount} cells.", this);
            return;
        }

        int minX = (int)expandedMinX;
        int minY = (int)expandedMinY;
        int maxX = (int)expandedMaxX;
        int maxY = (int)expandedMaxY;
        int seed = _useFixedSeed ? _fixedSeed : Guid.NewGuid().GetHashCode();
        System.Random random = new(seed);
        float offsetX = random.Next(-100000, 100001);
        float offsetY = random.Next(-100000, 100001);
        Vector2 broadOffset = new(
            random.Next(-100000, 100001),
            random.Next(-100000, 100001));
        Vector2 detailOffset = new(
            random.Next(-100000, 100001),
            random.Next(-100000, 100001));
        TileBase[] tiles = new TileBase[(int)(width * height)];
        for (int y = minY; y < maxY; y++)
        {
            int row = (int)((y - (long)minY) * width);
            for (int x = minX; x < maxX; x++)
            {
                float sampleX = x + 0.5f;
                float sampleY = y + 0.5f;
                float baseNoise = Mathf.PerlinNoise(
                    sampleX / _horizontalScale + offsetX,
                    sampleY / _verticalScale + offsetY);
                float field = Mathf.Clamp01(baseNoise +
                    _broadLayer.Sample(sampleX, sampleY, broadOffset) +
                    _detailLayer.Sample(sampleX, sampleY, detailOffset));
                if (_density >= 1f || (_density > 0f && field < _density))
                    tiles[row + x - minX] = _terrainTile;
            }
        }

        BoundsInt cellBounds = new(minX, minY, 0, (int)width, (int)height, 1);
        stageMap.ApplyGeneratedTiles(cellBounds, tiles);
        Debug.Log($"PerlinStageGenerator: Generated {width}x{height} cells with seed {seed}.", this);
    }

    private static bool IsFinitePositive(float value) => IsFinite(value) && value > 0f;
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
