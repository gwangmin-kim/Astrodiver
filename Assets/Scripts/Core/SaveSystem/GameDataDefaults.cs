using UnityEngine;

[CreateAssetMenu(
    fileName = "GameDataDefaults",
    menuName = "Astrodiver/Save System/Game Data Defaults")]
public sealed class GameDataDefaults : ScriptableObject
{
    [Header("Inventory")]
    [SerializeField, Min(1)] private int _creatureSlotCount;

    [Header("Player Stats")]
    [SerializeField] private PlayerMovementData _movement;
    [SerializeField] private BatteryData _battery;

    [Header("Equipment")]
    [SerializeField] private NetGunData _netGun;
    [SerializeField] private PlasmaGunData _plasmaGun;

    public GameSaveData CreateSaveData()
    {
        GameSaveData data = new();
        data.inventory.initialized = true;

        int slotCount = Mathf.Max(1, _creatureSlotCount);
        for (int i = 0; i < slotCount; i++)
        {
            data.inventory.creatureSlots.Add(new CreatureSlotSaveData
            {
                definitionId = string.Empty,
                count = 0
            });
        }

        data.playerStats.movementInitialized = true;
        data.playerStats.movement = _movement;
        data.playerStats.batteryInitialized = true;
        data.playerStats.battery = _battery;

        data.equipment.netGunInitialized = true;
        data.equipment.netGun = _netGun;
        data.equipment.plasmaGunInitialized = true;
        data.equipment.plasmaGun = _plasmaGun;
        data.Normalize();
        return data;
    }
}
