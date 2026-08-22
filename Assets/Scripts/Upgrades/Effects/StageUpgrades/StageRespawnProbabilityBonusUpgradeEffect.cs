using System;
using UnityEngine;

[Serializable]
public sealed class StageRespawnProbabilityBonusUpgradeEffect : UpgradeEffect
{
    [SerializeField] private StageDefinition _stage;
    [SerializeField, Min(0f)] private float _bonus;

    public StageRespawnProbabilityBonusUpgradeEffect()
    {
    }

    public StageRespawnProbabilityBonusUpgradeEffect(
        StageDefinition stage,
        float bonus)
    {
        _stage = stage;
        _bonus = bonus;
    }

    public StageDefinition Stage => _stage;
    public float Bonus => _bonus;

    public override bool TryValidate(out string error)
    {
        if (_stage == null)
        {
            error = "A stage respawn probability bonus requires a stage definition.";
            return false;
        }

        if (float.IsNaN(_bonus) || float.IsInfinity(_bonus) || _bonus < 0f)
        {
            error = "A stage respawn probability bonus must be a finite non-negative value.";
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

        context.RuntimeData.StageRespawnProbabilityBonuses.AddBonus(
            _stage,
            _bonus);
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

        float current = runtimeData.StageRespawnProbabilityBonuses
            .GetBonus(_stage);
        float next = current > float.MaxValue - _bonus
            ? float.MaxValue
            : current + _bonus;
        preview = UpgradeEffectPreview.Numeric(
            current,
            next,
            false);
        return true;
    }
}
