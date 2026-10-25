using System;
using System.Collections.Generic;
using Astrodiver.Persistence.GameData;

namespace Astrodiver.Player.Inventory
{
    /// <summary>
    /// 저장 데이터 연결, 생물 슬롯 변환, 저장 실패 시 복원 담당
    /// </summary>
    public sealed class InventoryPersistence
    {
        private readonly CreatureInventory _creatures;
        private readonly IInventorySave _save;
        private readonly Func<InventoryRuntimeData> _settings;
        private readonly Func<string, string> _resolveCreatureId;
        private readonly Action<string> _warn;

        public InventoryData Carried { get; private set; }
        public InventoryData Chest { get; private set; }
        public bool IsInitialized => Carried != null && Chest != null;
        public bool HasUnsavedChanges => _save.HasUnsavedChanges;

        public InventoryPersistence(
            CreatureInventory creatures,
            IInventorySave save,
            Func<InventoryRuntimeData> settings,
            Func<string, string> resolveCreatureId,
            Action<string> warn)
        {
            _creatures = creatures ?? throw new ArgumentNullException(nameof(creatures));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _resolveCreatureId = resolveCreatureId ?? throw new ArgumentNullException(nameof(resolveCreatureId));
            _warn = warn;
        }

        public void Load(GameSaveData data)
        {
            Carried = data?.inventory;
            Chest = data?.resourceChest;
            if (!IsInitialized)
            {
                _creatures.Clear();
                return;
            }
            RestoreCreatureSlots(Carried);
        }

        public InventoryData CreateSnapshot(InventoryData inventory, bool isExploring)
        {
            return isExploring ? null : inventory?.Clone();
        }

        public void RestoreCarriedSnapshot(InventoryData snapshot)
        {
            if (Carried == null || snapshot == null)
            {
                return;
            }
            Carried.CopyFrom(snapshot);
            RestoreCreatureSlots(Carried);
        }

        // 탐사 중에는 메모리 변경만 기록하고 정산 시점까지 파일 저장을 미룹니다.
        public bool CompleteMutation(
            InventoryData mutated, InventoryData snapshot, bool wasDirty, bool isExploring)
        {
            _save.MarkDirty();
            if (!isExploring && !_save.SaveNow())
            {
                mutated.CopyFrom(snapshot);
                // 상자 변경 실패여도 기존과 동일하게 휴대 생물 슬롯을 재구성합니다.
                RestoreCreatureSlots(Carried);
                _save.RestoreDirtyState(wasDirty);
                return false;
            }
            return true;
        }

        public void SyncCreatureSaveData()
        {
            Carried?.SetCreatures(_creatures.Slots);
        }

        public void RestoreCreatureSlots(InventoryData inventory)
        {
            _creatures.Reset(_settings()?.CreatureSlotCapacity ?? 1);
            IReadOnlyList<CreatureInventoryEntry> savedEntries = inventory?.Creatures;
            if (savedEntries == null)
            {
                return;
            }

            for (int entryIndex = 0; entryIndex < savedEntries.Count; entryIndex++)
            {
                CreatureInventoryEntry entry = savedEntries[entryIndex];
                if (entry == null || entry.IsEmpty)
                {
                    continue;
                }

                string definitionId = _resolveCreatureId(entry.DefinitionId);
                if (definitionId == null)
                {
                    _warn?.Invoke(
                        $"Inventory: Saved creature '{entry.DefinitionId}' is not defined and was not loaded.");
                    continue;
                }

                int remaining = _creatures.RestoreEntry(definitionId, entry.Count, _settings()?.CreatureMaxStackCount ?? 10);

                if (remaining > 0)
                {
                    _warn?.Invoke(
                        $"Inventory: {remaining} of creature '{entry.DefinitionId}' exceeded the " +
                        $"runtime capacity of {_creatures.Capacity} slots and was not loaded.");
                }
            }
        }

        public void ResizeCreatureSlots(int capacity)
        {
            int normalizedCapacity = Math.Max(1, capacity);
            if (_creatures.Capacity == normalizedCapacity)
            {
                return;
            }

            if (normalizedCapacity < _creatures.Capacity)
            {
                for (int i = normalizedCapacity; i < _creatures.Capacity; i++)
                {
                    CreatureInventorySlot slot = _creatures.Slots[i];
                    if (slot != null && !slot.IsEmpty)
                    {
                        _warn?.Invoke(
                            $"Inventory: Creature '{slot.DefinitionId}' in slot {i} exceeded the new " +
                            $"capacity of {normalizedCapacity} and was removed.");
                    }
                }
            }

            _creatures.Resize(normalizedCapacity);

            SyncCreatureSaveData();
        }
    }
}
