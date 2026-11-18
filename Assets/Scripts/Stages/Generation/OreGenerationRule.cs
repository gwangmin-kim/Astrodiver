using Astrodiver.Stages.Map;
using System;
using UnityEngine;

namespace Astrodiver.Stages.Generation
{
    /// <summary>
    /// 광석 하나의 분포 설정. 노이즈가 threshold 이상인 고체 셀에 부여되며,
    /// threshold~fullAt 구간을 0~1 풍부도로 다시 정규화한다.
    /// </summary>
    [Serializable]
    public sealed class OreGenerationRule
    {
        [SerializeField] private bool _enabled = true;
        [SerializeField] private OreDefinition _ore;
        [SerializeField, Min(0.001f)] private float _horizontalScale = 6f;
        [SerializeField, Min(0.001f)] private float _verticalScale = 6f;
        [Tooltip("노이즈가 이 값 이상인 셀에 광석이 부여된다.")]
        [SerializeField, Range(0f, 1f)] private float _threshold = 0.6f;
        [Tooltip("노이즈가 이 값 이상이면 풍부도 1. Perlin 노이즈는 1 근처에 거의 도달하지 않으므로 1보다 낮게 둔다.")]
        [SerializeField, Range(0f, 1f)] private float _fullAt = 0.8f;

        public bool IsActive => _enabled && _ore != null;
        public OreDefinition Ore => _ore;
        public float HorizontalScale => _horizontalScale;
        public float VerticalScale => _verticalScale;
        public float Threshold => _threshold;
        public float FullAt => _fullAt;

        public bool TryValidate(out string error)
        {
            if (!_enabled) { error = string.Empty; return true; }
            if (_ore == null) { error = "Enabled ore rule requires an ore definition."; return false; }
            if (!_ore.TryValidate(out error)) return false;
            if (!IsFinitePositive(_horizontalScale) || !IsFinitePositive(_verticalScale))
            { error = $"Ore rule '{_ore.name}' noise scales must be finite and positive."; return false; }
            if (!(_threshold >= 0f && _threshold < _fullAt && _fullAt <= 1f))
            { error = $"Ore rule '{_ore.name}' requires 0 <= threshold < fullAt <= 1."; return false; }
            error = string.Empty; return true;
        }

        private static bool IsFinitePositive(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }
}
