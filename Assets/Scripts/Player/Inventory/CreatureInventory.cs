using UnityEngine;

[System.Serializable]
public struct CreatureInventorySlot
{
    [SerializeField] private CreatureDefinition _definition;
    [SerializeField] private int _count;

    public readonly CreatureDefinition Definition => _definition;
    public readonly int Count => _count;
    public readonly bool IsEmpty => _definition == null || _count <= 0;

    /// <summary>
    /// 빈 슬롯에 처음 생물 저장
    /// </summary>
    /// <param name="definition">추가할 생물 종류</param>
    /// <param name="count">추가할 생물 수량</param>.
    public void Set(CreatureDefinition definition, int count)
    {
        _definition = definition;
        _count = Mathf.Clamp(count, 0, definition == null ? 0 : definition.MaxStackCount);
    }

    /// <summary>
    /// 이미 존재하는 생물의 수량을 더함
    /// </summary>
    /// <param name="amount">추가할 생물 수량</param>
    /// <returns>실제로 슬롯에 추가된 양</returns>
    public int Add(int amount)
    {
        if (_definition == null || amount <= 0) return 0;

        int addedAmount = Mathf.Min(amount, _definition.MaxStackCount - _count);
        _count += addedAmount;
        return addedAmount;
    }
}

[System.Serializable]
public struct CreatureResourceData
{
    public CreatureDefinition definition;
    [Min(1)] public int amount;
}
