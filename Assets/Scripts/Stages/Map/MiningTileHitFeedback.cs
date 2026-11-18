using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Astrodiver.Stages.Map
{
    /// <summary>
    /// 피격된 채굴 타일 위에 같은 스프라이트의 오버레이를 띄워 펀치(크기) + 짧은 발광으로 피격을 표시한다.
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
        [SerializeField] private int _sortingOrderOffset = 1;

        private sealed class Entry
        {
            public Transform Root;
            public SpriteRenderer Base;
            public SpriteRenderer Flash;
            public Vector3Int Cell;
            public float Elapsed;
        }

        private readonly Dictionary<Vector3Int, Entry> _active = new();
        private readonly Stack<Entry> _pool = new();
        private readonly List<Vector3Int> _finished = new();
        private MaterialPropertyBlock _propertyBlock;
        private StageMap _map;
        private TilemapRenderer _tilemapRenderer;

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
            Matrix4x4 matrix = tilemap.GetTransformMatrix(damaged.Cell);
            entry.Root.SetPositionAndRotation(tilemap.GetCellCenterWorld(damaged.Cell), matrix.rotation);

            ConfigureRenderer(entry.Base, sprite, _sortingOrderOffset);
            entry.Base.sharedMaterial = _tilemapRenderer != null ? _tilemapRenderer.sharedMaterial : null;
            entry.Base.color = tilemap.color * tilemap.GetColor(damaged.Cell);

            ConfigureRenderer(entry.Flash, sprite, _sortingOrderOffset + 1);
            entry.Flash.sharedMaterial = _flashMaterial;
            entry.Flash.enabled = _flashMaterial != null;

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

            if (!entry.Flash.enabled) return;
            _propertyBlock ??= new MaterialPropertyBlock();
            entry.Flash.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_flashColorId, _flashColor);
            _propertyBlock.SetFloat(_flashAmountId, Mathf.Clamp01(_flashCurve.Evaluate(t)));
            entry.Flash.SetPropertyBlock(_propertyBlock);
        }

        private void ConfigureRenderer(SpriteRenderer renderer, Sprite sprite, int orderOffset)
        {
            renderer.sprite = sprite;
            if (_tilemapRenderer == null) return;
            renderer.sortingLayerID = _tilemapRenderer.sortingLayerID;
            renderer.sortingOrder = _tilemapRenderer.sortingOrder + orderOffset;
        }

        private Entry Rent()
        {
            if (_pool.Count > 0) return _pool.Pop();

            GameObject root = new("Mining Tile Hit Feedback");
            root.transform.SetParent(transform, false);
            GameObject flash = new("Flash");
            flash.transform.SetParent(root.transform, false);
            return new Entry
            {
                Root = root.transform,
                Base = root.AddComponent<SpriteRenderer>(),
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
