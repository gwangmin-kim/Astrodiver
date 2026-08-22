using System;
using UnityEngine;

[Serializable]
public sealed class FloatageDropMultiplierUpgradeEffect : UpgradeEffect
{
    [SerializeField] private FloatageDefinition _floatage;
    [SerializeField, Min(1f)] private float _multiplier = 1f;

    public FloatageDropMultiplierUpgradeEffect()
    {
    }

    public FloatageDropMultiplierUpgradeEffect(
        FloatageDefinition floatage,
        float multiplier)
    {
        _floatage = floatage;
        _multiplier = multiplier;
    }

    public FloatageDefinition Floatage => _floatage;
    public float Multiplier => _multiplier;

    public override bool TryValidate(out string error)
    {
        if (_floatage == null)
        {
            error = "A floatage drop multiplier effect requires a floatage definition.";
            return false;
        }

        if (float.IsNaN(_multiplier) || float.IsInfinity(_multiplier) ||
            _multiplier < 1f)
        {
            error = "A floatage drop multiplier must be a finite value of at least 1.";
            return false;
        }

        error = null;
        return true;
    }

    public override bool TryApply(UpgradeEffectContext context, out string error)
    {
        if (context == null)
        {
            error = "Upgrade effect context is null.";
            return false;
        }

        if (!TryValidate(out error))
        {
            return false;
        }

        context.RuntimeData.FloatageDropMultipliers.Multiply(_floatage, _multiplier);
        error = null;
        return true;
    }

    public override bool TryCreatePreview(
        GameRuntimeData runtimeData,
        out UpgradeEffectPreview preview)
    {
        if (runtimeData == null || !TryValidate(out _))
        {
            preview = default;
            return false;
        }

        float current = runtimeData.FloatageDropMultipliers.GetMultiplier(_floatage);
        float next = current > float.MaxValue / _multiplier
            ? float.MaxValue
            : current * _multiplier;
        preview = UpgradeEffectPreview.Numeric(
            current,
            next,
            false,
            $"{_floatage.name} 드롭 배율");
        return true;
    }
}
