using System;
using UnityEngine;

[Serializable]
public sealed class UnlockUpgradeEffect : UpgradeEffect
{
    [SerializeField] private UnlockUpgradeTarget _target = UnlockUpgradeTarget.NetGun;
    [SerializeField] private bool _isUnlocked = true;

    public UnlockUpgradeEffect()
    {
    }

    public UnlockUpgradeEffect(UnlockUpgradeTarget target, bool isUnlocked = true)
    {
        _target = target;
        _isUnlocked = isUnlocked;
    }

    public UnlockUpgradeTarget Target => _target;
    public bool IsUnlocked => _isUnlocked;

    public override bool TryCreatePreview(
        GameRuntimeData runtimeData,
        out UpgradeEffectPreview preview)
    {
        if (!_isUnlocked)
        {
            preview = default;
            return false;
        }

        preview = UpgradeEffectPreview.Unlock();
        return true;
    }

    public override bool TryValidate(out string error)
    {
        if (!Enum.IsDefined(typeof(UnlockUpgradeTarget), _target))
        {
            error = $"Unknown unlock upgrade target value '{(int)_target}'.";
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

        switch (_target)
        {
            case UnlockUpgradeTarget.NetGun:
                NetGunData netGun = context.RuntimeData.Equipment.netGun;
                netGun.isUnlocked = _isUnlocked;
                context.RuntimeData.Equipment.netGun = netGun;
                context.RuntimeData.Equipment.netGunInitialized = true;
                break;

            default:
                error = $"Unsupported unlock upgrade target '{_target}'.";
                return false;
        }

        error = null;
        return true;
    }
}
