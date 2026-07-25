using System;
using UnityEngine;

[Serializable]
public sealed class CreatureInventorySlot
{
    [SerializeField] private string _definitionId;
    [SerializeField] private int _count;

    public string DefinitionId => _definitionId;
    public int Count => _count;
    public bool IsEmpty => string.IsNullOrEmpty(_definitionId) || _count <= 0;

    public CreatureInventorySlot()
    {
    }

    public CreatureInventorySlot(string definitionId, int count)
    {
        _definitionId = definitionId;
        _count = count;
        Repair();
    }

    internal bool Matches(string creatureId)
    {
        return !string.IsNullOrEmpty(creatureId) && string.Equals(
            _definitionId,
            creatureId,
            StringComparison.Ordinal);
    }

    internal void Set(string creatureId, int value)
    {
        _definitionId = creatureId?.Trim();
        _count = Mathf.Max(0, value);
        if (string.IsNullOrEmpty(_definitionId) || _count == 0)
        {
            _definitionId = string.Empty;
            _count = 0;
        }
    }

    internal void Clear()
    {
        _definitionId = string.Empty;
        _count = 0;
    }

    internal void Repair()
    {
        Set(_definitionId, _count);
    }
}
