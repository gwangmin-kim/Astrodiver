using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "MiningDefinition", menuName = "Astrodiver/Stage Map/Mining Definition")]
public sealed class MiningTileDefinition : TileBase
{
    [SerializeField] private AutoTile _automaticTile;
    [SerializeField, Min(1)] private int _maxHp = 1;
    [SerializeField] private ResourceDefinition _dropResource;
    [SerializeField, Min(1)] private int _baseDropAmount = 1;
    [SerializeField] private bool _isDestructible = true;
    public AutoTile AutomaticTile => _automaticTile;
    public int MaxHp => Mathf.Max(1, _maxHp);
    public ResourceDefinition DropResource => _dropResource;
    public int BaseDropAmount => Mathf.Max(1, _baseDropAmount);
    public bool IsDestructible => _isDestructible;
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        if (_automaticTile != null)
        {
            _automaticTile.GetTileData(position, tilemap, ref tileData);
        }
        tileData.colliderType = Tile.ColliderType.Grid;
    }
    public bool TryValidate(out string error)
    {
        if (_automaticTile == null) { error = $"Mining definition '{name}' requires an AutoTile visual."; return false; }
        if (_automaticTile.m_DefaultGameObject != null) { error = $"Mining definition '{name}' AutoTile must not instantiate GameObjects."; return false; }
        if (_maxHp < 1) { error = $"Mining definition '{name}' requires at least one hit point."; return false; }
        if (_isDestructible && _dropResource == null) { error = $"Destructible mining definition '{name}' requires a drop resource."; return false; }
        if (_baseDropAmount < 1) { error = $"Mining definition '{name}' requires a positive base drop amount."; return false; }
        error = string.Empty; return true;
    }
}
