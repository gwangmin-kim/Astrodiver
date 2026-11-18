using System.Collections.Generic;
using UnityEngine;

namespace Astrodiver.Stages.Generation
{
    /// <summary>
    /// 고체 셀마다 모든 광석 규칙을 평가해 풍부도가 가장 높은 광석 하나만 부여한다.
    /// 결과는 buffer.OreIndex(규칙 인덱스, 없으면 -1)와 buffer.OreRichness(0~1)에 기록된다.
    /// </summary>
    internal sealed class OreDepositRule
    {
        private readonly IReadOnlyList<OreGenerationRule> _rules;
        private readonly Vector2[] _offsets;

        public OreDepositRule(IReadOnlyList<OreGenerationRule> rules, int seed)
        {
            _rules = rules;
            _offsets = new Vector2[rules.Count];
            // 지형 노이즈와 다른 스트림을 쓰도록 시드를 섞는다.
            System.Random random = new(unchecked(seed * 486187739 + 0x5EED0BE));
            for (int i = 0; i < _offsets.Length; i++)
                _offsets[i] = new Vector2(random.Next(-100000, 100001), random.Next(-100000, 100001));
        }

        public void Apply(StageGenerationBuffer buffer)
        {
            BoundsInt bounds = buffer.Bounds;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    int index = buffer.Index(x, y);
                    buffer.OreIndex[index] = -1;
                    buffer.OreRichness[index] = 0f;
                    if (!buffer.Solid[index]) continue;

                    float sampleX = x + 0.5f;
                    float sampleY = y + 0.5f;
                    int bestRule = -1;
                    float bestRichness = -1f;
                    for (int r = 0; r < _rules.Count; r++)
                    {
                        OreGenerationRule rule = _rules[r];
                        if (rule == null || !rule.IsActive) continue;

                        float noise = Mathf.PerlinNoise(
                            sampleX / rule.HorizontalScale + _offsets[r].x,
                            sampleY / rule.VerticalScale + _offsets[r].y);
                        if (noise < rule.Threshold) continue;

                        float richness = Mathf.InverseLerp(rule.Threshold, rule.FullAt, noise);
                        if (richness > bestRichness)
                        {
                            bestRichness = richness;
                            bestRule = r;
                        }
                    }

                    if (bestRule < 0) continue;
                    buffer.OreIndex[index] = (sbyte)bestRule;
                    buffer.OreRichness[index] = bestRichness;
                }
            }
        }
    }
}
