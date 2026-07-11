using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryController : MonoBehaviour
{
    [Header("Creature Inventory")]
    [SerializeField][Min(1)] private int _creatureSlotCount;
    [SerializeField] private CreatureInventorySlot[] _creatureSlots;

    [Header("Fragment Magnet")]
    public MagnetData magnetData;

    // Resource inventory
    private readonly Dictionary<ResourceDefinition, int> _resourceAmounts = new();

    public event Action<int, CreatureInventorySlot> CreatureSlotChanged; // (슬롯 번호, 슬롯 정보)
    public event Action<ResourceDefinition, int> ResourceAmountChanged; // (자원 종류, 자원 수)

    public IReadOnlyList<CreatureInventorySlot> CreatureSlots => _creatureSlots;

    private void Awake()
    {
        EnsureCreatureSlots();
    }

    /// <summary>
    /// 생물을 플레이어 인벤토리에 추가
    /// </summary>
    /// <param name="data">추가할 생물의 종류와 수량</param>
    /// <returns>수집 결과</returns>
    public CollectionResult CollectCreature(CreatureResourceData data)
    {
        EnsureCreatureSlots();

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
        return new CollectionResult(requestedAmount, requestedAmount - remainingAmount);
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
        return true;
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureCreatureSlots();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetData.radius);

        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, magnetData.collectRadius);
    }
#endif
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
