using System;
using UnityEngine;

[Serializable]
public sealed class MiningTileDropBonusUpgradeEffect : UpgradeEffect
{
    [SerializeField] private MiningTileDefinition _miningTile;
    [SerializeField, Min(0f)] private float _bonus;

    public MiningTileDropBonusUpgradeEffect()
    {
    }

    public MiningTileDropBonusUpgradeEffect(MiningTileDefinition miningTile, float bonus)
    {
        _miningTile = miningTile;
        _bonus = bonus;
    }

    public MiningTileDefinition MiningTile => _miningTile;
    public float Bonus => _bonus;

    public override bool TryValidate(out string error)
    {
        if (_miningTile == null)
        {
            error = "A mining tile drop bonus effect requires a mining tile definition.";
            return false;
        }

        if (float.IsNaN(_bonus) || float.IsInfinity(_bonus) || _bonus < 0f)
        {
            error = "A mining tile drop bonus must be a finite non-negative value.";
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

        context.RuntimeData.MiningTileDropMultipliers.AddBonus(_miningTile, _bonus);
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

        float current = runtimeData.MiningTileDropMultipliers.GetMultiplier(_miningTile);
        float next = current > float.MaxValue - _bonus
            ? float.MaxValue
            : current + _bonus;
        preview = UpgradeEffectPreview.Numeric(
            current,
            next,
            false,
            $"{(_miningTile.DropResource != null ? _miningTile.DropResource.DisplayName : _miningTile.name)} 드롭 배율");
        return true;
    }
}
