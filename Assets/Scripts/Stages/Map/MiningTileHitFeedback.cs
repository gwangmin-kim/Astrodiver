using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Astrodiver.Stages.Map
{
    /// <summary>
    /// 피격된 채굴 타일 위에 같은 스프라이트의 오버레이를 띄워 펀치(크기) + 짧은 발광으로 피격을 표시한다.
    /// StageOreOverlay가 있으면 부여된 광석 스프라이트도 함께 복제해 같이 튀게 한다.
    /// Tilemap의 셀 색상은 HDR 값을 표현할 수 없고 AutoTile 갱신 시 초기화될 수 있으므로,
    /// 타일맵 자체를 수정하지 않고 풀링된 SpriteRenderer로 처리한다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(StageMap))]
    public sealed class MiningTileHitFeedback : MonoBehaviour
    {
        private static readonly int _flashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int _flashAmountId = Shader.PropertyToID("_FlashAmount");

        [Header("Required References")]
        [Tooltip("Astrodiver/2D/Sprite Hit Flash 셰이더를 사용하는 머티리얼")]
        [SerializeField] private Material _flashMaterial;

        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float _duration = 0.16f;

        [Header("Punch")]
        [Tooltip("최대 크기 증가량 (0.15 = 115%)")]
        [SerializeField, Min(0f)] private float _punchAmount = 0.15f;
        [Tooltip("정규화된 시간(0~1)에 따른 펀치 비율. 오버레이가 원본 타일을 덮도록 0 이상을 유지")]
        [SerializeField] private AnimationCurve _punchCurve = new(
            new Keyframe(0f, 0f, 0f, 12f),
            new Keyframe(0.25f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -1.5f, 0f));

        [Header("Flash")]
        [Tooltip("원본 타일 색에 곱해 더해지는 밝기 배율. 1.5면 최고점에서 원래 밝기의 약 2.5배")]
        [SerializeField, ColorUsage(false, true)] private Color _flashColor = new(1.5f, 1.5f, 1.5f, 1f);
        [Tooltip("정규화된 시간(0~1)에 따른 발광 비율")]
        [SerializeField] private AnimationCurve _flashCurve = new(
            new Keyframe(0f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -2f, 0f));

        [Header("Sorting")]
        [Tooltip("베이스/광석 오버레이 중 더 위에 있는 렌더러 기준 정렬 순서 오프셋")]
        [SerializeField] private int _sortingOrderOffset = 1;

        private sealed class Layer
        {
            public Transform Transform;
            public SpriteRenderer Sprite;
            public SpriteRenderer Flash;
        }

        private sealed class Entry
        {
            public Transform Root;
            public Layer Base;
            public Layer Ore;
            public Vector3Int Cell;
            public float Elapsed;
        }

        private readonly Dictionary<Vector3Int, Entry> _active = new();
        private readonly Stack<Entry> _pool = new();
        private readonly List<Vector3Int> _finished = new();
        private MaterialPropertyBlock _propertyBlock;
        private StageMap _map;
        private TilemapRenderer _tilemapRenderer;
        private StageOreOverlay _oreOverlay;
        private TilemapRenderer _oreRenderer;

        private void OnEnable()
        {
            _map = GetComponent<StageMap>();
            _map.MiningCellDamaged += HandleCellDamaged;
            _map.MiningCellDestroyed += HandleCellDestroyed;
        }

        private void OnDisable()
        {
            if (_map != null)
            {
                _map.MiningCellDamaged -= HandleCellDamaged;
                _map.MiningCellDestroyed -= HandleCellDestroyed;
            }
            _map = null;
            ReleaseAll();
        }

        private void HandleCellDamaged(StageMapMiningCellDamaged damaged)
        {
            if (!Application.isPlaying || _map.Tilemap == null) return;
            Tilemap tilemap = _map.Tilemap;
            Sprite sprite = tilemap.GetSprite(damaged.Cell);
            if (sprite == null) return;

            // 같은 셀이 다시 맞으면 진행 중인 효과를 처음부터 재생한다.
            if (!_active.TryGetValue(damaged.Cell, out Entry entry))
            {
                entry = Rent();
                entry.Cell = damaged.Cell;
                _active.Add(damaged.Cell, entry);
            }
            entry.Elapsed = 0f;

            if (_tilemapRenderer == null) _tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
            entry.Root.SetPositionAndRotation(tilemap.GetCellCenterWorld(damaged.Cell), Quaternion.identity);
            int sortingOrder = GetBaseSortingOrder() + _sortingOrderOffset;

            ConfigureLayer(entry.Base, tilemap, _tilemapRenderer, damaged.Cell, sprite, sortingOrder);

            Tilemap oreTilemap = GetOreTilemap();
            Sprite oreSprite = oreTilemap != null ? oreTilemap.GetSprite(damaged.Cell) : null;
            entry.Ore.Transform.gameObject.SetActive(oreSprite != null);
            if (oreSprite != null)
                ConfigureLayer(entry.Ore, oreTilemap, _oreRenderer, damaged.Cell, oreSprite, sortingOrder + 2);

            entry.Root.gameObject.SetActive(true);
            Apply(entry, 0f);
        }

        private void HandleCellDestroyed(StageMapMiningCellDestroyed destroyed)
        {
            if (_active.Remove(destroyed.Cell, out Entry entry)) Release(entry);
        }

        private void Update()
        {
            if (_active.Count == 0) return;

            float deltaTime = Time.deltaTime;
            foreach (Entry entry in _active.Values)
            {
                entry.Elapsed += deltaTime;
                float t = entry.Elapsed / _duration;
                if (t >= 1f) { _finished.Add(entry.Cell); continue; }
                Apply(entry, t);
            }

            for (int i = 0; i < _finished.Count; i++)
            {
                if (_active.Remove(_finished[i], out Entry entry)) Release(entry);
            }
            _finished.Clear();
        }

        private void Apply(Entry entry, float t)
        {
            float scale = 1f + _punchAmount * Mathf.Max(0f, _punchCurve.Evaluate(t));
            entry.Root.localScale = new Vector3(scale, scale, 1f);

            float flashAmount = Mathf.Clamp01(_flashCurve.Evaluate(t));
            ApplyFlash(entry.Base.Flash, flashAmount);
            if (entry.Ore.Transform.gameObject.activeSelf) ApplyFlash(entry.Ore.Flash, flashAmount);
        }

        private void ApplyFlash(SpriteRenderer flash, float amount)
        {
            if (!flash.enabled) return;
            _propertyBlock ??= new MaterialPropertyBlock();
            flash.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_flashColorId, _flashColor);
            _propertyBlock.SetFloat(_flashAmountId, amount);
            flash.SetPropertyBlock(_propertyBlock);
        }

        private void ConfigureLayer(Layer layer, Tilemap tilemap, TilemapRenderer source,
            Vector3Int cell, Sprite sprite, int sortingOrder)
        {
            // 셀 회전(AutoTile/광석 랜덤 회전)은 레이어별로, 펀치 스케일은 공통 Root에 적용한다.
            layer.Transform.localRotation = tilemap.GetTransformMatrix(cell).rotation;

            ConfigureRenderer(layer.Sprite, sprite, source, sortingOrder);
            layer.Sprite.sharedMaterial = source != null ? source.sharedMaterial : null;
            layer.Sprite.color = tilemap.color * tilemap.GetColor(cell);

            ConfigureRenderer(layer.Flash, sprite, source, sortingOrder + 1);
            layer.Flash.sharedMaterial = _flashMaterial;
            layer.Flash.enabled = _flashMaterial != null;
        }

        private static void ConfigureRenderer(SpriteRenderer renderer, Sprite sprite,
            TilemapRenderer source, int sortingOrder)
        {
            renderer.sprite = sprite;
            if (source != null) renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = sortingOrder;
        }

        private Tilemap GetOreTilemap()
        {
            if (_oreOverlay == null) _oreOverlay = GetComponent<StageOreOverlay>();
            Tilemap oreTilemap = _oreOverlay != null ? _oreOverlay.OreTilemap : null;
            if (oreTilemap != null && _oreRenderer == null) _oreRenderer = oreTilemap.GetComponent<TilemapRenderer>();
            return oreTilemap;
        }

        /// <summary>복제 렌더러가 원본 베이스/광석 타일보다 항상 위에 그려지도록 둘 중 높은 정렬 순서를 기준으로 한다.</summary>
        private int GetBaseSortingOrder()
        {
            int order = _tilemapRenderer != null ? _tilemapRenderer.sortingOrder : 0;
            if (GetOreTilemap() != null && _oreRenderer != null) order = Mathf.Max(order, _oreRenderer.sortingOrder);
            return order;
        }

        private Entry Rent()
        {
            if (_pool.Count > 0) return _pool.Pop();

            GameObject root = new("Mining Tile Hit Feedback");
            root.transform.SetParent(transform, false);
            return new Entry
            {
                Root = root.transform,
                Base = CreateLayer("Base", root.transform),
                Ore = CreateLayer("Ore", root.transform)
            };
        }

        private static Layer CreateLayer(string name, Transform parent)
        {
            GameObject sprite = new(name);
            sprite.transform.SetParent(parent, false);
            GameObject flash = new("Flash");
            flash.transform.SetParent(sprite.transform, false);
            return new Layer
            {
                Transform = sprite.transform,
                Sprite = sprite.AddComponent<SpriteRenderer>(),
                Flash = flash.AddComponent<SpriteRenderer>()
            };
        }

        private void Release(Entry entry)
        {
            if (entry.Root == null) return;
            entry.Root.gameObject.SetActive(false);
            _pool.Push(entry);
        }

        private void ReleaseAll()
        {
            foreach (Entry entry in _active.Values) Release(entry);
            _active.Clear();
        }
    }
}
