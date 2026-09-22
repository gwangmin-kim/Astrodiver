using System;
using UnityEngine;

[Serializable]
public sealed class MiningTileDropMultiplierUpgradeEffect : UpgradeEffect
{
    [SerializeField] private MiningTileDefinition _miningTile;
    [SerializeField, Min(1f)] private float _multiplier = 1f;

    public MiningTileDropMultiplierUpgradeEffect()
    {
    }

    public MiningTileDropMultiplierUpgradeEffect(
        MiningTileDefinition miningTile,
        float multiplier)
    {
        _miningTile = miningTile;
        _multiplier = multiplier;
    }

    public MiningTileDefinition MiningTile => _miningTile;
    public float Multiplier => _multiplier;

    public override bool TryValidate(out string error)
    {
        if (_miningTile == null)
        {
            error = "A mining tile drop multiplier effect requires a mining tile definition.";
            return false;
        }

        if (float.IsNaN(_multiplier) || float.IsInfinity(_multiplier) ||
            _multiplier < 1f)
        {
            error = "A mining tile drop multiplier must be a finite value of at least 1.";
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

        context.RuntimeData.MiningTileDropMultipliers.Multiply(_miningTile, _multiplier);
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
        float next = current > float.MaxValue / _multiplier
            ? float.MaxValue
            : current * _multiplier;
        preview = UpgradeEffectPreview.Numeric(
            current,
            next,
            false,
            $"{(_miningTile.DropResource != null ? _miningTile.DropResource.DisplayName : _miningTile.name)} 드롭 배율");
        return true;
    }
}
