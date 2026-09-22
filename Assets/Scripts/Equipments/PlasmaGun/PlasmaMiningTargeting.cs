using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tile-only targeting.
/// Chain selection intentionally ignores occlusion.
/// </summary>
public sealed class PlasmaMiningTargeting
{
    private const float ContactTolerance = 0.002f;
    private readonly List<RaycastHit2D> _castHits = new();

    public bool TryFindFirstTarget(Vector2 origin, Vector2 direction, float radius,
        float range, LayerMask layers, out MiningHit target)
    {
        target = default;
        ContactFilter2D filter = new();
        filter.SetLayerMask(layers);
        filter.useTriggers = false;
        Physics2D.CircleCast(origin, radius, direction, filter, _castHits, range);
        float nearestDistance = float.PositiveInfinity;
        foreach (RaycastHit2D hit in _castHits)
        {
            if (hit.distance >= nearestDistance) continue;
            StageMap map = hit.collider.GetComponentInParent<StageMap>();
            if (!IsTargetable(map, layers) || hit.collider.gameObject != map.Tilemap.gameObject)
                continue;
            if (!TryResolveCell(map, hit.point, hit.normal, out Vector3Int cell)) continue;
            nearestDistance = hit.distance;
            target = new MiningHit(map, cell, hit.point, hit.normal);
        }
        return target.Map != null;
    }

    public bool TryResolveCell(StageMap map, Vector2 point, Vector2 normal, out Vector3Int cell)
    {
        Vector2 inside = point - normal * ContactTolerance;
        cell = map.Tilemap.WorldToCell(inside);
        if (map.GetMiningDefinition(cell) != null) return true;

        // A corner hit can round onto an empty neighbour. Only accept cells whose
        // bounds contain this contact within tolerance; never search deeper terrain.
        Vector3Int contactCell = map.Tilemap.WorldToCell(point);
        float nearest = float.PositiveInfinity;
        bool found = false;
        for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
            {
                Vector3Int candidate = contactCell + new Vector3Int(x, y, 0);
                if (map.GetMiningDefinition(candidate) == null) continue;
                Vector2 center = map.Tilemap.GetCellCenterWorld(candidate);
                Vector2 delta = inside - center;
                if (Mathf.Abs(delta.x) > 0.5f + ContactTolerance ||
                    Mathf.Abs(delta.y) > 0.5f + ContactTolerance) continue;
                float distance = delta.sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                cell = candidate;
                found = true;
            }
        return found;
    }

    /// <summary>Replace this function to change the chain policy.</summary>
    public bool TryFindNextMiningTarget(MiningHit previous, float radius, LayerMask layers,
        IReadOnlyList<MiningHit> selected, out MiningHit target)
    {
        target = default;
        if (previous.Map == null || radius < 0f) return false;
        Vector2 origin = previous.CellCenter;
        float nearest = radius * radius;
        foreach (StageMap map in StageMap.ActiveMaps)
        {
            if (!IsTargetable(map, layers)) continue;
            Vector3Int min = map.Tilemap.WorldToCell(origin - Vector2.one * radius);
            Vector3Int max = map.Tilemap.WorldToCell(origin + Vector2.one * radius);
            for (int y = min.y; y <= max.y; y++)
                for (int x = min.x; x <= max.x; x++)
                {
                    Vector3Int cell = new(x, y, 0);
                    if (map.GetMiningDefinition(cell) == null || Contains(selected, map, cell)) continue;
                    Vector3 center = map.Tilemap.GetCellCenterWorld(cell);
                    float distance = ((Vector2)center - origin).sqrMagnitude;
                    // Stable ties: map registration order, then y/x ascending.
                    if (distance > nearest || (target.Map != null && distance == nearest)) continue;
                    nearest = distance;
                    target = new MiningHit(map, cell, center, Vector2.zero);
                }
        }
        return target.Map != null;
    }

    private static bool Contains(IReadOnlyList<MiningHit> selected, StageMap map, Vector3Int cell)
    {
        for (int i = 0; i < selected.Count; i++)
            if (selected[i].IsCell(map, cell)) return true;
        return false;
    }

    private static bool IsTargetable(StageMap map, LayerMask layers)
    {
        if (map == null || !map.isActiveAndEnabled || map.Tilemap == null ||
            (layers.value & (1 << map.Tilemap.gameObject.layer)) == 0) return false;
        return map.Tilemap.TryGetComponent(out CompositeCollider2D collider) &&
            collider.isActiveAndEnabled && !collider.isTrigger &&
            collider.attachedRigidbody != null && collider.attachedRigidbody.simulated;
    }
}
