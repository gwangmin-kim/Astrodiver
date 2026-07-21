using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    public static PlayerInventoryController Instance { get; private set; }

    [Header("Creature Inventory")]
    [SerializeField][Min(1)] private int _creatureSlotCount;
    [SerializeField] private CreatureInventorySlot[] _creatureSlots;

    // Resource inventory
    private readonly Dictionary<ResourceDefinition, int> _resourceAmounts = new();

    public event Action<int, CreatureInventorySlot> CreatureSlotChanged; // (슬롯 번호, 슬롯 정보)
    public event Action<ResourceDefinition, int> ResourceAmountChanged; // (자원 종류, 자원 수)

    public IReadOnlyList<CreatureInventorySlot> CreatureSlots => _creatureSlots;
    public IReadOnlyDictionary<ResourceDefinition, int> ResourceAmounts => _resourceAmounts;
    public bool HasUncommittedChanges { get; private set; }

    private void OnValidate()
    {
        EnsureCreatureSlots();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureCreatureSlots();
        RestoreFromGameData();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 생물을 플레이어 인벤토리에 추가
    /// </summary>
    /// <param name="data">추가할 생물의 종류와 수량</param>
    /// <returns>수집 결과</returns>
    public CollectionResult CollectCreature(CreatureResourceData data)
    {
        // EnsureCreatureSlots();

        int requestedAmount = Mathf.Max(0, data.amount);
        if (data.definition == null || requestedAmount == 0)
        {
            return new CollectionResult(requestedAmount, 0);
        }

        int remainingAmount = requestedAmount;

        // 먼저 같은 종류의 생물을 보유 중인지 확인, 보유 중일 경우 해당 슬롯에 채워넣기 시도
        for (int i = 0; i < _creatureSlots.Length && remainingAmount > 0; i++)
        {
            CreatureInventorySlot slot = _creatureSlots[i];
            if (slot.Definition != data.definition) continue;

            int addedAmount = slot.Add(remainingAmount);
            if (addedAmount == 0) continue;

            remainingAmount -= addedAmount;
            _creatureSlots[i] = slot;
            CreatureSlotChanged?.Invoke(i, slot);
            InventoryEvents.RaiseCreatureSlotChanged(i, slot);
        }

        // 남은 생물이 있다면 빈 슬롯에 채워넣기 시도
        for (int i = 0; i < _creatureSlots.Length && remainingAmount > 0; i++)
        {
            CreatureInventorySlot slot = _creatureSlots[i];
            if (!slot.IsEmpty) continue;

            slot.Set(data.definition, 0);
            int addedAmount = slot.Add(remainingAmount);
            remainingAmount -= addedAmount;
            _creatureSlots[i] = slot;
            CreatureSlotChanged?.Invoke(i, slot);
            InventoryEvents.RaiseCreatureSlotChanged(i, slot);
        }

        // 결과 반환
        CollectionResult result = new(requestedAmount, requestedAmount - remainingAmount);
        if (result.CollectedAmount > 0)
        {
            HasUncommittedChanges = true;
        }

        return result;
    }

    /// <summary>
    /// 자원을 플레이어 인벤토리에 추가
    /// </summary>
    /// <param name="data">자원의 종류와 수량</param>
    /// <returns>전체 보유량</returns>
    public int CollectResourceFragment(FragmentResourceData data)
    {
        if (data.definition == null || data.amount <= 0)
        {
            return data.definition == null ? 0 : GetResourceAmount(data.definition);
        }

        int currentAmount = GetResourceAmount(data.definition);
        int nextAmount = currentAmount > int.MaxValue - data.amount
            ? int.MaxValue
            : currentAmount + data.amount;

        _resourceAmounts[data.definition] = nextAmount;
        ResourceAmountChanged?.Invoke(data.definition, nextAmount);
        InventoryEvents.RaiseResourceAmountChanged(data.definition, nextAmount);
        HasUncommittedChanges = true;
        return nextAmount;
    }

    public int GetResourceAmount(ResourceDefinition definition)
    {
        if (definition == null) return 0;

        return _resourceAmounts.TryGetValue(definition, out int amount)
            ? amount
            : 0;
    }

    public bool TrySpendResource(ResourceDefinition definition, int amount)
    {
        if (definition == null || amount <= 0) return false;

        int currentAmount = GetResourceAmount(definition);
        if (currentAmount < amount) return false;

        int nextAmount = currentAmount - amount;
        _resourceAmounts[definition] = nextAmount;
        ResourceAmountChanged?.Invoke(definition, nextAmount);
        InventoryEvents.RaiseResourceAmountChanged(definition, nextAmount);
        HasUncommittedChanges = true;
        return true;
    }

    public void LoseSessionInventory(float lossRatio)
    {
        float clampedLossRatio = Mathf.Clamp01(lossRatio);
        if (clampedLossRatio <= 0f)
        {
            return;
        }

        // EnsureCreatureSlots();
        for (int i = 0; i < _creatureSlots.Length; i++)
        {
            CreatureInventorySlot slot = _creatureSlots[i];
            if (slot.IsEmpty)
            {
                continue;
            }

            int lostAmount = Mathf.CeilToInt(slot.Count * clampedLossRatio);
            int remainingAmount = Mathf.Max(0, slot.Count - lostAmount);
            slot.Set(remainingAmount > 0 ? slot.Definition : null, remainingAmount);
            _creatureSlots[i] = slot;
            CreatureSlotChanged?.Invoke(i, slot);
            InventoryEvents.RaiseCreatureSlotChanged(i, slot);
        }

        ResourceDefinition[] definitions = new ResourceDefinition[_resourceAmounts.Count];
        _resourceAmounts.Keys.CopyTo(definitions, 0);

        foreach (ResourceDefinition definition in definitions)
        {
            int currentAmount = _resourceAmounts[definition];
            int lostAmount = Mathf.CeilToInt(currentAmount * clampedLossRatio);
            int remainingAmount = Mathf.Max(0, currentAmount - lostAmount);
            _resourceAmounts[definition] = remainingAmount;
            ResourceAmountChanged?.Invoke(definition, remainingAmount);
            InventoryEvents.RaiseResourceAmountChanged(definition, remainingAmount);
        }

        HasUncommittedChanges = true;
    }

    private void RestoreFromGameData()
    {
        InventorySaveData fallback = CreateSaveData();
        InventorySaveData savedData =
            GameDataManager.Instance.GetOrInitializeInventory(fallback);

        GameDefinitionRegistry definitions = GameDataManager.Instance.Definitions;
        if (definitions == null)
        {
            Debug.LogError("Game definition registry is not initialized.", this);
            return;
        }

        savedData.Normalize();

        int savedSlotCount = savedData.creatureSlots.Count;
        _creatureSlotCount = Mathf.Max(
            1,
            savedSlotCount > 0 ? savedSlotCount : _creatureSlotCount);
        _creatureSlots = new CreatureInventorySlot[_creatureSlotCount];

        int restoredSlotCount = Mathf.Min(savedSlotCount, _creatureSlots.Length);
        for (int i = 0; i < restoredSlotCount; i++)
        {
            CreatureSlotSaveData savedSlot = savedData.creatureSlots[i];
            if (string.IsNullOrEmpty(savedSlot.definitionId) || savedSlot.count <= 0)
            {
                continue;
            }

            if (!definitions.TryGetCreature(
                    savedSlot.definitionId,
                    out CreatureDefinition definition))
            {
                Debug.LogWarning(
                    $"Could not restore creature '{savedSlot.definitionId}' because its definition was not found.",
                    this);
                continue;
            }

            CreatureInventorySlot slot = new();
            slot.Set(definition, savedSlot.count);
            _creatureSlots[i] = slot;
        }

        _resourceAmounts.Clear();
        for (int i = 0; i < savedData.resourceAmounts.Count; i++)
        {
            ResourceAmountSaveData savedAmount = savedData.resourceAmounts[i];
            if (!definitions.TryGetResource(
                    savedAmount.definitionId,
                    out ResourceDefinition definition))
            {
                Debug.LogWarning(
                    $"Could not restore resource '{savedAmount.definitionId}' because its definition was not found.",
                    this);
                continue;
            }

            _resourceAmounts[definition] = Mathf.Max(0, savedAmount.amount);
        }

        HasUncommittedChanges = false;
    }

    /// <summary>
    /// 런타임 인벤토리 데이터를 통합 데이터로 이관, 영구 저장소에도 반영
    /// 세션 종료 시 호출
    /// </summary>
    public bool CommitToGameDataAndSave()
    {
        GameDataManager manager = GameDataManager.Instance;
        if (manager == null)
        {
            Debug.LogError("Cannot commit inventory because GameDataManager was not found.", this);
            return false;
        }

        manager.SetInventory(CreateSaveData());
        bool saveSucceeded = manager.SaveNow();
        if (saveSucceeded)
        {
            HasUncommittedChanges = false;
        }

        return saveSucceeded;
    }

    private InventorySaveData CreateSaveData()
    {
        // EnsureCreatureSlots();

        InventorySaveData saveData = new()
        {
            initialized = true
        };

        // 생물 자원 처리
        for (int i = 0; i < _creatureSlots.Length; i++)
        {
            CreatureInventorySlot slot = _creatureSlots[i];
            saveData.creatureSlots.Add(new CreatureSlotSaveData
            {
                // 빈 슬롯의 위치도 유지하기 위한 장치
                definitionId = slot.IsEmpty ? string.Empty : slot.Definition.Id,
                count = slot.IsEmpty ? 0 : slot.Count
            });
        }

        // 파편 자원 처리
        foreach (KeyValuePair<ResourceDefinition, int> pair in _resourceAmounts)
        {
            if (pair.Key == null || pair.Value <= 0)
            {
                continue;
            }

            saveData.resourceAmounts.Add(new ResourceAmountSaveData
            {
                definitionId = pair.Key.Id,
                amount = pair.Value
            });
        }

        saveData.Normalize();
        return saveData;
    }

    private void EnsureCreatureSlots()
    {
        int slotCount = Mathf.Max(1, _creatureSlotCount);
        if (_creatureSlots != null && _creatureSlots.Length == slotCount) return;

        CreatureInventorySlot[] resizedSlots = new CreatureInventorySlot[slotCount];
        if (_creatureSlots != null)
        {
            Array.Copy(_creatureSlots, resizedSlots, Mathf.Min(_creatureSlots.Length, resizedSlots.Length));
        }

        _creatureSlots = resizedSlots;
    }
}

[System.Serializable]
public struct FragmentResourceData
{
    public ResourceDefinition definition;
    [Min(1)] public int amount;
}

/// <summary>
/// 수집 결과를 표현하는 구조체
/// 생물의 경우 요청된 수와 실제 처리된 수가 달라질 수 있음 (슬롯이 제한되어있기 때문)
/// 요청된 수와 실제 처리된 수의 쌍을 가지고 있음
/// </summary>
[System.Serializable]
public readonly struct CollectionResult
{
    public int RequestedAmount { get; }
    public int CollectedAmount { get; }
    public int RemainingAmount => RequestedAmount - CollectedAmount;
    public bool IsFullyCollected => RemainingAmount == 0;

    public CollectionResult(int requestedAmount, int collectedAmount)
    {
        RequestedAmount = requestedAmount;
        CollectedAmount = collectedAmount;
    }
}
