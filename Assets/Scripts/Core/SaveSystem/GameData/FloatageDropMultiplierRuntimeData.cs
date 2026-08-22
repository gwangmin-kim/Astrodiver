using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds derived, definition-specific floatage drop multipliers.
/// This data is rebuilt from purchased upgrades and is never saved directly.
/// </summary>
public sealed class FloatageDropMultiplierRuntimeData
{
    private readonly Dictionary<FloatageDefinition, float> _multipliers = new();

    public float GetMultiplier(FloatageDefinition definition)
    {
        if (definition == null ||
            !_multipliers.TryGetValue(definition, out float multiplier))
        {
            return 1f;
        }

        return Mathf.Max(1f, multiplier);
    }

    public void Multiply(FloatageDefinition definition, float multiplier)
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

    public void AddBonus(FloatageDefinition definition, float bonus)
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
