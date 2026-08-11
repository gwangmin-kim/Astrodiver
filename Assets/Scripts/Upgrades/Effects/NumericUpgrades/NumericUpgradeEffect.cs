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
    [SerializeField] private NumericUpgradeTarget _target = NumericUpgradeTarget.MovementSpeed;
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

        UpgradeRuntimeData data = context.RuntimeData;
        PlayerStatsSaveData player = data.PlayerStats;
        EquipmentSaveData equipment = data.Equipment;
        InventoryRuntimeData inventory = data.Inventory;

        switch (_target)
        {
            case NumericUpgradeTarget.MovementSpeed:
                {
                    PlayerMovementData value = player.movement;
                    value.moveSpeed = ApplyFloat(value.moveSpeed, 0f);
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
            case NumericUpgradeTarget.MagnetRadius:
                {
                    MagnetData value = player.magnet;
                    ApplyMagnet(ref value);
                    player.magnet = value;
                    player.magnetInitialized = true;
                    break;
                }
            case NumericUpgradeTarget.NetCaptureCount:
            case NumericUpgradeTarget.NetCount:
            case NumericUpgradeTarget.NetRadius:
            case NumericUpgradeTarget.NetShootRange:
            case NumericUpgradeTarget.NetChargeTime:
            case NumericUpgradeTarget.NetCollectSpeed:
                {
                    NetGunData value = equipment.netGun;
                    ApplyNetGun(ref value);
                    equipment.netGun = value;
                    equipment.netGunInitialized = true;
                    break;
                }
            case NumericUpgradeTarget.PlasmaChargeTime:
            case NumericUpgradeTarget.PlasmaDamage:
            case NumericUpgradeTarget.PlasmaAttackInterval:
            case NumericUpgradeTarget.PlasmaAttackRange:
            case NumericUpgradeTarget.PlasmaChainCount:
            case NumericUpgradeTarget.PlasmaChainDamageRate:
            case NumericUpgradeTarget.PlasmaChainDetectRange:
                {
                    PlasmaGunData value = equipment.plasmaGun;
                    ApplyPlasmaGun(ref value);
                    equipment.plasmaGun = value;
                    equipment.plasmaGunInitialized = true;
                    break;
                }
            case NumericUpgradeTarget.CreatureSlotCapacity:
                inventory.CreatureSlotCapacity =
                    ApplyInt(inventory.CreatureSlotCapacity, 1);
                break;
            default:
                error = $"Unsupported numeric upgrade target '{_target}'.";
                return false;
        }

        error = null;
        return true;
    }

    private void ApplyMagnet(ref MagnetData data)
    {
        switch (_target)
        {
            case NumericUpgradeTarget.MagnetRadius:
                data.radius = ApplyFloat(data.radius, 0f);
                break;
        }
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
            case NumericUpgradeTarget.NetRadius:
                data.netData.radius = ApplyFloat(data.netData.radius, 0f);
                break;
            case NumericUpgradeTarget.NetShootRange:
                data.maxShootRange = ApplyFloat(data.maxShootRange, 0f);
                break;
            case NumericUpgradeTarget.NetChargeTime:
                data.chargeTime = ApplyFloat(data.chargeTime, 0f);
                break;
            case NumericUpgradeTarget.NetCollectSpeed:
                data.collectSpeed = ApplyFloat(data.collectSpeed, 0f);
                break;
        }
    }

    private void ApplyPlasmaGun(ref PlasmaGunData data)
    {
        switch (_target)
        {
            case NumericUpgradeTarget.PlasmaChargeTime:
                data.chargeTime = ApplyFloat(data.chargeTime, 0f);
                break;
            case NumericUpgradeTarget.PlasmaDamage:
                data.tickDamage = ApplyFloat(data.tickDamage, 0f);
                break;
            case NumericUpgradeTarget.PlasmaAttackInterval:
                data.tickInterval = ApplyFloat(data.tickInterval, 0f);
                break;
            case NumericUpgradeTarget.PlasmaAttackRange:
                data.attackRange = ApplyFloat(data.attackRange, 0f);
                break;
            case NumericUpgradeTarget.PlasmaChainCount:
                data.chainCount = ApplyInt(data.chainCount, 0);
                break;
            case NumericUpgradeTarget.PlasmaChainDamageRate:
                data.chainedDamageRate = ApplyFloat(data.chainedDamageRate, 0f);
                break;
            case NumericUpgradeTarget.PlasmaChainDetectRange:
                data.chainDetectRange = ApplyFloat(data.chainDetectRange, 0f);
                break;
        }
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
