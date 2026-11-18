using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Astrodiver.Stages.Map
{
    /// <summary>
    /// StageMap에 부여된 광석을 베이스 타일맵 위의 별도 오버레이 Tilemap에 그린다.
    /// 오버레이는 콜라이더가 없으며, 베이스 AutoTile 갱신/콜라이더 재생성과 분리되어 있다.
    /// 오버레이 Tilemap을 지정하지 않으면 플레이 중 Grid 아래에 자동 생성한다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(StageMap))]
    public sealed class StageOreOverlay : MonoBehaviour
    {
        [Tooltip("비워두면 플레이 시 Grid 아래에 'Ore Overlay' Tilemap을 자동 생성한다.")]
        [SerializeField] private Tilemap _oreTilemap;
        [Tooltip("자동 생성 시 베이스 TilemapRenderer 기준 정렬 순서 오프셋")]
        [SerializeField] private int _sortingOrderOffset = 1;

        private readonly Dictionary<Sprite, Tile> _tileCache = new();
        private readonly List<TileChangeData> _changes = new();
        private StageMap _map;
        private bool _createdTilemap;

        public Tilemap OreTilemap => _oreTilemap;

        private void OnEnable()
        {
            _map = GetComponent<StageMap>();
            _map.GeneratedTilesApplied += Rebuild;
            _map.MiningCellDestroyed += HandleCellDestroyed;
            // StageGenerator가 먼저 생성했을 수 있으므로(실행 순서 -1100) 현재 데이터로 한 번 그린다.
            if (Application.isPlaying) Rebuild();
        }

        private void OnDisable()
        {
            if (_map != null)
            {
                _map.GeneratedTilesApplied -= Rebuild;
                _map.MiningCellDestroyed -= HandleCellDestroyed;
            }
            _map = null;
        }

        private void OnDestroy()
        {
            foreach (Tile tile in _tileCache.Values)
            {
                if (tile != null) Destroy(tile);
            }
            _tileCache.Clear();
            if (_createdTilemap && _oreTilemap != null) Destroy(_oreTilemap.gameObject);
        }

        public void Rebuild()
        {
            if (!Application.isPlaying || _map == null || !EnsureTilemap()) return;

            _oreTilemap.ClearAllTiles();
            BoundsInt bounds = _map.OreBounds;
            _changes.Clear();
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int cell = new(x, y, 0);
                    if (!_map.TryGetOre(cell, out OreDefinition ore, out float richness)) continue;
                    Tile tile = GetTile(ore.GetSprite(richness));
                    if (tile == null) continue;
                    _changes.Add(new TileChangeData(cell, tile, Color.white, GetRotation(ore, cell)));
                }
            }

            if (_changes.Count > 0) _oreTilemap.SetTiles(_changes.ToArray(), true);
            _changes.Clear();
        }

        private void HandleCellDestroyed(StageMapMiningCellDestroyed destroyed)
        {
            if (_oreTilemap != null && destroyed.Ore != null) _oreTilemap.SetTile(destroyed.Cell, null);
        }

        private Tile GetTile(Sprite sprite)
        {
            if (sprite == null) return null;
            if (_tileCache.TryGetValue(sprite, out Tile tile) && tile != null) return tile;

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = $"Ore Overlay ({sprite.name})";
            tile.hideFlags = HideFlags.DontSave;
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            // 셀별 회전 행렬을 적용할 수 있도록 Transform 잠금은 해제한다.
            tile.flags = TileFlags.LockColor;
            _tileCache[sprite] = tile;
            return tile;
        }

        private static Matrix4x4 GetRotation(OreDefinition ore, Vector3Int cell)
        {
            if (!ore.RandomRotation) return Matrix4x4.identity;
            int quarterTurns = (int)(Hash(cell) & 3u);
            return quarterTurns == 0
                ? Matrix4x4.identity
                : Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, quarterTurns * 90f));
        }

        /// <summary>시드와 무관한 셀 좌표 해시. 같은 셀은 항상 같은 회전을 갖는다.</summary>
        private static uint Hash(Vector3Int cell)
        {
            unchecked
            {
                uint h = (uint)cell.x * 0x8DA6B343u ^ (uint)cell.y * 0xD8163841u;
                h ^= h >> 13;
                h *= 0x85EBCA6Bu;
                h ^= h >> 16;
                return h;
            }
        }

        private bool EnsureTilemap()
        {
            if (_oreTilemap != null) return true;
            if (_map.Grid == null || _map.Tilemap == null) return false;

            GameObject overlay = new("Ore Overlay");
            overlay.transform.SetParent(_map.Grid.transform, false);
            _oreTilemap = overlay.AddComponent<Tilemap>();
            TilemapRenderer renderer = overlay.AddComponent<TilemapRenderer>();
            if (_map.Tilemap.TryGetComponent(out TilemapRenderer baseRenderer))
            {
                renderer.sharedMaterial = baseRenderer.sharedMaterial;
                renderer.sortingLayerID = baseRenderer.sortingLayerID;
                renderer.sortingOrder = baseRenderer.sortingOrder + _sortingOrderOffset;
                renderer.mode = baseRenderer.mode;
                renderer.maskInteraction = baseRenderer.maskInteraction;
            }
            _oreTilemap.tileAnchor = _map.Tilemap.tileAnchor;
            _oreTilemap.color = _map.Tilemap.color;
            _createdTilemap = true;
            return true;
        }
    }
}
