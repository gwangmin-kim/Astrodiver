using System;
using System.Collections.Generic;
using Astrodiver.Persistence.GameData;
using UnityEngine;

namespace Astrodiver.Player.Inventory
{
    /// <summary>
    /// 인벤토리 탐사 스냅샷 및 결산
    /// </summary>
    public sealed class ExplorationInventorySession
    {
        private readonly IExplorationInventorySave _save;
        private readonly CreatureInventory _creatures;
        private readonly Action<InventoryData> _restoreCreatureSlots;
        private InventoryData _inventory;
        private InventoryData _resourceChest;
        private InventoryData _sessionStartSnapshot;
        private bool _sessionStartWasDirty;

        public bool IsActive { get; private set; }

        public ExplorationInventorySession(
            IExplorationInventorySave save,
            CreatureInventory creatures,
            Action<InventoryData> restoreCreatureSlots)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _creatures = creatures ?? throw new ArgumentNullException(nameof(creatures));
            _restoreCreatureSlots = restoreCreatureSlots ??
                throw new ArgumentNullException(nameof(restoreCreatureSlots));
        }

        public bool Begin(
            InventoryData inventory,
            InventoryData resourceChest,
            bool isChestUnlocked,
            out bool depositedResources,
            out string error)
        {
            depositedResources = false;
            error = null;
            if (IsActive)
            {
                error = "탐사 세션이 이미 시작 상태이다.";
                return false;
            }
            if (inventory == null || resourceChest == null)
            {
                error = "인벤토리 데이터가 초기화되지 않았다.";
                return false;
            }

            _inventory = inventory;
            _resourceChest = resourceChest;
            InventoryData playerBeforeDeposit = _inventory.Clone();
            InventoryData chestBeforeDeposit = _resourceChest.Clone();
            bool wasDirtyBeforeDeposit = _save.HasUnsavedChanges;
            depositedResources = DepositCarriedResourcesToChest(isChestUnlocked);
            if (depositedResources)
            {
                _save.MarkDirty();
            }

            if (_save.HasUnsavedChanges && !_save.SaveNow())
            {
                _inventory.CopyFrom(playerBeforeDeposit);
                _resourceChest.CopyFrom(chestBeforeDeposit);
                _save.RestoreDirtyState(wasDirtyBeforeDeposit);
                error = "Cannot start exploration with unsaved hub data.";
                return false;
            }

            // A successful hub save is not undone if suspension cannot begin.
            if (!_save.BeginSaveSuspension())
            {
                error = "Cannot suspend saving for the exploration session.";
                return false;
            }

            _sessionStartSnapshot = _inventory.Clone();
            _sessionStartWasDirty = _save.HasUnsavedChanges;
            IsActive = true;
            return true;
        }

        public bool Complete(float lossRatio, bool isChestUnlocked, out bool depositedResources)
        {
            depositedResources = false;
            if (!IsActive)
            {
                return false;
            }

            InventoryData runtimeBeforePenalty = _inventory.Clone();
            InventoryData chestBeforeDeposit = _resourceChest.Clone();
            bool wasDirty = _save.HasUnsavedChanges;
            ApplySessionLoss(lossRatio);
            depositedResources = DepositCarriedResourcesToChest(isChestUnlocked);
            _save.MarkDirty();

            if (!_save.SaveExplorationResult())
            {
                _inventory.CopyFrom(runtimeBeforePenalty);
                _resourceChest.CopyFrom(chestBeforeDeposit);
                _restoreCreatureSlots(_inventory);
                _save.RestoreDirtyState(wasDirty);
                return false;
            }

            ClearSessionState();
            return true;
        }

        public bool Cancel()
        {
            if (!IsActive)
            {
                return false;
            }

            _inventory.CopyFrom(_sessionStartSnapshot);
            _restoreCreatureSlots(_inventory);
            _save.RestoreDirtyState(_sessionStartWasDirty);
            ClearSessionState();
            return true;
        }

        private void ClearSessionState()
        {
            IsActive = false;
            _sessionStartSnapshot = null;
            _sessionStartWasDirty = false;
            // Subscribers must observe an inactive session when saving resumes.
            _save.EndSaveSuspension();
        }

        private bool DepositCarriedResourcesToChest(bool isChestUnlocked)
        {
            return isChestUnlocked && _inventory.TransferAllResourcesTo(_resourceChest);
        }

        private void ApplySessionLoss(float lossRatio)
        {
            float clampedLossRatio = Mathf.Clamp01(lossRatio);
            if (clampedLossRatio <= 0f)
            {
                return;
            }

            _creatures.ApplyLoss(clampedLossRatio);
            _inventory.SetCreatures(_creatures.Slots);

            List<ResourceInventoryEntry> resources = _inventory.MutableResourceAmounts;
            for (int i = resources.Count - 1; i >= 0; i--)
            {
                ResourceInventoryEntry entry = resources[i];
                if (entry == null || entry.IsEmpty)
                {
                    continue;
                }

                int lostAmount = Mathf.CeilToInt(entry.Amount * clampedLossRatio);
                _inventory.SetResourceAmount(entry.DefinitionId, entry.Amount - lostAmount);
            }
        }
    }
}
