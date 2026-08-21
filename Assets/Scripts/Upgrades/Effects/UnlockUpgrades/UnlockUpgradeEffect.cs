using System;
using UnityEngine;

[Serializable]
public sealed class UnlockUpgradeEffect : UpgradeEffect
{
    [SerializeField] private UnlockUpgradeTarget _target = UnlockUpgradeTarget.NetGun;

    public UnlockUpgradeEffect()
    {
    }

    public UnlockUpgradeEffect(UnlockUpgradeTarget target)
    {
        _target = target;
    }

    public UnlockUpgradeTarget Target => _target;

    public override bool TryCreatePreview(
        GameRuntimeData runtimeData,
        out UpgradeEffectPreview preview)
    {
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
                netGun.isUnlocked = true;
                context.RuntimeData.Equipment.netGun = netGun;
                break;

            case UnlockUpgradeTarget.ResourceChest:
                context.RuntimeData.Facilities.ResourceChestUnlocked = true;
                break;

            case UnlockUpgradeTarget.Worktable:
                context.RuntimeData.Facilities.WorktableUnlocked = true;
                break;

            default:
                error = $"Unsupported unlock upgrade target '{_target}'.";
                return false;
        }

        error = null;
        return true;
    }
}
