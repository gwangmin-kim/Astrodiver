using System;
using UnityEngine;

[Serializable]
public sealed class FloatageDropBonusUpgradeEffect : UpgradeEffect
{
    [SerializeField] private FloatageDefinition _floatage;
    [SerializeField, Min(0f)] private float _bonus;

    public FloatageDropBonusUpgradeEffect()
    {
    }

    public FloatageDropBonusUpgradeEffect(FloatageDefinition floatage, float bonus)
    {
        _floatage = floatage;
        _bonus = bonus;
    }

    public FloatageDefinition Floatage => _floatage;
    public float Bonus => _bonus;

    public override bool TryValidate(out string error)
    {
        if (_floatage == null)
        {
            error = "A floatage drop bonus effect requires a floatage definition.";
            return false;
        }

        if (float.IsNaN(_bonus) || float.IsInfinity(_bonus) || _bonus < 0f)
        {
            error = "A floatage drop bonus must be a finite non-negative value.";
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

        context.RuntimeData.FloatageDropMultipliers.AddBonus(_floatage, _bonus);
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
        float next = current > float.MaxValue - _bonus
            ? float.MaxValue
            : current + _bonus;
        preview = UpgradeEffectPreview.Numeric(
            current,
            next,
            false,
            $"{_floatage.name} 드롭 배율");
        return true;
    }
}
