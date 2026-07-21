using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GameSaveData
{
    public const int CurrentSchemaVersion = 2;

    public int schemaVersion = CurrentSchemaVersion;
    public InventorySaveData inventory = new();
    public PlayerStatsSaveData playerStats = new();
    public EquipmentSaveData equipment = new();
    public List<string> unlockedUpgradeIds = new();
    public List<string> completedEventIds = new();

    public void Normalize()
    {
        schemaVersion = Mathf.Max(CurrentSchemaVersion, schemaVersion);
        inventory ??= new InventorySaveData();
        playerStats ??= new PlayerStatsSaveData();
        equipment ??= new EquipmentSaveData();
        unlockedUpgradeIds ??= new List<string>();
        completedEventIds ??= new List<string>();

        inventory.Normalize();
        NormalizeUniqueIds(unlockedUpgradeIds);
        NormalizeUniqueIds(completedEventIds);
    }

    private static void NormalizeUniqueIds(List<string> ids)
    {
        HashSet<string> uniqueIds = new(StringComparer.Ordinal);

        for (int i = ids.Count - 1; i >= 0; i--)
        {
            string normalizedId = ids[i]?.Trim();
            if (string.IsNullOrEmpty(normalizedId) || !uniqueIds.Add(normalizedId))
            {
                ids.RemoveAt(i);
                continue;
            }

            ids[i] = normalizedId;
        }

        ids.Sort(StringComparer.Ordinal);
    }
}

[Serializable]
public sealed class InventorySaveData
{
    public bool initialized;
    public List<CreatureSlotSaveData> creatureSlots = new();
    public List<ResourceAmountSaveData> resourceAmounts = new();

    public void Normalize()
    {
        creatureSlots ??= new List<CreatureSlotSaveData>();
        resourceAmounts ??= new List<ResourceAmountSaveData>();

        // 생물 자원 처리
        for (int i = 0; i < creatureSlots.Count; i++)
        {
            CreatureSlotSaveData slot = creatureSlots[i];
            slot.definitionId = slot.definitionId?.Trim();
            slot.count = Mathf.Max(0, slot.count);

            // 빈 슬롯의 위치도 유지하기 위한 장치
            if (string.IsNullOrEmpty(slot.definitionId) || slot.count == 0)
            {
                slot.definitionId = string.Empty;
                slot.count = 0;
            }

            creatureSlots[i] = slot;
        }

        // 파편 자원 처리
        Dictionary<string, int> mergedAmounts = new(StringComparer.Ordinal);
        for (int i = 0; i < resourceAmounts.Count; i++)
        {
            ResourceAmountSaveData entry = resourceAmounts[i];
            string id = entry.definitionId?.Trim();
            if (string.IsNullOrEmpty(id) || entry.amount <= 0)
            {
                continue;
            }

            int currentAmount = mergedAmounts.TryGetValue(id, out int current) ? current : 0;
            mergedAmounts[id] = currentAmount > int.MaxValue - entry.amount
                ? int.MaxValue
                : currentAmount + entry.amount;
        }

        resourceAmounts.Clear();
        foreach (KeyValuePair<string, int> pair in mergedAmounts)
        {
            resourceAmounts.Add(new ResourceAmountSaveData
            {
                definitionId = pair.Key,
                amount = pair.Value
            });
        }

        resourceAmounts.Sort((left, right) =>
            string.CompareOrdinal(left.definitionId, right.definitionId));
    }
}

[Serializable]
public struct CreatureSlotSaveData
{
    public string definitionId;
    public int count;
}

[Serializable]
public struct ResourceAmountSaveData
{
    public string definitionId;
    public int amount;
}

[Serializable]
public sealed class PlayerStatsSaveData
{
    public bool movementInitialized;
    public PlayerMovementData movement;
    public bool batteryInitialized;
    public BatteryData battery;
    public bool magnetInitialized;
    public MagnetData magnet;
}

[Serializable]
public sealed class EquipmentSaveData
{
    public bool netGunInitialized;
    public NetGunData netGun;
    public bool plasmaGunInitialized;
    public PlasmaGunData plasmaGun;
}
