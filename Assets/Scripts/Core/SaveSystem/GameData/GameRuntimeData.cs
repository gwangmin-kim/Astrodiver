using System;

/// <summary>
/// 기본값과 저장된 진행 상태로부터 구축된 인게임 런타임 데이터의 루트입니다.
/// </summary>
[Serializable]
public sealed class GameRuntimeData
{
    public GameRuntimeData(
        PlayerStatsRuntimeData playerStats,
        EquipmentRuntimeData equipment,
        InventoryRuntimeData inventory,
        FacilityRuntimeData facilities)
    {
        PlayerStats = playerStats ?? throw new ArgumentNullException(nameof(playerStats));
        Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Facilities = facilities ?? throw new ArgumentNullException(nameof(facilities));
        FloatageDropMultipliers = new FloatageDropMultiplierRuntimeData();
        StageRespawnProbabilityBonuses =
            new StageRespawnProbabilityBonusRuntimeData();
    }

    public PlayerStatsRuntimeData PlayerStats { get; }
    public EquipmentRuntimeData Equipment { get; }
    public InventoryRuntimeData Inventory { get; }
    public FacilityRuntimeData Facilities { get; }
    public FloatageDropMultiplierRuntimeData FloatageDropMultipliers { get; }
    public StageRespawnProbabilityBonusRuntimeData
        StageRespawnProbabilityBonuses { get; }
}
