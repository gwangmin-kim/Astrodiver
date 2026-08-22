using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds additive respawn-probability bonuses for individual stages.
/// This data is derived from purchased upgrades and is never saved directly.
/// </summary>
public sealed class StageRespawnProbabilityBonusRuntimeData
{
    private readonly Dictionary<StageDefinition, float> _bonuses = new();

    public float GetBonus(StageDefinition definition)
    {
        if (definition == null ||
            !_bonuses.TryGetValue(definition, out float bonus))
        {
            return 0f;
        }

        return Mathf.Max(0f, bonus);
    }

    public void AddBonus(StageDefinition definition, float bonus)
    {
        if (definition == null || float.IsNaN(bonus) ||
            float.IsInfinity(bonus) || bonus <= 0f)
        {
            return;
        }

        float current = GetBonus(definition);
        _bonuses[definition] = current > float.MaxValue - bonus
            ? float.MaxValue
            : current + bonus;
    }
}
