public enum NumericUpgradeTarget
{
    // Player movement: 1000-1099
    MovementSpeedRatio = 1000,

    // Battery: 1100-1199
    BatteryCapacity = 1100,

    // Magnet: 1200-1299
    MagnetRadiusRatio = 1200,

    // Net gun: 2000-2099
    NetCaptureCount = 2000,
    NetCount = 2001,
    NetRadiusRatio = 2002,
    NetShootRangeRatio = 2003,
    NetChargeTimeRatio = 2004,
    NetCollectSpeedRatio = 2005,
    NetAmmoCapacity = 2006,

    // Plasma gun: 3000-3099
    PlasmaChargeSpeedMultiplier = 3000,
    PlasmaDamage = 3001,
    PlasmaTickSpeedMultiplier = 3002,
    PlasmaAttackRangeRatio = 3003,
    PlasmaChainCount = 3004,
    PlasmaChainDamageRateRatio = 3005,
    PlasmaChainDetectRangeRatio = 3006,
    PlasmaAmmoCapacity = 3007,

    // Inventory: 4000-4099
    CreatureSlotCapacity = 4000,
    CreatureMaxStackCount = 4001,
    TimeoutInventoryLossRatio = 4002,
}
