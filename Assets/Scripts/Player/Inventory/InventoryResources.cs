using System;
using System.Collections.Generic;
using Astrodiver.Persistence.GameData;
using Astrodiver.Upgrades;

namespace Astrodiver.Player.Inventory
{
    /// <summary>
    /// 자원 보관 위치와 비용 계산을 담당<br/>
    /// 저장과 변경 알림은 호출자가 처리
    /// </summary>
    public sealed class InventoryResources
    {
        private readonly Dictionary<string, int> _resourceCostBuffer = new(StringComparer.Ordinal);

        public InventoryData SelectCollectionInventory(
            InventoryData carried, InventoryData chest, bool isChestUnlocked, bool isExploring)
        {
            return isChestUnlocked && !isExploring ? chest : carried;
        }

        public InventoryData SelectSpendableInventory(
            InventoryData carried, InventoryData chest, bool isChestUnlocked)
        {
            return isChestUnlocked ? chest : carried;
        }

        public int GetAmount(InventoryData inventory, string resourceId)
        {
            return inventory?.GetResourceAmount(resourceId) ?? 0;
        }

        public void Add(InventoryData inventory, string resourceId, int amount)
        {
            inventory.AddResource(resourceId, amount);
        }

        public bool CanAfford(InventoryData inventory, IReadOnlyList<UpgradeResourceCost> costs)
        {
            if (inventory == null || !TryAggregateCosts(costs))
            {
                return false;
            }

            foreach (KeyValuePair<string, int> cost in _resourceCostBuffer)
            {
                if (inventory.GetResourceAmount(cost.Key) < cost.Value)
                {
                    return false;
                }
            }
            return true;
        }

        public bool TrySpend(InventoryData inventory, string resourceId, int amount)
        {
            return inventory != null && inventory.TrySpendResource(resourceId, amount);
        }

        public bool TrySpend(InventoryData inventory, IReadOnlyList<UpgradeResourceCost> costs)
        {
            // 전체 비용을 먼저 검사하여 부족한 항목이 있으면 아무 자원도 차감하지 않습니다.
            if (!CanAfford(inventory, costs))
            {
                return false;
            }

            foreach (KeyValuePair<string, int> cost in _resourceCostBuffer)
            {
                if (!inventory.TrySpendResource(cost.Key, cost.Value))
                {
                    return false;
                }
            }
            return true;
        }

        private bool TryAggregateCosts(IReadOnlyList<UpgradeResourceCost> costs)
        {
            _resourceCostBuffer.Clear();
            if (costs == null)
            {
                return false;
            }

            for (int i = 0; i < costs.Count; i++)
            {
                UpgradeResourceCost cost = costs[i];
                if (cost.Resource == null || string.IsNullOrWhiteSpace(cost.Resource.Id) ||
                    cost.Amount <= 0)
                {
                    return false;
                }

                string id = cost.Resource.Id.Trim();
                _resourceCostBuffer.TryGetValue(id, out int current);
                _resourceCostBuffer[id] = current > int.MaxValue - cost.Amount
                    ? int.MaxValue
                    : current + cost.Amount;
            }

            return true;
        }
    }
}
