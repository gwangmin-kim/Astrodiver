using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class InventoryData
{
    [SerializeField] private List<CreatureInventoryEntry> _creatures = new();
    [SerializeField] private List<ResourceInventoryEntry> _resourceAmounts = new();

    public IReadOnlyList<CreatureInventoryEntry> Creatures => _creatures;
    public IReadOnlyList<ResourceInventoryEntry> ResourceAmounts => _resourceAmounts;

    internal List<ResourceInventoryEntry> MutableResourceAmounts => _resourceAmounts;

    public InventoryData()
    {
    }

    public InventoryData(
        IReadOnlyList<CreatureInventoryEntry> initialCreatures,
        IReadOnlyList<ResourceInventoryEntry> initialResources)
    {
        _creatures = new List<CreatureInventoryEntry>(initialCreatures?.Count ?? 0);
        if (initialCreatures != null)
        {
            for (int i = 0; i < initialCreatures.Count; i++)
            {
                CreatureInventoryEntry source = initialCreatures[i];
                if (source != null && !source.IsEmpty)
                {
                    _creatures.Add(new CreatureInventoryEntry(source.DefinitionId, source.Count));
                }
            }
        }

        _resourceAmounts = new List<ResourceInventoryEntry>(initialResources?.Count ?? 0);
        if (initialResources != null)
        {
            for (int i = 0; i < initialResources.Count; i++)
            {
                ResourceInventoryEntry source = initialResources[i];
                if (source != null)
                {
                    _resourceAmounts.Add(new ResourceInventoryEntry(
                        source.DefinitionId,
                        source.Amount));
                }
            }
        }

        RepairAfterLoad();
    }

    public void RepairAfterLoad()
    {
        _creatures ??= new List<CreatureInventoryEntry>();
        _resourceAmounts ??= new List<ResourceInventoryEntry>();

        for (int i = _creatures.Count - 1; i >= 0; i--)
        {
            CreatureInventoryEntry entry = _creatures[i];
            if (entry == null)
            {
                _creatures.RemoveAt(i);
                continue;
            }

            entry.Repair();
            if (entry.IsEmpty)
            {
                _creatures.RemoveAt(i);
            }
        }

        for (int i = _resourceAmounts.Count - 1; i >= 0; i--)
        {
            ResourceInventoryEntry entry = _resourceAmounts[i];
            if (entry == null)
            {
                _resourceAmounts.RemoveAt(i);
                continue;
            }

            entry.Repair();
            if (entry.IsEmpty)
            {
                _resourceAmounts.RemoveAt(i);
                continue;
            }

            for (int previousIndex = 0; previousIndex < i; previousIndex++)
            {
                ResourceInventoryEntry previous = _resourceAmounts[previousIndex];
                if (previous == null || !string.Equals(
                        previous.DefinitionId,
                        entry.DefinitionId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                previous.SetAmount(SaturatingAdd(previous.Amount, entry.Amount));
                _resourceAmounts.RemoveAt(i);
                break;
            }
        }

        _resourceAmounts.Sort((left, right) =>
            string.CompareOrdinal(left.DefinitionId, right.DefinitionId));
    }

    public InventoryData Clone()
    {
        InventoryData clone = new();
        clone.CopyFrom(this);
        return clone;
    }

    public void CopyFrom(InventoryData source)
    {
        _creatures ??= new List<CreatureInventoryEntry>();
        _resourceAmounts ??= new List<ResourceInventoryEntry>();
        _creatures.Clear();
        _resourceAmounts.Clear();

        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source._creatures.Count; i++)
        {
            CreatureInventoryEntry sourceEntry = source._creatures[i];
            if (sourceEntry != null && !sourceEntry.IsEmpty)
            {
                _creatures.Add(new CreatureInventoryEntry(
                    sourceEntry.DefinitionId,
                    sourceEntry.Count));
            }
        }

        for (int i = 0; i < source._resourceAmounts.Count; i++)
        {
            ResourceInventoryEntry sourceEntry = source._resourceAmounts[i];
            if (sourceEntry != null)
            {
                _resourceAmounts.Add(new ResourceInventoryEntry(
                    sourceEntry.DefinitionId,
                    sourceEntry.Amount));
            }
        }
    }

    internal void SetCreatures(IReadOnlyList<CreatureInventorySlot> slots)
    {
        _creatures ??= new List<CreatureInventoryEntry>();
        _creatures.Clear();
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            CreatureInventorySlot slot = slots[i];
            if (slot != null && !slot.IsEmpty)
            {
                _creatures.Add(new CreatureInventoryEntry(slot.DefinitionId, slot.Count));
            }
        }
    }

    public int GetResourceAmount(string definitionId)
    {
        string id = definitionId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return 0;
        }

        ResourceInventoryEntry entry = FindResource(id);
        return entry?.Amount ?? 0;
    }

    internal int AddResource(string definitionId, int amount)
    {
        string id = definitionId?.Trim();
        if (string.IsNullOrEmpty(id) || amount <= 0)
        {
            return GetResourceAmount(id);
        }

        ResourceInventoryEntry entry = FindResource(id);
        if (entry == null)
        {
            entry = new ResourceInventoryEntry(id, 0);
            _resourceAmounts.Add(entry);
        }

        entry.SetAmount(SaturatingAdd(entry.Amount, amount));
        return entry.Amount;
    }

    internal bool TrySpendResource(string definitionId, int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        ResourceInventoryEntry entry = FindResource(definitionId?.Trim());
        if (entry == null || entry.Amount < amount)
        {
            return false;
        }

        entry.SetAmount(entry.Amount - amount);
        if (entry.Amount == 0)
        {
            _resourceAmounts.Remove(entry);
        }

        return true;
    }

    internal void SetResourceAmount(string definitionId, int amount)
    {
        string id = definitionId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        ResourceInventoryEntry entry = FindResource(id);
        int normalizedAmount = Mathf.Max(0, amount);
        if (normalizedAmount == 0)
        {
            if (entry != null)
            {
                _resourceAmounts.Remove(entry);
            }

            return;
        }

        if (entry == null)
        {
            _resourceAmounts.Add(new ResourceInventoryEntry(id, normalizedAmount));
        }
        else
        {
            entry.SetAmount(normalizedAmount);
        }
    }

    /// <summary>
    /// 인벤토리 데이터의 ResourceInventoryEntry를 이관하는 함수
    /// 플레이어 인벤토리의 자원을 상자 인벤토리로 이동시키기 위한 용도
    /// 다른 용도로 사용되어선 안됨
    /// </summary>
    internal bool TransferAllResourcesTo(InventoryData destination)
    {
        if (destination == null || ReferenceEquals(this, destination))
        {
            return false;
        }

        _resourceAmounts ??= new List<ResourceInventoryEntry>();
        destination._resourceAmounts ??= new List<ResourceInventoryEntry>();
        if (_resourceAmounts.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < _resourceAmounts.Count; i++)
        {
            ResourceInventoryEntry entry = _resourceAmounts[i];
            if (entry != null && !entry.IsEmpty)
            {
                destination.AddResource(entry.DefinitionId, entry.Amount);
            }
        }

        _resourceAmounts.Clear();
        return true;
    }

    private ResourceInventoryEntry FindResource(string definitionId)
    {
        if (string.IsNullOrEmpty(definitionId))
        {
            return null;
        }

        for (int i = 0; i < _resourceAmounts.Count; i++)
        {
            ResourceInventoryEntry entry = _resourceAmounts[i];
            if (entry != null && string.Equals(
                    entry.DefinitionId,
                    definitionId,
                    StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private static int SaturatingAdd(int left, int right)
    {
        return left > int.MaxValue - right ? int.MaxValue : left + right;
    }

    public bool TryValidate(out string error)
    {
        if (_creatures == null || _resourceAmounts == null)
        {
            error = "Inventory collections are null.";
            return false;
        }

        for (int i = 0; i < _creatures.Count; i++)
        {
            CreatureInventoryEntry entry = _creatures[i];
            if (entry == null || entry.IsEmpty ||
                string.IsNullOrWhiteSpace(entry.DefinitionId) ||
                !string.Equals(
                    entry.DefinitionId,
                    entry.DefinitionId.Trim(),
                    StringComparison.Ordinal))
            {
                error = $"Creature entry {i} is invalid.";
                return false;
            }
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int i = 0; i < _resourceAmounts.Count; i++)
        {
            ResourceInventoryEntry entry = _resourceAmounts[i];
            if (entry == null || entry.IsEmpty ||
                string.IsNullOrWhiteSpace(entry.DefinitionId) ||
                !string.Equals(
                    entry.DefinitionId,
                    entry.DefinitionId.Trim(),
                    StringComparison.Ordinal) ||
                !ids.Add(entry.DefinitionId))
            {
                error = $"Resource entry {i} is invalid or duplicated.";
                return false;
            }
        }

        error = null;
        return true;
    }
}
