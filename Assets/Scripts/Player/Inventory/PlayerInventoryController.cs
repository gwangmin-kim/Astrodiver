using Astrodiver.Definitions;
using Astrodiver.Persistence;
using Astrodiver.Persistence.GameData;
using Astrodiver.Upgrades;

namespace Astrodiver.Player.Inventory
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public sealed class PlayerInventoryController : MonoBehaviour
    {
        public static PlayerInventoryController Instance { get; private set; }

        private readonly Dictionary<string, int> _resourceCostBuffer =
            new(StringComparer.Ordinal);

        private InventoryData _inventory;
        private InventoryData _resourceChest;
        private GameDataManager _gameDataManager;
        private readonly CreatureInventory _creatures = new();
        private ExplorationInventorySession _explorationSession;
        private bool IsExploreSessionActive => _explorationSession?.IsActive == true;
        private bool _hasPendingChestResourcePopupRequest;

        public event Action Changed; // 자원 변경이 일어났을 때 호출
        public event Action ChestResourcesChanged;

        public IReadOnlyList<CreatureInventorySlot> CreatureSlots =>
            _creatures.Slots;

        public int CreatureSlotCapacity => _creatures.Capacity;
        public int CreatureMaxStackCount =>
            _gameDataManager?.RuntimeData?.Inventory?.CreatureMaxStackCount ?? 10;
        public bool IsInitialized { get; private set; }

        public IReadOnlyList<ResourceInventoryEntry> ResourceAmounts =>
            _inventory?.ResourceAmounts;

        public IReadOnlyList<ResourceInventoryEntry> ChestResourceAmounts =>
            _resourceChest?.ResourceAmounts;

        public bool IsResourceChestUnlocked =>
            _gameDataManager?.RuntimeData?.Facilities?.ResourceChestUnlocked ?? false;

        private InventoryData SpendableResourceInventory =>
            IsResourceChestUnlocked ? _resourceChest : _inventory;

        private InventoryData ResourceCollectionInventory =>
            IsResourceChestUnlocked && !IsExploreSessionActive
                ? _resourceChest
                : _inventory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _gameDataManager = GameDataManager.Instance;
            if (_gameDataManager == null)
            {
                Debug.LogError("Cannot initialize inventory because GameDataManager is missing.", this);
                enabled = false;
                return;
            }

            _explorationSession = new ExplorationInventorySession(
                new GameDataExplorationInventorySave(_gameDataManager),
                _creatures,
                data => InitializeCreatureSlots(
                    _gameDataManager.RuntimeData?.Inventory.CreatureSlotCapacity ?? 1,
                    data.Creatures));

            _gameDataManager.DataChanged += OnGameDataChanged;
            _gameDataManager.RuntimeDataChanged += OnRuntimeDataChanged;
            if (_gameDataManager.IsInitialized)
            {
                InitializeFromGameData(_gameDataManager.SaveData);
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            if (_gameDataManager != null)
            {
                _gameDataManager.DataChanged -= OnGameDataChanged;
                _gameDataManager.RuntimeDataChanged -= OnRuntimeDataChanged;
                if (IsExploreSessionActive)
                {
                    _gameDataManager.EndSaveSuspension();
                }
            }

            Instance = null;
            IsInitialized = false;
        }

        /// <summary>
        /// 인벤토리에 생물 추가 요청
        /// </summary>
        public bool TryAddCreature(CreatureDefinition creature)
        {
            if (_inventory == null || creature == null || string.IsNullOrWhiteSpace(creature.Id))
            {
                return false;
            }

            if (!_creatures.CanAdd(creature.Id, CreatureMaxStackCount))
            {
                Debug.Log("Inventory: Cannot Find Available Slot.", this);
                return false;
            }
            InventoryData snapshot = CreateHubRollbackSnapshot(_inventory);
            bool wasDirty = GameDataManager.Instance.HasUnsavedChanges;
            _creatures.TryAdd(creature.Id, CreatureMaxStackCount);
            SyncCreatureSaveData();

            if (!CompleteInventoryMutation(_inventory, snapshot, wasDirty))
            {
                return false;
            }

            GameDataManager.Instance?.CompleteEventAndSave(
                GameProgressEventId.CaptureFirstCreature);
            return true;
        }

        /// <summary>
        /// 인벤토리에 자원 추가 요청
        /// </summary>
        public int GetCreatureCount(CreatureDefinition creature)
        {
            if (_inventory == null || creature == null ||
                string.IsNullOrWhiteSpace(creature.Id))
            {
                return 0;
            }

            return _creatures.GetCount(creature.Id.Trim());
        }

        public int TakeCreature(CreatureDefinition creature, int requestedAmount)
        {
            if (_inventory == null || IsExploreSessionActive ||
                creature == null || string.IsNullOrWhiteSpace(creature.Id) ||
                requestedAmount <= 0)
            {
                return 0;
            }

            InventoryData snapshot = _inventory.Clone();
            bool wasDirty = GameDataManager.Instance.HasUnsavedChanges;
            int takenAmount = TakeCreatureInternal(
                creature.Id.Trim(),
                requestedAmount);

            if (takenAmount == 0)
            {
                return 0;
            }

            return CompleteHubSpend(_inventory, snapshot, wasDirty)
                ? takenAmount
                : 0;
        }

        internal bool TryPeekLeftmostCreatureForWorktable(out string creatureId)
        {
            creatureId = null;
            return _inventory != null && !IsExploreSessionActive && _creatures.TryPeekFirst(out creatureId);
        }

        internal InventoryData CreateWorktableTransferSnapshot()
        {
            return _inventory?.Clone();
        }

        internal bool TryRemoveLeftmostCreatureForWorktable(string expectedCreatureId)
        {
            if (_inventory == null || IsExploreSessionActive || !_creatures.TryRemoveFirst(expectedCreatureId))
            {
                return false;
            }
            SyncCreatureSaveData();
            return true;
        }

        internal void RestoreWorktableTransferSnapshot(InventoryData snapshot)
        {
            if (_inventory == null || snapshot == null)
            {
                return;
            }

            _inventory.CopyFrom(snapshot);
            InitializeCreatureSlots(
                _gameDataManager?.RuntimeData?.Inventory?.CreatureSlotCapacity ?? 1,
                _inventory.Creatures);
        }

        internal void NotifyWorktableTransferCommitted()
        {
            Changed?.Invoke();
        }

        internal void NotifyWorktableProcessingCommitted()
        {
            Changed?.Invoke();
            NotifyChestResourcesChanged();
        }

        internal bool ConsumePendingChestResourcePopupRequest()
        {
            bool hasRequest = _hasPendingChestResourcePopupRequest;
            _hasPendingChestResourcePopupRequest = false;
            return hasRequest;
        }

        public bool TryAddResource(ResourceDefinition resource, int amount = 1)
        {
            if (_inventory == null || resource == null ||
                string.IsNullOrWhiteSpace(resource.Id) || amount <= 0)
            {
                return false;
            }

            InventoryData destination = ResourceCollectionInventory;
            InventoryData snapshot = CreateHubRollbackSnapshot(destination);
            bool wasDirty = GameDataManager.Instance.HasUnsavedChanges;
            destination.AddResource(resource.Id, amount);
            if (!CompleteInventoryMutation(destination, snapshot, wasDirty))
            {
                return false;
            }

            if (ReferenceEquals(destination, _resourceChest))
            {
                NotifyChestResourcesChanged();
            }

            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.CompleteEventAndSave(
                    GameProgressEventId.GetFirstResource);
            }

            return true;
        }

        public int GetResourceAmount(ResourceDefinition resource)
        {
            return resource == null || _inventory == null
                ? 0
                : _inventory.GetResourceAmount(resource.Id);
        }

        public int GetChestResourceAmount(ResourceDefinition resource)
        {
            return resource == null || _resourceChest == null
                ? 0
                : _resourceChest.GetResourceAmount(resource.Id);
        }

        /// <summary>
        /// 업그레이드 지불 가능 여부 검사
        /// </summary>
        public bool CanAfford(IReadOnlyList<UpgradeResourceCost> costs)
        {
            InventoryData spendableInventory = SpendableResourceInventory;
            if (spendableInventory == null || !TryAggregateCosts(costs))
            {
                return false;
            }

            foreach (KeyValuePair<string, int> cost in _resourceCostBuffer)
            {
                if (spendableInventory.GetResourceAmount(cost.Key) < cost.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 자원 소비 (일반적인 형태)
        /// 세션 중에 호출 불가
        /// </summary>
        public bool TrySpendResource(ResourceDefinition resource, int amount)
        {
            if (_inventory == null || IsExploreSessionActive ||
                resource == null || amount <= 0)
            {
                return false;
            }

            InventoryData spendableInventory = SpendableResourceInventory;
            InventoryData snapshot = spendableInventory.Clone();
            bool wasDirty = GameDataManager.Instance.HasUnsavedChanges;
            if (!TrySpendResourceInternal(resource.Id, amount))
            {
                return false;
            }

            return CompleteHubSpend(spendableInventory, snapshot, wasDirty);
        }

        /// <summary>
        /// 자원 소비 (업그레이드 비용 전달 형태)
        /// 세션 중에 호출 불가
        /// </summary>
        public bool TrySpendResources(IReadOnlyList<UpgradeResourceCost> costs)
        {
            if (_inventory == null || IsExploreSessionActive)
            {
                return false;
            }

            InventoryData spendableInventory = SpendableResourceInventory;
            InventoryData snapshot = spendableInventory.Clone();
            bool wasDirty = GameDataManager.Instance.HasUnsavedChanges;
            if (!TrySpendResourcesForTransaction(costs))
            {
                return false;
            }

            return CompleteHubSpend(spendableInventory, snapshot, wasDirty);
        }

        internal bool TrySpendResourcesForTransaction(IReadOnlyList<UpgradeResourceCost> costs)
        {
            if (_inventory == null || IsExploreSessionActive || !CanAfford(costs))
            {
                return false;
            }

            foreach (KeyValuePair<string, int> cost in _resourceCostBuffer)
            {
                if (!TrySpendResourceInternal(cost.Key, cost.Value))
                {
                    return false;
                }
            }

            return true;
        }

        internal void NotifyTransactionCommitted()
        {
            Changed?.Invoke();
            if (IsResourceChestUnlocked)
            {
                NotifyChestResourcesChanged();
            }
        }

        public bool BeginExploreSession()
        {
            if (!IsInitialized || _inventory == null)
            {
                Debug.LogError("Cannot start exploration before inventory data is initialized.", this);
                return false;
            }
            if (IsExploreSessionActive)
            {
                Debug.LogWarning("An exploration session is already active.", this);
                return false;
            }
            if (!_explorationSession.Begin(_inventory, _resourceChest, IsResourceChestUnlocked,
                    out bool depositedResources, out string error))
            {
                Debug.LogError(error, this);
                return false;
            }
            if (depositedResources)
            {
                NotifyChestResourcesChanged();
            }
            return true;
        }

        public bool CompleteExploreSession(float lossRatio)
        {
            if (!IsExploreSessionActive)
            {
                Debug.LogWarning("Cannot complete exploration because no session is active.", this);
                return false;
            }
            if (!_explorationSession.Complete(lossRatio, IsResourceChestUnlocked,
                    out bool depositedResources))
            {
                return false;
            }
            Changed?.Invoke();
            if (depositedResources)
            {
                NotifyChestResourcesChanged();
            }
            return true;
        }

        public void CancelExploreSession()
        {
            if (_explorationSession != null && _explorationSession.Cancel())
            {
                Changed?.Invoke();
            }
        }

        public bool TryResolveCreatureDefinition(
            CreatureInventorySlot slot,
            out CreatureDefinition definition)
        {
            definition = null;
            return slot != null && !slot.IsEmpty &&
                GameDataManager.Instance.Definitions.TryGetCreature(slot.DefinitionId, out definition);
        }

        public bool TryResolveResourceDefinition(
            ResourceInventoryEntry entry,
            out ResourceDefinition definition)
        {
            definition = null;
            return entry != null && !entry.IsEmpty &&
                GameDataManager.Instance.Definitions.TryGetResource(entry.DefinitionId, out definition);
        }

        private int TakeCreatureInternal(string creatureId, int requestedAmount)
        {
            int taken = _creatures.Take(creatureId, requestedAmount);
            if (taken > 0)
            {
                SyncCreatureSaveData();
            }
            return taken;
        }

        private bool TryAggregateCosts(IReadOnlyList<UpgradeResourceCost> costs)
        {
            _resourceCostBuffer.Clear();
            if (_inventory == null || costs == null)
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

        private bool TrySpendResourceInternal(string resourceId, int amount)
        {
            return SpendableResourceInventory.TrySpendResource(resourceId, amount);
        }

        /// <summary>
        /// Hub(우주선)에서 저장 실패에 대비해 롤백 스냅샷 생성
        /// </summary>
        private InventoryData CreateHubRollbackSnapshot(InventoryData inventory)
        {
            return IsExploreSessionActive ? null : inventory.Clone();
        }

        private bool CompleteInventoryMutation(
            InventoryData mutatedInventory,
            InventoryData hubSnapshot,
            bool wasDirty)
        {
            GameDataManager manager = GameDataManager.Instance;
            manager.MarkDirty();

            if (!IsExploreSessionActive && !manager.SaveNow())
            {
                mutatedInventory.CopyFrom(hubSnapshot);
                InitializeCreatureSlots(
                    manager.RuntimeData?.Inventory.CreatureSlotCapacity ?? 1,
                    _inventory.Creatures);
                manager.RestoreDirtyState(wasDirty);
                return false;
            }

            Changed?.Invoke();
            return true;
        }

        private bool CompleteHubSpend(
            InventoryData spentInventory,
            InventoryData snapshot,
            bool wasDirty)
        {
            if (IsExploreSessionActive)
            {
                return false;
            }

            GameDataManager manager = GameDataManager.Instance;
            manager.MarkDirty();
            if (manager.SaveNow())
            {
                Changed?.Invoke();
                if (ReferenceEquals(spentInventory, _resourceChest))
                {
                    NotifyChestResourcesChanged();
                }
                return true;
            }

            spentInventory.CopyFrom(snapshot);
            InitializeCreatureSlots(
                manager.RuntimeData?.Inventory.CreatureSlotCapacity ?? 1,
                _inventory.Creatures);
            manager.RestoreDirtyState(wasDirty);
            return false;
        }

        private void NotifyChestResourcesChanged()
        {
            _hasPendingChestResourcePopupRequest = true;
            ChestResourcesChanged?.Invoke();
        }

        private void OnGameDataChanged(GameSaveData data)
        {
            InitializeFromGameData(data);
        }

        private void InitializeFromGameData(GameSaveData data)
        {
            _inventory = data?.inventory;
            _resourceChest = data?.resourceChest;
            if (_inventory == null || _resourceChest == null)
            {
                _creatures.Clear();
                IsInitialized = false;
                Changed?.Invoke();
                return;
            }

            InitializeCreatureSlots(
                _gameDataManager.RuntimeData?.Inventory.CreatureSlotCapacity ?? 1,
                _inventory.Creatures);
            IsInitialized = true;
            Changed?.Invoke();
        }

        private void OnRuntimeDataChanged(GameRuntimeData runtimeData)
        {
            if (!IsInitialized)
            {
                return;
            }

            int capacity = runtimeData?.Inventory?.CreatureSlotCapacity ?? 1;
            ResizeCreatureSlots(capacity);
            Changed?.Invoke();
        }

        private void InitializeCreatureSlots(
            int capacity,
            IReadOnlyList<CreatureInventoryEntry> savedEntries)
        {
            _creatures.Reset(capacity);
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

                if (!GameDataManager.Instance.Definitions.TryGetCreature(
                        entry.DefinitionId,
                        out CreatureDefinition definition))
                {
                    Debug.LogWarning(
                        $"Inventory: Saved creature '{entry.DefinitionId}' is not defined and was not loaded.",
                        this);
                    continue;
                }

                int remaining = _creatures.RestoreEntry(definition.Id, entry.Count, CreatureMaxStackCount);

                if (remaining > 0)
                {
                    Debug.LogWarning(
                        $"Inventory: {remaining} of creature '{entry.DefinitionId}' exceeded the " +
                        $"runtime capacity of {_creatures.Capacity} slots and was not loaded.",
                        this);
                }
            }
        }

        private void ResizeCreatureSlots(int capacity)
        {
            int normalizedCapacity = Mathf.Max(1, capacity);
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
                        Debug.LogWarning(
                            $"Inventory: Creature '{slot.DefinitionId}' in slot {i} exceeded the new " +
                            $"capacity of {normalizedCapacity} and was removed.",
                            this);
                    }
                }
            }

            _creatures.Resize(normalizedCapacity);

            SyncCreatureSaveData();
        }

        private void SyncCreatureSaveData()
        {
            _inventory?.SetCreatures(_creatures.Slots);
        }
    }
}
