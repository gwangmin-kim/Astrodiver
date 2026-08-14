using System;
using UnityEngine;

public enum NumericUpgradeOperation
{
    Add = 0,
    Multiply = 1,
    Set = 2
}

[Serializable]
public sealed class NumericUpgradeEffect : UpgradeEffect
{
    [SerializeField]
    private NumericUpgradeTarget _target =
        NumericUpgradeTarget.MovementSpeedRatio;
    [SerializeField] private NumericUpgradeOperation _operation = NumericUpgradeOperation.Add;
    [SerializeField] private float _value;

    public NumericUpgradeEffect()
    {
    }

    public NumericUpgradeEffect(
        NumericUpgradeTarget target,
        NumericUpgradeOperation operation,
        float value)
    {
        _target = target;
        _operation = operation;
        _value = value;
    }

    public NumericUpgradeTarget Target => _target;
    public NumericUpgradeOperation Operation => _operation;
    public float Value => _value;

    public override bool TryCreatePreview(
        GameRuntimeData runtimeData,
        out UpgradeEffectPreview preview)
    {
        if (runtimeData == null ||
            !TryGetCurrentValue(runtimeData, out float currentValue, out bool isInteger))
        {
            preview = default;
            return false;
        }

        float nextValue = isInteger
            ? ApplyInt(Mathf.RoundToInt(currentValue), GetMinimumInt())
            : ApplyFloat(currentValue, GetMinimumFloat());
        preview = UpgradeEffectPreview.Numeric(currentValue, nextValue, isInteger);
        return true;
    }

    /// <summary>
    /// 유효한 업그레이드 효과인지 검증
    /// </summary>
    public override bool TryValidate(out string error)
    {
        if (!Enum.IsDefined(typeof(NumericUpgradeTarget), _target))
        {
            error = $"Unknown numeric upgrade target value '{(int)_target}'.";
            return false;
        }

        if (!Enum.IsDefined(typeof(NumericUpgradeOperation), _operation))
        {
            error = $"Unknown numeric upgrade operation value '{(int)_operation}'.";
            return false;
        }

        if (_operation == NumericUpgradeOperation.Multiply && _value < 0f)
        {
            error = "A multiply effect cannot use a negative value.";
            return false;
        }

        if (float.IsNaN(_value) || float.IsInfinity(_value))
        {
            error = "A numeric effect requires a finite value.";
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

        GameRuntimeData data = context.RuntimeData;
        PlayerStatsRuntimeData player = data.PlayerStats;
        EquipmentRuntimeData equipment = data.Equipment;
        InventoryRuntimeData inventory = data.Inventory;

        switch (_target)
        {
            case NumericUpgradeTarget.MovementSpeedRatio:
                {
                    PlayerMovementData value = player.movement;
                    value.moveSpeedRatio = ApplyFloat(value.moveSpeedRatio, 0f);
                    player.movement = value;
                    player.movementInitialized = true;
                    break;
                }
            case NumericUpgradeTarget.BatteryCapacity:
                {
                    BatteryData value = player.battery;
                    value.amount = ApplyFloat(value.amount, 0f);
                    player.battery = value;
                    player.batteryInitialized = true;
                    break;
                }
            case NumericUpgradeTarget.MagnetRadiusRatio:
                {
                    MagnetData value = player.magnet;
                    value.radiusRatio = ApplyFloat(value.radiusRatio, 0f);
                    player.magnet = value;
                    player.magnetInitialized = true;
                    break;
                }
            case NumericUpgradeTarget.NetCaptureCount:
            case NumericUpgradeTarget.NetCount:
            case NumericUpgradeTarget.NetRadiusRatio:
            case NumericUpgradeTarget.NetShootRangeRatio:
            case NumericUpgradeTarget.NetChargeTimeRatio:
            case NumericUpgradeTarget.NetCollectSpeedRatio:
            case NumericUpgradeTarget.NetAmmoCapacity:
                {
                    NetGunData value = equipment.netGun;
                    ApplyNetGun(ref value);
                    equipment.netGun = value;
                    equipment.netGunInitialized = true;
                    break;
                }
            case NumericUpgradeTarget.PlasmaChargeSpeedMultiplier:
            case NumericUpgradeTarget.PlasmaDamage:
            case NumericUpgradeTarget.PlasmaTickSpeedMultiplier:
            case NumericUpgradeTarget.PlasmaAttackRangeRatio:
            case NumericUpgradeTarget.PlasmaChainCount:
            case NumericUpgradeTarget.PlasmaChainDamageRateRatio:
            case NumericUpgradeTarget.PlasmaChainDetectRangeRatio:
            case NumericUpgradeTarget.PlasmaAmmoCapacity:
                {
                    PlasmaGunData value = equipment.plasmaGun;
                    ApplyPlasmaGun(ref value);
                    equipment.plasmaGun = value;
                    equipment.plasmaGunInitialized = true;
                    break;
                }
            case NumericUpgradeTarget.CreatureSlotCapacity:
            case NumericUpgradeTarget.CreatureMaxStackCount:
            case NumericUpgradeTarget.TimeoutInventoryLossRatio:
                {
                    ApplyInventory(inventory);
                    break;
                }
            default:
                error = $"Unsupported numeric upgrade target '{_target}'.";
                return false;
        }

        error = null;
        return true;
    }

    private void ApplyNetGun(ref NetGunData data)
    {
        switch (_target)
        {
            case NumericUpgradeTarget.NetCaptureCount:
                data.netData.captureCount = ApplyInt(data.netData.captureCount, 1);
                break;
            case NumericUpgradeTarget.NetCount:
                data.netCount = ApplyInt(data.netCount, 1);
                break;
            case NumericUpgradeTarget.NetRadiusRatio:
                data.netData.radiusRatio = ApplyFloat(data.netData.radiusRatio, 0f);
                break;
            case NumericUpgradeTarget.NetShootRangeRatio:
                data.shootRangeRatio = ApplyFloat(data.shootRangeRatio, 0f);
                break;
            case NumericUpgradeTarget.NetChargeTimeRatio:
                data.chargeTimeRatio = ApplyFloat(data.chargeTimeRatio, 0f);
                break;
            case NumericUpgradeTarget.NetCollectSpeedRatio:
                data.collectSpeedRatio = ApplyFloat(data.collectSpeedRatio, 0f);
                break;
            case NumericUpgradeTarget.NetAmmoCapacity:
                data.ammoCapacity = ApplyInt(data.ammoCapacity, 0);
                break;
        }
    }

    private void ApplyPlasmaGun(ref PlasmaGunData data)
    {
        switch (_target)
        {
            case NumericUpgradeTarget.PlasmaChargeSpeedMultiplier:
                data.chargeSpeedMultiplier = ApplyFloat(data.chargeSpeedMultiplier, 0f);
                break;
            case NumericUpgradeTarget.PlasmaDamage:
                data.tickDamage = ApplyInt(data.tickDamage, 0);
                break;
            case NumericUpgradeTarget.PlasmaTickSpeedMultiplier:
                data.tickSpeedMultiplier = ApplyFloat(data.tickSpeedMultiplier, 0f);
                break;
            case NumericUpgradeTarget.PlasmaAttackRangeRatio:
                data.attackRangeRatio = ApplyFloat(data.attackRangeRatio, 0f);
                break;
            case NumericUpgradeTarget.PlasmaChainCount:
                data.chainCount = ApplyInt(data.chainCount, 0);
                break;
            case NumericUpgradeTarget.PlasmaChainDamageRateRatio:
                data.chainedDamageRateRatio = ApplyFloat(
                    data.chainedDamageRateRatio,
                    0f);
                break;
            case NumericUpgradeTarget.PlasmaChainDetectRangeRatio:
                data.chainRangeRatio = ApplyFloat(
                    data.chainRangeRatio,
                    0f);
                break;
            case NumericUpgradeTarget.PlasmaAmmoCapacity:
                data.ammoCapacity = ApplyInt(data.ammoCapacity, 0);
                break;
        }
    }

    private void ApplyInventory(InventoryRuntimeData data)
    {
        switch (_target)
        {
            case NumericUpgradeTarget.CreatureSlotCapacity:
                data.CreatureSlotCapacity =
                    ApplyInt(data.CreatureSlotCapacity, 1);
                break;
            case NumericUpgradeTarget.CreatureMaxStackCount:
                data.CreatureMaxStackCount =
                    ApplyInt(data.CreatureMaxStackCount, 1);
                break;
            case NumericUpgradeTarget.TimeoutInventoryLossRatio:
                data.TimeoutInventoryLossRatio =
                    ApplyFloat(data.TimeoutInventoryLossRatio, 0f);
                break;
        }
    }

    private bool TryGetCurrentValue(
        GameRuntimeData data,
        out float value,
        out bool isInteger)
    {
        isInteger = false;
        switch (_target)
        {
            case NumericUpgradeTarget.MovementSpeedRatio:
                value = data.PlayerStats.movement.moveSpeedRatio;
                return true;
            case NumericUpgradeTarget.BatteryCapacity:
                value = data.PlayerStats.battery.amount;
                return true;
            case NumericUpgradeTarget.MagnetRadiusRatio:
                value = data.PlayerStats.magnet.radiusRatio;
                return true;
            case NumericUpgradeTarget.NetCaptureCount:
                value = data.Equipment.netGun.netData.captureCount;
                isInteger = true;
                return true;
            case NumericUpgradeTarget.NetCount:
                value = data.Equipment.netGun.netCount;
                isInteger = true;
                return true;
            case NumericUpgradeTarget.NetRadiusRatio:
                value = data.Equipment.netGun.netData.radiusRatio;
                return true;
            case NumericUpgradeTarget.NetShootRangeRatio:
                value = data.Equipment.netGun.shootRangeRatio;
                return true;
            case NumericUpgradeTarget.NetChargeTimeRatio:
                value = data.Equipment.netGun.chargeTimeRatio;
                return true;
            case NumericUpgradeTarget.NetCollectSpeedRatio:
                value = data.Equipment.netGun.collectSpeedRatio;
                return true;
            case NumericUpgradeTarget.NetAmmoCapacity:
                value = data.Equipment.netGun.ammoCapacity;
                isInteger = true;
                return true;
            case NumericUpgradeTarget.PlasmaChargeSpeedMultiplier:
                value = data.Equipment.plasmaGun.chargeSpeedMultiplier;
                return true;
            case NumericUpgradeTarget.PlasmaDamage:
                value = data.Equipment.plasmaGun.tickDamage;
                isInteger = true;
                return true;
            case NumericUpgradeTarget.PlasmaTickSpeedMultiplier:
                value = data.Equipment.plasmaGun.tickSpeedMultiplier;
                return true;
            case NumericUpgradeTarget.PlasmaAttackRangeRatio:
                value = data.Equipment.plasmaGun.attackRangeRatio;
                return true;
            case NumericUpgradeTarget.PlasmaChainCount:
                value = data.Equipment.plasmaGun.chainCount;
                isInteger = true;
                return true;
            case NumericUpgradeTarget.PlasmaChainDamageRateRatio:
                value = data.Equipment.plasmaGun.chainedDamageRateRatio;
                return true;
            case NumericUpgradeTarget.PlasmaChainDetectRangeRatio:
                value = data.Equipment.plasmaGun.chainRangeRatio;
                return true;
            case NumericUpgradeTarget.PlasmaAmmoCapacity:
                value = data.Equipment.plasmaGun.ammoCapacity;
                isInteger = true;
                return true;
            case NumericUpgradeTarget.CreatureSlotCapacity:
                value = data.Inventory.CreatureSlotCapacity;
                isInteger = true;
                return true;
            case NumericUpgradeTarget.CreatureMaxStackCount:
                value = data.Inventory.CreatureMaxStackCount;
                isInteger = true;
                return true;
            case NumericUpgradeTarget.TimeoutInventoryLossRatio:
                value = data.Inventory.TimeoutInventoryLossRatio;
                return true;
            default:
                value = 0f;
                return false;
        }
    }

    private int GetMinimumInt()
    {
        return _target switch
        {
            NumericUpgradeTarget.NetCaptureCount => 1,
            NumericUpgradeTarget.NetCount => 1,
            NumericUpgradeTarget.CreatureSlotCapacity => 1,
            NumericUpgradeTarget.CreatureMaxStackCount => 1,
            _ => 0
        };
    }

    private float GetMinimumFloat()
    {
        return 0f;
    }

    private float ApplyFloat(float current, float minimum)
    {
        float result = _operation switch
        {
            NumericUpgradeOperation.Add => current + _value,
            NumericUpgradeOperation.Multiply => current * _value,
            NumericUpgradeOperation.Set => _value,
            _ => current
        };

        return Mathf.Max(minimum, result);
    }

    private int ApplyInt(int current, int minimum)
    {
        float result = _operation switch
        {
            NumericUpgradeOperation.Add => current + _value,
            NumericUpgradeOperation.Multiply => current * _value,
            NumericUpgradeOperation.Set => _value,
            _ => current
        };

        return Mathf.Max(minimum, Mathf.RoundToInt(result));
    }
}
