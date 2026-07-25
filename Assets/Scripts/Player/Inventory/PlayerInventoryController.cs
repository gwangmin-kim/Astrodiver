using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerInventoryController : MonoBehaviour
{
    public static PlayerInventoryController Instance { get; private set; }

    private readonly Dictionary<string, int> _resourceCostBuffer =
        new(StringComparer.Ordinal);

    private InventoryData _inventory;
    private InventoryData _sessionStartSnapshot;
    private bool _sessionStartWasDirty;
    private bool _isExploreSessionActive;

    public event Action Changed; // 자원 변경이 일어났을 때 호출

    public IReadOnlyList<CreatureInventorySlot> CreatureSlots =>
        _inventory?.CreatureSlots;

    public IReadOnlyList<ResourceInventoryEntry> ResourceAmounts =>
        _inventory?.ResourceAmounts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 인벤토리 초기화
        GameDataManager manager = GameDataManager.Instance;
        if (manager == null || manager.Data == null)
        {
            Debug.LogError("Cannot initialize inventory because GameDataManager is not ready.", this);
            enabled = false;
            return;
        }
        _inventory = manager.Data.inventory;
        manager.DataChanged += OnGameDataChanged;
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        GameDataManager manager = GameDataManager.Instance;
        if (manager != null)
        {
            manager.DataChanged -= OnGameDataChanged;
            if (_isExploreSessionActive)
            {
                manager.EndSaveSuspension();
            }
        }

        Instance = null;
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

        CreatureInventorySlot[] slots = _inventory.MutableCreatureSlots;
        int targetIndex = FindCreatureSlot(slots, creature);
        if (targetIndex < 0)
        {
            Debug.Log("Inventory: Cannot Find Available Slot.", this);
            return false;
        }

        InventoryData snapshot = CreateHubRollbackSnapshot();
        bool wasDirty = GameDataManager.Instance.HasUnsavedChanges;
        CreatureInventorySlot slot = slots[targetIndex];
        if (slot == null)
        {
            slot = new CreatureInventorySlot();
            slots[targetIndex] = slot;
        }

        int nextCount = slot.IsEmpty ? 1 : slot.Count + 1;
        slot.Set(creature.Id, nextCount);

        return CompleteInventoryMutation(snapshot, wasDirty);
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

        string creatureId = creature.Id.Trim();
        int total = 0;
        IReadOnlyList<CreatureInventorySlot> slots = _inventory.CreatureSlots;
        for (int i = 0; i < slots.Count; i++)
        {
            CreatureInventorySlot slot = slots[i];
            if (slot == null || !slot.Matches(creatureId))
            {
                continue;
            }

            total = total > int.MaxValue - slot.Count
                ? int.MaxValue
                : total + slot.Count;
        }

        return total;
    }

    public int TakeCreature(CreatureDefinition creature, int requestedAmount)
    {
        if (_inventory == null || _isExploreSessionActive ||
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

        return CompleteHubSpend(snapshot, wasDirty)
            ? takenAmount
            : 0;
    }

    public bool TryAddResource(ResourceDefinition resource, int amount = 1)
    {
        if (_inventory == null || resource == null ||
            string.IsNullOrWhiteSpace(resource.Id) || amount <= 0)
        {
            return false;
        }

        InventoryData snapshot = CreateHubRollbackSnapshot();
        bool wasDirty = GameDataManager.Instance.HasUnsavedChanges;
        _inventory.AddResource(resource.Id, amount);
        return CompleteInventoryMutation(snapshot, wasDirty);
    }

    public int GetResourceAmount(ResourceDefinition resource)
    {
        return resource == null || _inventory == null
            ? 0
            : _inventory.GetResourceAmount(resource.Id);
    }

    /// <summary>
    /// 업그레이드 지불 가능 여부 검사
    /// </summary>
    public bool CanAfford(IReadOnlyList<UpgradeResourceCost> costs)
    {
        if (!TryAggregateCosts(costs))
        {
            return false;
        }

        foreach (KeyValuePair<string, int> cost in _resourceCostBuffer)
        {
            if (_inventory.GetResourceAmount(cost.Key) < cost.Value)
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
        if (_inventory == null || _isExploreSessionActive ||
            resource == null || amount <= 0)
        {
            return false;
        }

        InventoryData snapshot = _inventory.Clone();
        bool wasDirty = GameDataManager.Instance.HasUnsavedChanges;
        if (!TrySpendResourceInternal(resource.Id, amount))
        {
            return false;
        }

        return CompleteHubSpend(snapshot, wasDirty);
    }

    /// <summary>
    /// 자원 소비 (업그레이드 비용 전달 형태)
    /// 세션 중에 호출 불가
    /// </summary>
    public bool TrySpendResources(IReadOnlyList<UpgradeResourceCost> costs)
    {
        if (_inventory == null || _isExploreSessionActive)
        {
            return false;
        }

        InventoryData snapshot = _inventory.Clone();
        bool wasDirty = GameDataManager.Instance.HasUnsavedChanges;
        if (!TrySpendResourcesForTransaction(costs))
        {
            return false;
        }

        return CompleteHubSpend(snapshot, wasDirty);
    }

    internal bool TrySpendResourcesForTransaction(IReadOnlyList<UpgradeResourceCost> costs)
    {
        if (_inventory == null || _isExploreSessionActive || !CanAfford(costs))
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
    }

    public bool BeginExploreSession()
    {
        if (_isExploreSessionActive)
        {
            Debug.LogWarning("An exploration session is already active.", this);
            return false;
        }

        GameDataManager manager = GameDataManager.Instance;
        if (manager.HasUnsavedChanges && !manager.SaveNow())
        {
            Debug.LogError("Cannot start exploration with unsaved hub data.", this);
            return false;
        }

        if (!manager.BeginSaveSuspension())
        {
            Debug.LogError("Cannot suspend saving for the exploration session.", this);
            return false;
        }

        _sessionStartSnapshot = _inventory.Clone();
        _sessionStartWasDirty = manager.HasUnsavedChanges;
        _isExploreSessionActive = true;
        return true;
    }

    public bool CompleteExploreSession(float lossRatio)
    {
        if (!_isExploreSessionActive)
        {
            Debug.LogWarning("Cannot complete exploration because no session is active.", this);
            return false;
        }

        GameDataManager manager = GameDataManager.Instance;
        InventoryData runtimeBeforePenalty = _inventory.Clone();
        bool wasDirty = manager.HasUnsavedChanges;
        ApplySessionLoss(lossRatio);
        manager.MarkDirty();

        if (!manager.SaveExplorationResult())
        {
            _inventory.CopyFrom(runtimeBeforePenalty);
            manager.RestoreDirtyState(wasDirty);
            return false;
        }

        ClearSessionState();
        Changed?.Invoke();
        return true;
    }

    public void CancelExploreSession()
    {
        if (!_isExploreSessionActive)
        {
            return;
        }

        _inventory.CopyFrom(_sessionStartSnapshot);
        GameDataManager.Instance.RestoreDirtyState(_sessionStartWasDirty);
        ClearSessionState();
        Changed?.Invoke();
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

    private static int FindCreatureSlot(
        IReadOnlyList<CreatureInventorySlot> slots,
        CreatureDefinition creature)
    {
        // 일치하면서 자리가 남는 슬롯이 있는지 검사
        for (int i = 0; i < slots.Count; i++)
        {
            CreatureInventorySlot slot = slots[i];
            if (slot != null && slot.Matches(creature.Id) &&
                slot.Count < creature.MaxStackCount)
            {
                return i;
            }
        }

        // 빈 슬롯이 있는지 검사
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    private int TakeCreatureInternal(string creatureId, int requestedAmount)
    {
        CreatureInventorySlot[] slots = _inventory.MutableCreatureSlots;
        int remainingAmount = requestedAmount;

        for (int i = slots.Length - 1; i >= 0 && remainingAmount > 0; i--)
        {
            CreatureInventorySlot slot = slots[i];
            if (slot == null || !slot.Matches(creatureId))
            {
                continue;
            }

            int takenFromSlot = Mathf.Min(slot.Count, remainingAmount);
            slot.Set(creatureId, slot.Count - takenFromSlot);
            remainingAmount -= takenFromSlot;
        }

        return requestedAmount - remainingAmount;
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
        return _inventory.TrySpendResource(resourceId, amount);
    }

    /// <summary>
    /// Hub(우주선)에서 저장 실패에 대비해 롤백 스냅샷 생성
    /// </summary>
    private InventoryData CreateHubRollbackSnapshot()
    {
        return _isExploreSessionActive ? null : _inventory.Clone();
    }

    private bool CompleteInventoryMutation(InventoryData hubSnapshot, bool wasDirty)
    {
        GameDataManager manager = GameDataManager.Instance;
        manager.MarkDirty();

        if (!_isExploreSessionActive && !manager.SaveNow())
        {
            _inventory.CopyFrom(hubSnapshot);
            manager.RestoreDirtyState(wasDirty);
            return false;
        }

        Changed?.Invoke();
        return true;
    }

    private bool CompleteHubSpend(InventoryData snapshot, bool wasDirty)
    {
        if (_isExploreSessionActive)
        {
            return false;
        }

        GameDataManager manager = GameDataManager.Instance;
        manager.MarkDirty();
        if (manager.SaveNow())
        {
            Changed?.Invoke();
            return true;
        }

        _inventory.CopyFrom(snapshot);
        manager.RestoreDirtyState(wasDirty);
        return false;
    }

    private void ApplySessionLoss(float lossRatio)
    {
        float clampedLossRatio = Mathf.Clamp01(lossRatio);
        if (clampedLossRatio <= 0f)
        {
            return;
        }

        CreatureInventorySlot[] slots = _inventory.MutableCreatureSlots;
        for (int i = 0; i < slots.Length; i++)
        {
            CreatureInventorySlot slot = slots[i];
            if (slot == null || slot.IsEmpty)
            {
                continue;
            }

            int lostAmount = Mathf.CeilToInt(slot.Count * clampedLossRatio);
            slot.Set(slot.DefinitionId, slot.Count - lostAmount);
        }

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

    private void ClearSessionState()
    {
        _isExploreSessionActive = false;
        _sessionStartSnapshot = null;
        _sessionStartWasDirty = false;
        GameDataManager.Instance.EndSaveSuspension();
    }

    private void OnGameDataChanged(GameSaveData data)
    {
        _inventory = data?.inventory;
        Changed?.Invoke();
    }
}
