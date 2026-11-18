using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Astrodiver.Stages.Map
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class StageMap : MonoBehaviour
    {
        public static readonly Vector3 cellSize = Vector3.one;
        private static readonly List<StageMap> _activeMaps = new();
        public static IReadOnlyList<StageMap> ActiveMaps => _activeMaps;
        [SerializeField] private Grid _grid;
        [SerializeField] private Tilemap _tilemap;
        private readonly Dictionary<Vector3Int, int> _miningHitPoints = new();
        private BoundsInt _oreBounds;
        private OreDefinition[] _orePalette;
        private sbyte[] _oreIndices;
        private float[] _oreRichness;
        public event Action<StageMapMiningCellDestroyed> MiningCellDestroyed;
        /// <summary>피해를 받았지만 파괴되지 않은 셀. 파괴 시에는 MiningCellDestroyed만 발생한다.</summary>
        public event Action<StageMapMiningCellDamaged> MiningCellDamaged;
        public Grid Grid => _grid;
        public Tilemap Tilemap => _tilemap;
        public void Configure(Grid grid, Tilemap tilemap) { _grid = grid; _tilemap = tilemap; EnforceTransformLock(); }
        /// <summary>광석 데이터가 저장된 셀 범위. 광석이 없으면 크기가 0이다.</summary>
        public BoundsInt OreBounds => _oreBounds;
        public void ApplyGeneratedTiles(BoundsInt bounds, TileBase[] tiles) => ApplyGeneratedTiles(bounds, tiles, default);
        public void ApplyGeneratedTiles(BoundsInt bounds, TileBase[] tiles, StageOreLayerData ores)
        {
            _tilemap.ClearAllTiles();
            _miningHitPoints.Clear();
            SetOreLayer(bounds, ores);
            _tilemap.SetTilesBlock(bounds, tiles);
            _tilemap.RefreshAllTiles();
            TilemapCollider2D tilemapCollider = _tilemap.GetComponent<TilemapCollider2D>();
            tilemapCollider.ProcessTilemapChanges();
            CompositeCollider2D composite = _tilemap.GetComponent<CompositeCollider2D>();
            if (composite.generationType == CompositeCollider2D.GenerationType.Manual)
                composite.GenerateGeometry();
            Physics2D.SyncTransforms();
        }
        public void EnforceTransformLock() { Pin(_grid != null ? _grid.transform : null); Pin(_tilemap != null ? _tilemap.transform : null); }
        public bool TryValidate(out string error)
        {
            if (_grid == null || _tilemap == null) { error = "StageMap requires a Grid and one collision Tilemap."; return false; }
            if ((_grid.cellSize - cellSize).sqrMagnitude > 0.000001f) { error = "Stage map Grid cell size must be (1, 1, 1)."; return false; }
            if (_tilemap.GetComponent<TilemapRenderer>() == null || _tilemap.GetComponent<TilemapCollider2D>() == null || _tilemap.GetComponent<CompositeCollider2D>() == null) { error = "The collision Tilemap requires TilemapRenderer, TilemapCollider2D, and CompositeCollider2D."; return false; }
            error = string.Empty; return true;
        }
        public bool TryGetMiningCell(Vector3Int cell, out MiningTileDefinition definition, out int currentHitPoints)
        {
            definition = GetMiningDefinition(cell);
            if (definition == null) { currentHitPoints = 0; return false; }
            if (!_miningHitPoints.TryGetValue(cell, out currentHitPoints)) { currentHitPoints = GetMaxHitPoints(cell, definition); _miningHitPoints.Add(cell, currentHitPoints); }
            return true;
        }
        /// <summary>베이스 타일 HP + 부여된 광석의 풍부도 비례 보너스 HP.</summary>
        public int GetMaxHitPoints(Vector3Int cell, MiningTileDefinition definition)
        {
            if (definition == null) return 0;
            long hp = definition.MaxHp;
            if (TryGetOre(cell, out OreDefinition ore, out float richness)) hp += ore.GetBonusHp(richness);
            return (int)Math.Min(hp, int.MaxValue);
        }
        /// <summary>셀에 부여된 광석과 풍부도(0~1)를 조회한다. 베이스 타일이 없는 셀은 항상 false.</summary>
        public bool TryGetOre(Vector3Int cell, out OreDefinition ore, out float richness)
        {
            ore = null; richness = 0f;
            if (!TryGetOreIndex(cell, out int index)) return false;
            int paletteIndex = _oreIndices[index];
            if (paletteIndex < 0 || paletteIndex >= _orePalette.Length) return false;
            ore = _orePalette[paletteIndex];
            if (ore == null) return false;
            richness = _oreRichness[index];
            return true;
        }
        public bool TryApplyMiningDamage(Vector3Int cell, int damage, out int remainingHitPoints)
        {
            remainingHitPoints = 0;
            if (damage <= 0 || !TryGetMiningCell(cell, out MiningTileDefinition definition, out int hitPoints) || !definition.IsDestructible) return false;
            remainingHitPoints = Mathf.Max(0, hitPoints - damage);
            if (remainingHitPoints > 0)
            {
                _miningHitPoints[cell] = remainingHitPoints;
                MiningCellDamaged?.Invoke(new StageMapMiningCellDamaged(cell, damage, remainingHitPoints, definition));
                return true;
            }
            _miningHitPoints.Remove(cell);
            TryGetOre(cell, out OreDefinition ore, out float oreRichness);
            ClearOre(cell);
            Vector3 worldPosition = _tilemap.GetCellCenterWorld(cell);
            _tilemap.SetTile(cell, null);
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) _tilemap.RefreshTile(cell + new Vector3Int(x, y, 0));
            if (_tilemap.TryGetComponent(out TilemapCollider2D collider)) collider.ProcessTilemapChanges();
            if (_tilemap.TryGetComponent(out CompositeCollider2D composite) &&
                composite.generationType == CompositeCollider2D.GenerationType.Manual)
                composite.GenerateGeometry();
            Physics2D.SyncTransforms();
            MiningCellDestroyed?.Invoke(new StageMapMiningCellDestroyed(cell, worldPosition, definition, ore, oreRichness));
            return true;
        }
        private void OnEnable()
        {
            EnforceTransformLock();
            if (!_activeMaps.Contains(this)) _activeMaps.Add(this);
        }
        private void OnDisable() => _activeMaps.Remove(this);
        private void OnValidate() => EnforceTransformLock();
        private void LateUpdate() => EnforceTransformLock();
        public MiningTileDefinition GetMiningDefinition(Vector3Int cell) => _tilemap != null ? _tilemap.GetTile<MiningTileDefinition>(cell) : null;
        private void SetOreLayer(BoundsInt bounds, StageOreLayerData ores)
        {
            int cellCount = bounds.size.x * bounds.size.y;
            if (ores.IsEmpty || ores.Indices.Length != cellCount || ores.Richness.Length != cellCount)
            {
                if (!ores.IsEmpty) Debug.LogError("StageMap: Ore layer size does not match the generated bounds; ores were discarded.", this);
                _oreBounds = default; _orePalette = null; _oreIndices = null; _oreRichness = null;
                return;
            }
            _oreBounds = bounds; _orePalette = ores.Palette; _oreIndices = ores.Indices; _oreRichness = ores.Richness;
        }
        private bool TryGetOreIndex(Vector3Int cell, out int index)
        {
            index = -1;
            if (_oreIndices == null || cell.x < _oreBounds.xMin || cell.x >= _oreBounds.xMax || cell.y < _oreBounds.yMin || cell.y >= _oreBounds.yMax) return false;
            index = (cell.y - _oreBounds.yMin) * _oreBounds.size.x + cell.x - _oreBounds.xMin;
            return true;
        }
        private void ClearOre(Vector3Int cell)
        {
            if (!TryGetOreIndex(cell, out int index)) return;
            _oreIndices[index] = -1;
            _oreRichness[index] = 0f;
        }
        private static void Pin(Transform target) { if (target == null) return; target.SetPositionAndRotation(Vector3.zero, Quaternion.identity); target.localScale = Vector3.one; }
    }
    public readonly struct StageMapMiningCellDamaged
    {
        public StageMapMiningCellDamaged(Vector3Int cell, int damage, int remainingHitPoints, MiningTileDefinition definition) { Cell = cell; Damage = damage; RemainingHitPoints = remainingHitPoints; Definition = definition; }
        public Vector3Int Cell { get; } public int Damage { get; } public int RemainingHitPoints { get; } public MiningTileDefinition Definition { get; }
    }
    public readonly struct StageMapMiningCellDestroyed
    {
        public StageMapMiningCellDestroyed(Vector3Int cell, Vector3 worldPosition, MiningTileDefinition definition, OreDefinition ore = null, float oreRichness = 0f) { Cell = cell; WorldPosition = worldPosition; Definition = definition; Ore = ore; OreRichness = ore != null ? oreRichness : 0f; }
        public Vector3Int Cell { get; } public Vector3 WorldPosition { get; } public MiningTileDefinition Definition { get; }
        /// <summary>파괴된 셀에 부여되어 있던 광석. 없으면 null.</summary>
        public OreDefinition Ore { get; } public float OreRichness { get; }
    }
}
