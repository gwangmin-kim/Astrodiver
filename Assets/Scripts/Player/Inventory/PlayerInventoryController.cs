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

        private readonly InventoryResources _resources = new();

        private InventoryPersistence _persistence;
        private InventoryData Inventory => _persistence?.Carried;
        private InventoryData ResourceChest => _persistence?.Chest;
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
            Inventory?.ResourceAmounts;

        public IReadOnlyList<ResourceInventoryEntry> ChestResourceAmounts =>
            ResourceChest?.ResourceAmounts;

        public bool IsResourceChestUnlocked =>
            _gameDataManager?.RuntimeData?.Facilities?.ResourceChestUnlocked ?? false;

        private InventoryData SpendableResourceInventory =>
            _resources.SelectSpendableInventory(Inventory, ResourceChest, IsResourceChestUnlocked);

        private InventoryData ResourceCollectionInventory =>
            _resources.SelectCollectionInventory(
                Inventory, ResourceChest, IsResourceChestUnlocked, IsExploreSessionActive);

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

            var save = new GameDataExplorationInventorySave(_gameDataManager);
            _persistence = new InventoryPersistence(
                _creatures,
                save,
                () => _gameDataManager.RuntimeData?.Inventory,
                id => _gameDataManager.Definitions.TryGetCreature(id, out var definition)
                    ? definition.Id : null,
                message => Debug.LogWarning(message, this));
            _explorationSession = new ExplorationInventorySession(
                save, _creatures, _persistence.RestoreCreatureSlots);

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
            if (Inventory == null || creature == null || string.IsNullOrWhiteSpace(creature.Id))
            {
                return false;
            }

            if (!_creatures.CanAdd(creature.Id, CreatureMaxStackCount))
            {
                Debug.Log("Inventory: Cannot Find Available Slot.", this);
                return false;
            }
            InventoryData snapshot = CreateHubRollbackSnapshot(Inventory);
            bool wasDirty = _persistence.HasUnsavedChanges;
            _creatures.TryAdd(creature.Id, CreatureMaxStackCount);
            SyncCreatureSaveData();

            if (!CompleteInventoryMutation(Inventory, snapshot, wasDirty))
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
            if (Inventory == null || creature == null ||
                string.IsNullOrWhiteSpace(creature.Id))
            {
                return 0;
            }

            return _creatures.GetCount(creature.Id.Trim());
        }

        public int TakeCreature(CreatureDefinition creature, int requestedAmount)
        {
            if (Inventory == null || IsExploreSessionActive ||
                creature == null || string.IsNullOrWhiteSpace(creature.Id) ||
                requestedAmount <= 0)
            {
                return 0;
            }

            InventoryData snapshot = _persistence.CreateSnapshot(Inventory, false);
            bool wasDirty = _persistence.HasUnsavedChanges;
            int takenAmount = TakeCreatureInternal(
                creature.Id.Trim(),
                requestedAmount);

            if (takenAmount == 0)
            {
                return 0;
            }

            return CompleteHubSpend(Inventory, snapshot, wasDirty)
                ? takenAmount
                : 0;
        }

        internal bool TryPeekLeftmostCreatureForWorktable(out string creatureId)
        {
            creatureId = null;
            return Inventory != null && !IsExploreSessionActive && _creatures.TryPeekFirst(out creatureId);
        }

        internal InventoryData CreateWorktableTransferSnapshot()
        {
            return _persistence?.CreateSnapshot(Inventory, false);
        }

        internal bool TryRemoveLeftmostCreatureForWorktable(string expectedCreatureId)
        {
            if (Inventory == null || IsExploreSessionActive || !_creatures.TryRemoveFirst(expectedCreatureId))
            {
                return false;
            }
            SyncCreatureSaveData();
            return true;
        }

        internal void RestoreWorktableTransferSnapshot(InventoryData snapshot)
        {
            _persistence?.RestoreCarriedSnapshot(snapshot);
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
            if (Inventory == null || resource == null ||
                string.IsNullOrWhiteSpace(resource.Id) || amount <= 0)
            {
                return false;
            }

            InventoryData destination = ResourceCollectionInventory;
            InventoryData snapshot = CreateHubRollbackSnapshot(destination);
            bool wasDirty = _persistence.HasUnsavedChanges;
            _resources.Add(destination, resource.Id, amount);
            if (!CompleteInventoryMutation(destination, snapshot, wasDirty))
            {
                return false;
            }

            if (ReferenceEquals(destination, ResourceChest))
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
            return resource == null || Inventory == null
                ? 0
                : _resources.GetAmount(Inventory, resource.Id);
        }

        public int GetChestResourceAmount(ResourceDefinition resource)
        {
            return resource == null || ResourceChest == null
                ? 0
                : _resources.GetAmount(ResourceChest, resource.Id);
        }

        /// <summary>
        /// 업그레이드 지불 가능 여부 검사
        /// </summary>
        public bool CanAfford(IReadOnlyList<UpgradeResourceCost> costs)
        {
            return Inventory != null && _resources.CanAfford(SpendableResourceInventory, costs);
        }

        /// <summary>
        /// 자원 소비 (일반적인 형태)
        /// 세션 중에 호출 불가
        /// </summary>
        public bool TrySpendResource(ResourceDefinition resource, int amount)
        {
            if (Inventory == null || IsExploreSessionActive ||
                resource == null || amount <= 0)
            {
                return false;
            }

            InventoryData spendableInventory = SpendableResourceInventory;
            InventoryData snapshot = _persistence.CreateSnapshot(spendableInventory, false);
            bool wasDirty = _persistence.HasUnsavedChanges;
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
            if (Inventory == null || IsExploreSessionActive)
            {
                return false;
            }

            InventoryData spendableInventory = SpendableResourceInventory;
            InventoryData snapshot = _persistence.CreateSnapshot(spendableInventory, false);
            bool wasDirty = _persistence.HasUnsavedChanges;
            if (!TrySpendResourcesForTransaction(costs))
            {
                return false;
            }

            return CompleteHubSpend(spendableInventory, snapshot, wasDirty);
        }

        internal bool TrySpendResourcesForTransaction(IReadOnlyList<UpgradeResourceCost> costs)
        {
            return Inventory != null && !IsExploreSessionActive &&
                _resources.TrySpend(SpendableResourceInventory, costs);
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
            if (!IsInitialized || Inventory == null)
            {
                Debug.LogError("Cannot start exploration before inventory data is initialized.", this);
                return false;
            }
            if (IsExploreSessionActive)
            {
                Debug.LogWarning("An exploration session is already active.", this);
                return false;
            }
            if (!_explorationSession.Begin(Inventory, ResourceChest, IsResourceChestUnlocked,
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

        private bool TrySpendResourceInternal(string resourceId, int amount)
        {
            return _resources.TrySpend(SpendableResourceInventory, resourceId, amount);
        }

        /// <summary>
        /// Hub(우주선)에서 저장 실패에 대비해 롤백 스냅샷 생성
        /// </summary>
        private InventoryData CreateHubRollbackSnapshot(InventoryData inventory)
        {
            return _persistence.CreateSnapshot(inventory, IsExploreSessionActive);
        }

        private bool CompleteInventoryMutation(
            InventoryData mutatedInventory, InventoryData hubSnapshot, bool wasDirty)
        {
            if (!_persistence.CompleteMutation(mutatedInventory, hubSnapshot, wasDirty, IsExploreSessionActive))
            {
                return false;
            }
            Changed?.Invoke();
            return true;
        }

        private bool CompleteHubSpend(
            InventoryData spentInventory, InventoryData snapshot, bool wasDirty)
        {
            if (IsExploreSessionActive ||
                !_persistence.CompleteMutation(spentInventory, snapshot, wasDirty, false))
            {
                return false;
            }
            Changed?.Invoke();
            if (ReferenceEquals(spentInventory, ResourceChest))
            {
                NotifyChestResourcesChanged();
            }
            return true;
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
            _persistence.Load(data);
            IsInitialized = _persistence.IsInitialized;
            Changed?.Invoke();
        }

        private void OnRuntimeDataChanged(GameRuntimeData runtimeData)
        {
            if (!IsInitialized)
            {
                return;
            }

            int capacity = runtimeData?.Inventory?.CreatureSlotCapacity ?? 1;
            _persistence.ResizeCreatureSlots(capacity);
            Changed?.Invoke();
        }

        private void SyncCreatureSaveData()
        {
            _persistence.SyncCreatureSaveData();
        }
    }
}
