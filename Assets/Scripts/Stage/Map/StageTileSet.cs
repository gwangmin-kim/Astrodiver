using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    fileName = "StageTileSet",
    menuName = "Astrodiver/Stage Map/Tile Set")]
public sealed class StageTileSet : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private StageMapLayerMask _layers =
        StageMapLayerMask.Platform;
    [SerializeField] private AutoTile _automaticTile;

    public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
        ? name
        : _displayName;
    public StageMapLayerMask Layers => _layers;
    public AutoTile AutomaticTile => _automaticTile;

    public bool SupportsLayer(StageMapLayer layer)
    {
        StageMapLayerMask layerMask =
            (StageMapLayerMask)(1 << (int)layer);
        return (_layers & layerMask) != 0;
    }

    public bool TryValidate(StageMapLayer layer, out string error)
    {
        if (_layers == StageMapLayerMask.None)
        {
            error = $"Tile set '{DisplayName}' has no compatible layers selected.";
            return false;
        }

        if ((_layers & ~StageMapLayerMask.All) != 0)
        {
            error = $"Tile set '{DisplayName}' contains an invalid layer mask.";
            return false;
        }

        if (!SupportsLayer(layer))
        {
            error =
                $"Tile set '{DisplayName}' does not support {layer}. " +
                $"Supported layers: {_layers}.";
            return false;
        }

        if (_automaticTile == null)
        {
            error = $"Tile set '{DisplayName}' has no AutoTile assigned.";
            return false;
        }

        if (_automaticTile.m_DefaultColliderType !=
            Tile.ColliderType.None)
        {
            error = "Visual AutoTile collider must be None.";
            return false;
        }

        if (_automaticTile.m_DefaultGameObject != null)
        {
            error = "Visual AutoTile default GameObject must be empty.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
