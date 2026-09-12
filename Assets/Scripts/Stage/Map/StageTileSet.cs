using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "StageTileSet", menuName = "Astrodiver/Stage Map/Tile Set")]
public sealed class StageTileSet : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private AutoTile _automaticTile;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
    public AutoTile AutomaticTile => _automaticTile;
    public StageMapLayer Layers => StageMapLayer.Platform;
    public bool SupportsLayer(StageMapLayer layer) => layer == StageMapLayer.Platform;
    public bool TryValidate(StageMapLayer layer, out string error)
    {
        if (!SupportsLayer(layer)) { error = "Only the single Platform Tilemap is supported."; return false; }
        return TryValidate(out error);
    }
    public bool TryValidate(out string error)
    {
        if (_automaticTile == null) { error = $"Tile set '{DisplayName}' has no AutoTile assigned."; return false; }
        error = string.Empty; return true;
    }
}
