using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class StageMap : MonoBehaviour
{
    public static readonly Vector3 cellSize = Vector3.one;
    [SerializeField] private Grid _grid;
    [SerializeField] private Tilemap _tilemap;
    private readonly Dictionary<Vector3Int, int> _miningHitPoints = new();
    public event Action<StageMapMiningCellDestroyed> MiningCellDestroyed;
    public Grid Grid => _grid;
    public Tilemap Tilemap => _tilemap;
    public void Configure(Grid grid, Tilemap tilemap) { _grid = grid; _tilemap = tilemap; EnforceTransformLock(); }
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
        definition = GetDefinition(cell);
        if (definition == null) { currentHitPoints = 0; return false; }
        if (!_miningHitPoints.TryGetValue(cell, out currentHitPoints)) { currentHitPoints = definition.MaxHp; _miningHitPoints.Add(cell, currentHitPoints); }
        return true;
    }
    public bool TryApplyMiningDamage(Vector3Int cell, int damage, out int remainingHitPoints)
    {
        remainingHitPoints = 0;
        if (damage <= 0 || !TryGetMiningCell(cell, out MiningTileDefinition definition, out int hitPoints) || !definition.IsDestructible) return false;
        remainingHitPoints = Mathf.Max(0, hitPoints - damage);
        if (remainingHitPoints > 0) { _miningHitPoints[cell] = remainingHitPoints; return true; }
        _miningHitPoints.Remove(cell);
        Vector3 worldPosition = _tilemap.GetCellCenterWorld(cell);
        _tilemap.SetTile(cell, null);
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) _tilemap.RefreshTile(cell + new Vector3Int(x, y, 0));
        if (_tilemap.TryGetComponent(out TilemapCollider2D collider)) collider.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
        MiningCellDestroyed?.Invoke(new StageMapMiningCellDestroyed(cell, worldPosition, definition));
        return true;
    }
    private void OnEnable() => EnforceTransformLock();
    private void OnValidate() => EnforceTransformLock();
    private void LateUpdate() => EnforceTransformLock();
    private MiningTileDefinition GetDefinition(Vector3Int cell) => _tilemap != null ? _tilemap.GetTile<MiningTileDefinition>(cell) : null;
    private static void Pin(Transform target) { if (target == null) return; target.SetPositionAndRotation(Vector3.zero, Quaternion.identity); target.localScale = Vector3.one; }
}
public readonly struct StageMapMiningCellDestroyed
{
    public StageMapMiningCellDestroyed(Vector3Int cell, Vector3 worldPosition, MiningTileDefinition definition) { Cell = cell; WorldPosition = worldPosition; Definition = definition; }
    public Vector3Int Cell { get; } public Vector3 WorldPosition { get; } public MiningTileDefinition Definition { get; }
}
