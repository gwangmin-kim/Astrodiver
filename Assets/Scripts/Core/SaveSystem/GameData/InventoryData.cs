using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class InventoryData
{
    [SerializeField] private CreatureInventorySlot[] _creatureSlots = Array.Empty<CreatureInventorySlot>();
    [SerializeField] private List<ResourceInventoryEntry> _resourceAmounts = new();

    public IReadOnlyList<CreatureInventorySlot> CreatureSlots => _creatureSlots;
    public IReadOnlyList<ResourceInventoryEntry> ResourceAmounts => _resourceAmounts;

    internal CreatureInventorySlot[] MutableCreatureSlots => _creatureSlots;
    internal List<ResourceInventoryEntry> MutableResourceAmounts => _resourceAmounts;

    public InventoryData()
    {
    }

    public InventoryData(
        IReadOnlyList<CreatureInventorySlot> initialCreatureSlots,
        IReadOnlyList<ResourceInventoryEntry> initialResources)
    {
        _creatureSlots = new CreatureInventorySlot[initialCreatureSlots?.Count ?? 0];
        for (int i = 0; i < _creatureSlots.Length; i++)
        {
            CreatureInventorySlot source = initialCreatureSlots[i];
            _creatureSlots[i] = source == null
                ? new CreatureInventorySlot()
                : new CreatureInventorySlot(source.DefinitionId, source.Count);
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

    public void RepairAfterLoad(int? requiredCreatureSlotCount = null)
    {
        _creatureSlots ??= Array.Empty<CreatureInventorySlot>();
        _resourceAmounts ??= new List<ResourceInventoryEntry>();

        if (requiredCreatureSlotCount.HasValue)
        {
            int slotCount = Mathf.Max(0, requiredCreatureSlotCount.Value);
            if (_creatureSlots.Length != slotCount)
            {
                Array.Resize(ref _creatureSlots, slotCount);
            }
        }

        for (int i = 0; i < _creatureSlots.Length; i++)
        {
            _creatureSlots[i] ??= new CreatureInventorySlot();
            _creatureSlots[i].Repair();
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
        _creatureSlots ??= Array.Empty<CreatureInventorySlot>();
        _resourceAmounts ??= new List<ResourceInventoryEntry>();
        _resourceAmounts.Clear();

        if (source == null)
        {
            _creatureSlots = Array.Empty<CreatureInventorySlot>();
            return;
        }

        _creatureSlots = new CreatureInventorySlot[source._creatureSlots.Length];
        for (int i = 0; i < source._creatureSlots.Length; i++)
        {
            CreatureInventorySlot sourceSlot = source._creatureSlots[i];
            _creatureSlots[i] = sourceSlot == null
                ? new CreatureInventorySlot()
                : new CreatureInventorySlot(sourceSlot.DefinitionId, sourceSlot.Count);
        }

        for (int i = 0; i < source._resourceAmounts.Count; i++)
        {
            ResourceInventoryEntry sourceEntry = source._resourceAmounts[i];
            if (sourceEntry == null)
            {
                continue;
            }

            _resourceAmounts.Add(new ResourceInventoryEntry(
                sourceEntry.DefinitionId,
                sourceEntry.Amount));
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
        if (_creatureSlots == null || _resourceAmounts == null)
        {
            error = "Inventory collections are null.";
            return false;
        }

        for (int i = 0; i < _creatureSlots.Length; i++)
        {
            CreatureInventorySlot slot = _creatureSlots[i];
            if (slot == null)
            {
                error = $"Creature slot {i} is null.";
                return false;
            }

            bool hasId = !string.IsNullOrEmpty(slot.DefinitionId);
            bool hasCount = slot.Count > 0;
            if (hasId != hasCount ||
                hasId && (string.IsNullOrWhiteSpace(slot.DefinitionId) ||
                    !string.Equals(slot.DefinitionId, slot.DefinitionId.Trim(), StringComparison.Ordinal)))
            {
                error = $"Creature slot {i} is invalid.";
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
