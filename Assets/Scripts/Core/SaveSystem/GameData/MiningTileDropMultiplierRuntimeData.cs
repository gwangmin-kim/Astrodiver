using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds derived, definition-specific mining tile drop multipliers.
/// This data is rebuilt from purchased upgrades and is never saved directly.
/// </summary>
public sealed class MiningTileDropMultiplierRuntimeData
{
    private readonly Dictionary<MiningTileDefinition, float> _multipliers = new();

    public float GetMultiplier(MiningTileDefinition definition)
    {
        if (definition == null ||
            !_multipliers.TryGetValue(definition, out float multiplier))
        {
            return 1f;
        }

        return Mathf.Max(1f, multiplier);
    }

    public void Multiply(MiningTileDefinition definition, float multiplier)
    {
        if (definition == null)
        {
            return;
        }

        float current = GetMultiplier(definition);
        _multipliers[definition] = current > float.MaxValue / multiplier
            ? float.MaxValue
            : current * multiplier;
    }

    public void AddBonus(MiningTileDefinition definition, float bonus)
    {
        if (definition == null || bonus <= 0f)
        {
            return;
        }

        float current = GetMultiplier(definition);
        _multipliers[definition] = current > float.MaxValue - bonus
            ? float.MaxValue
            : current + bonus;
    }
}
