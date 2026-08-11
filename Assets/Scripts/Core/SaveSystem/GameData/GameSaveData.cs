using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 영구 저장될 게임 데이터
/// 보유 자원, 업그레이드, 진행한 이벤트 등
/// </summary>
[Serializable]
public sealed class GameSaveData
{
    public const int CurrentSchemaVersion = 8;

    public int schemaVersion = CurrentSchemaVersion;
    public InventoryData inventory = new();
    public List<UpgradeNodeSaveData> upgradeNodes = new();
    [HideInInspector] public List<string> unlockedUpgradeIds = new();
    public List<GameProgressEventId> completedEvents = new();

    public void RepairAfterLoad()
    {
        schemaVersion = Mathf.Max(CurrentSchemaVersion, schemaVersion);
        inventory ??= new InventoryData();
        upgradeNodes ??= new List<UpgradeNodeSaveData>();
        unlockedUpgradeIds ??= new List<string>();
        completedEvents ??= new List<GameProgressEventId>();

        inventory.RepairAfterLoad();
        NormalizeUpgradeNodes();
        MigrateLegacyUpgradeIds();
        NormalizeCompletedEvents();
    }

    public GameSaveData Clone()
    {
        return JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(this));
    }

    public bool TryValidate(out string error)
    {
        if (inventory == null)
        {
            error = "Inventory data is null.";
            return false;
        }

        if (!inventory.TryValidate(out error))
        {
            return false;
        }

        if (upgradeNodes == null ||
            unlockedUpgradeIds == null ||
            completedEvents == null)
        {
            error = "One or more save data containers are null.";
            return false;
        }

        error = null;
        return true;
    }

    private void NormalizeUpgradeNodes()
    {
        Dictionary<string, int> levels = new(StringComparer.Ordinal);
        for (int i = 0; i < upgradeNodes.Count; i++)
        {
            UpgradeNodeSaveData entry = upgradeNodes[i];
            string id = entry.nodeId?.Trim();
            if (string.IsNullOrEmpty(id) || entry.level <= 0)
            {
                continue;
            }

            if (!levels.TryGetValue(id, out int currentLevel) || entry.level > currentLevel)
            {
                levels[id] = entry.level;
            }
        }

        upgradeNodes.Clear();
        foreach (KeyValuePair<string, int> pair in levels)
        {
            upgradeNodes.Add(new UpgradeNodeSaveData
            {
                nodeId = pair.Key,
                level = pair.Value
            });
        }

        upgradeNodes.Sort((left, right) => string.CompareOrdinal(left.nodeId, right.nodeId));
    }

    private void MigrateLegacyUpgradeIds()
    {
        NormalizeUniqueIds(unlockedUpgradeIds);
        for (int i = 0; i < unlockedUpgradeIds.Count; i++)
        {
            string id = unlockedUpgradeIds[i];
            bool alreadyMigrated = false;
            for (int nodeIndex = 0; nodeIndex < upgradeNodes.Count; nodeIndex++)
            {
                if (string.Equals(upgradeNodes[nodeIndex].nodeId, id, StringComparison.Ordinal))
                {
                    alreadyMigrated = true;
                    break;
                }
            }

            if (!alreadyMigrated)
            {
                upgradeNodes.Add(new UpgradeNodeSaveData
                {
                    nodeId = id,
                    level = 1
                });
            }
        }

        unlockedUpgradeIds.Clear();
        upgradeNodes.Sort((left, right) => string.CompareOrdinal(left.nodeId, right.nodeId));
    }

    private void NormalizeCompletedEvents()
    {
        HashSet<GameProgressEventId> uniqueEvents = new();
        for (int i = completedEvents.Count - 1; i >= 0; i--)
        {
            GameProgressEventId eventId = completedEvents[i];
            if (eventId == GameProgressEventId.None ||
                !Enum.IsDefined(typeof(GameProgressEventId), eventId) ||
                !uniqueEvents.Add(eventId))
            {
                completedEvents.RemoveAt(i);
            }
        }

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
