using Astrodiver.Definitions;
using UnityEngine;

namespace Astrodiver.Stages.Map
{
    /// <summary>
    /// 베이스 채굴 타일 위에 "부여"되는 특수 자원. 타일마다 0~1의 풍부도(richness)를 가지며,
    /// 풍부도에 따라 표시 스프라이트 단계, 추가 HP, 드롭 개수가 결정된다.
    /// </summary>
    [CreateAssetMenu(fileName = "OreDefinition", menuName = "Astrodiver/Stage Map/Ore Definition")]
    public sealed class OreDefinition : ScriptableObject
    {
        [Header("Resource")]
        [SerializeField] private ResourceDefinition _resource;

        [Header("Visual")]
        [Tooltip("풍부도 단계별 조각 스프라이트. 드문 단계 → 꽉 찬 단계 순서")]
        [SerializeField] private Sprite[] _levelSprites = System.Array.Empty<Sprite>();
        [Tooltip("셀 좌표 기반으로 90° 단위 회전을 섞어 반복 패턴을 줄인다. 광원 방향이 그려진 스프라이트라면 끈다.")]
        [SerializeField] private bool _randomRotation;
        [Tooltip("Scene 뷰 디버그 기즈모 색상")]
        [SerializeField] private Color _debugColor = new(1f, 0.8f, 0.2f, 1f);

        [Header("Hit Points")]
        [Tooltip("풍부도 1일 때 베이스 타일 HP에 더해지는 HP. 풍부도에 비례해 반올림된다.")]
        [SerializeField, Min(0)] private int _bonusHpAtFull;

        [Header("Drops")]
        [Tooltip("풍부도 0일 때 드롭 개수 (광석이 부여된 타일은 최소 1개)")]
        [SerializeField, Min(1)] private int _minDropAmount = 1;
        [Tooltip("풍부도 1일 때 드롭 개수")]
        [SerializeField, Min(1)] private int _maxDropAmount = 4;

        public ResourceDefinition Resource => _resource;
        public int LevelCount => _levelSprites?.Length ?? 0;
        public Color DebugColor => _debugColor;
        public bool RandomRotation => _randomRotation;

        /// <summary>풍부도(0~1)를 스프라이트 단계 인덱스로 변환한다. 단계 수로 균등 분할한다.</summary>
        public int GetLevel(float richness)
        {
            int count = LevelCount;
            if (count == 0) return -1;
            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(richness) * count), 0, count - 1);
        }

        public Sprite GetSprite(float richness)
        {
            int level = GetLevel(richness);
            return level >= 0 ? _levelSprites[level] : null;
        }

        public int GetBonusHp(float richness) =>
            Mathf.RoundToInt(Mathf.Max(0, _bonusHpAtFull) * Mathf.Clamp01(richness));

        /// <summary>
        /// 풍부도에 비례한 드롭 기대값을 확률적 반올림으로 정수화한다.
        /// 기대값이 풍부도에 정확히 비례하도록 소수부를 확률로 처리한다.
        /// </summary>
        public int RollDropCount(float richness, float random01)
        {
            int min = Mathf.Max(1, _minDropAmount);
            int max = Mathf.Max(min, _maxDropAmount);
            float expected = Mathf.Lerp(min, max, Mathf.Clamp01(richness));
            int whole = Mathf.FloorToInt(expected);
            return Mathf.Max(1, whole + (random01 < expected - whole ? 1 : 0));
        }

        public bool TryValidate(out string error)
        {
            if (_resource == null) { error = $"Ore definition '{name}' requires a resource."; return false; }
            if (LevelCount == 0) { error = $"Ore definition '{name}' requires at least one level sprite."; return false; }
            for (int i = 0; i < _levelSprites.Length; i++)
            {
                if (_levelSprites[i] == null) { error = $"Ore definition '{name}' has an empty level sprite at index {i}."; return false; }
            }
            if (_maxDropAmount < _minDropAmount) { error = $"Ore definition '{name}' max drop amount must be >= min drop amount."; return false; }
            error = string.Empty; return true;
        }
    }
}
