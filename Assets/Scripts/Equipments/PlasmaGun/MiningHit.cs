using UnityEngine;

/// <summary>A cell identity and a snapshot of its beam/impact position.</summary>
public readonly struct MiningHit
{
    public MiningHit(StageMap map, Vector3Int cell, Vector3 position, Vector2 normal)
    {
        Map = map;
        Cell = cell;
        Position = position;
        Normal = normal;
    }

    public StageMap Map { get; }
    public Vector3Int Cell { get; }
    public Vector3 Position { get; }
    public Vector2 Normal { get; }
    public Vector3 CellCenter => Map.Tilemap.GetCellCenterWorld(Cell);
    public bool IsCell(StageMap map, Vector3Int cell) => Map == map && Cell == cell;
}
