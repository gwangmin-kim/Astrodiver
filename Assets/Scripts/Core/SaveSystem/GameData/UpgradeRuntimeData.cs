using System;

[Serializable]
public sealed class UpgradeRuntimeData
{
    public UpgradeRuntimeData(
        PlayerStatsSaveData playerStats,
        EquipmentSaveData equipment,
        InventoryRuntimeData inventory)
    {
        PlayerStats = playerStats ?? new PlayerStatsSaveData();
        Equipment = equipment ?? new EquipmentSaveData();
        Inventory = inventory ?? new InventoryRuntimeData();
    }

    public PlayerStatsSaveData PlayerStats { get; }
    public EquipmentSaveData Equipment { get; }
    public InventoryRuntimeData Inventory { get; }
}
