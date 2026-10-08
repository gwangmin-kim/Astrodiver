using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-1100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(StageMap))]
public sealed class StageGenerator : MonoBehaviour
{
    private const int MaxCellCount = 1000000;

    [Serializable]
    internal sealed class NoiseLayer
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
        public float HorizontalScale => _horizontalScale;
        public float VerticalScale => _verticalScale;
        public float Amplitude => _amplitude;
        public bool IsValid => !_enabled ||
            (IsFinitePositive(_horizontalScale) && IsFinitePositive(_verticalScale) &&
             IsFinite(_amplitude) && _amplitude >= 0f && _amplitude <= 1f);
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
    [Header("Spawn Guarantee")]
    [SerializeField] private BoxCollider2D _returnArea;
    [SerializeField, Min(0)] private int _airTransitionCells = 3;
    [SerializeField] private bool _guaranteeFloor = true;
    [SerializeField, Min(1)] private int _floorWidthCells = 8;
    [SerializeField, Min(1)] private int _floorThicknessCells = 2;
    [SerializeField, Min(0)] private int _floorTransitionCells = 3;

    private void Awake()
    {
        StageMap stageMap = GetComponent<StageMap>();
        if (stageMap == null || _worldBounds == null || _terrainTile == null || _returnArea == null)
        {
            Debug.LogError("StageGenerator: StageMap, WorldBounds2D, terrain tile, and ReturnArea are required.", this);
            return;
        }

        if (!stageMap.TryValidate(out string mapError))
        {
            Debug.LogError($"StageGenerator: {mapError}", this);
            return;
        }

        if (!IsFinitePositive(_horizontalScale) || !IsFinitePositive(_verticalScale) ||
            !IsFinite(_density) || _density < 0f || _density > 1f ||
            _broadLayer == null || !_broadLayer.IsValid ||
            _detailLayer == null || !_detailLayer.IsValid)
        {
            Debug.LogError("StageGenerator: Noise scales must be finite and positive; density and enabled amplitudes must be within 0..1.", this);
            return;
        }

        Grid grid = stageMap.Grid;
        if (grid.cellLayout != GridLayout.CellLayout.Rectangle ||
            grid.cellSwizzle != GridLayout.CellSwizzle.XYZ ||
            grid.cellGap != Vector3.zero ||
            _worldBounds.transform.rotation != Quaternion.identity)
        {
            Debug.LogError("StageGenerator: Only axis-aligned world bounds and a unit rectangular Grid are supported.", this);
            return;
        }

        if (!TryGetGenerationBounds(out BoundsInt cellBounds))
            return;

        if (_airTransitionCells < 0 || _floorTransitionCells < 0 ||
            (_guaranteeFloor && (_floorWidthCells < 1 || _floorThicknessCells < 1)) ||
            !_returnArea.enabled || !_returnArea.gameObject.activeInHierarchy ||
            !_returnArea.isTrigger || _returnArea.transform.rotation != Quaternion.identity)
        {
            Debug.LogError("StageGenerator: ReturnArea must be an active, axis-aligned BoxCollider2D trigger with valid guarantee settings.", this);
            return;
        }

        if (!TryGetReturnAreaBounds(out RectInt airBounds) ||
            !SpawnGuaranteeRule.TryGetFloorBounds(airBounds, _guaranteeFloor,
                _floorWidthCells, _floorThicknessCells, out RectInt floorBounds) ||
            !Contains(cellBounds, airBounds) ||
            (_guaranteeFloor && !Contains(cellBounds, floorBounds)))
        {
            Debug.LogError("StageGenerator: ReturnArea and its required floor must fit inside the generated cell bounds.", this);
            return;
        }

        int seed = _useFixedSeed ? _fixedSeed : Guid.NewGuid().GetHashCode();
        StageGenerationBuffer buffer = new(cellBounds);
        new PerlinTerrainRule(_horizontalScale, _verticalScale, _broadLayer, _detailLayer, seed)
            .Apply(buffer);
        buffer.Classify(_density);
        new SpawnGuaranteeRule(airBounds, floorBounds, _guaranteeFloor,
                _airTransitionCells, _floorTransitionCells, _density)
            .Apply(buffer);

        TileBase[] tiles = new TileBase[buffer.CellCount];
        for (int i = 0; i < tiles.Length; i++)
        {
            if (buffer.Solid[i])
                tiles[i] = _terrainTile;
        }

        stageMap.ApplyGeneratedTiles(cellBounds, tiles);
        Debug.Log($"StageGenerator: Generated {cellBounds.size.x}x{cellBounds.size.y} cells with seed {seed}.", this);
    }

    private bool TryGetGenerationBounds(out BoundsInt bounds)
    {
        bounds = default;
        Vector2 worldMin = _worldBounds.WorldMin;
        Vector2 worldMax = _worldBounds.WorldMax;
        if (!IsFinite(worldMin.x) || !IsFinite(worldMin.y) ||
            !IsFinite(worldMax.x) || !IsFinite(worldMax.y) ||
            worldMin.x < int.MinValue + 1d || worldMin.y < int.MinValue + 1d ||
            worldMax.x > int.MaxValue - 1d || worldMax.y > int.MaxValue - 1d)
        {
            Debug.LogError("StageGenerator: World bounds must be finite and within the cell index range.", this);
            return false;
        }

        long minX = (long)Mathf.FloorToInt(worldMin.x) - 1;
        long minY = (long)Mathf.FloorToInt(worldMin.y) - 1;
        long maxX = (long)Mathf.CeilToInt(worldMax.x) + 1;
        long maxY = (long)Mathf.CeilToInt(worldMax.y) + 1;
        long width = maxX - minX;
        long height = maxY - minY;
        if (minX < int.MinValue || minY < int.MinValue ||
            maxX > int.MaxValue || maxY > int.MaxValue ||
            width <= 0 || height <= 0 ||
            width > MaxCellCount || height > MaxCellCount || width * height > MaxCellCount)
        {
            Debug.LogError($"StageGenerator: Cell area must contain 1..{MaxCellCount} cells.", this);
            return false;
        }

        bounds = new BoundsInt((int)minX, (int)minY, 0, (int)width, (int)height, 1);
        return true;
    }

    private bool TryGetReturnAreaBounds(out RectInt bounds)
    {
        bounds = default;
        Vector2 halfSize = _returnArea.size * 0.5f;
        Transform areaTransform = _returnArea.transform;
        Vector2 first = areaTransform.TransformPoint(_returnArea.offset - halfSize);
        Vector2 second = areaTransform.TransformPoint(_returnArea.offset + halfSize);
        Vector2 min = Vector2.Min(first, second);
        Vector2 max = Vector2.Max(first, second);
        if (!IsFinite(min.x) || !IsFinite(min.y) || !IsFinite(max.x) || !IsFinite(max.y) ||
            min.x <= int.MinValue || min.y <= int.MinValue ||
            max.x >= int.MaxValue || max.y >= int.MaxValue ||
            min.x >= max.x || min.y >= max.y)
            return false;

        int minX = Mathf.FloorToInt(min.x);
        int minY = Mathf.FloorToInt(min.y);
        int maxX = Mathf.CeilToInt(max.x);
        int maxY = Mathf.CeilToInt(max.y);
        if ((long)maxX - minX > int.MaxValue || (long)maxY - minY > int.MaxValue)
            return false;
        bounds = new RectInt(minX, minY, maxX - minX, maxY - minY);
        return true;
    }

    private static bool Contains(BoundsInt outer, RectInt inner) =>
        inner.xMin >= outer.xMin && inner.yMin >= outer.yMin &&
        inner.xMax <= outer.xMax && inner.yMax <= outer.yMax;

    private static bool IsFinitePositive(float value) => IsFinite(value) && value > 0f;
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
