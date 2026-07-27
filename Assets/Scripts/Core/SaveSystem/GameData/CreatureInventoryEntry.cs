using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class CreatureInventoryEntry
{
    [SerializeField]
    [FormerlySerializedAs("definitionId")]
    private string _definitionId;

    [SerializeField]
    [FormerlySerializedAs("count")]
    private int _count;

    public string DefinitionId => _definitionId;
    public int Count => _count;
    public bool IsEmpty => string.IsNullOrEmpty(_definitionId) || _count <= 0;

    public CreatureInventoryEntry()
    {
    }

    public CreatureInventoryEntry(string definitionId, int count)
    {
        _definitionId = definitionId;
        _count = count;
        Repair();
    }

    internal void Repair()
    {
        _definitionId = _definitionId?.Trim();
        _count = Mathf.Max(0, _count);
        if (string.IsNullOrEmpty(_definitionId) || _count == 0)
        {
            _definitionId = string.Empty;
            _count = 0;
        }
    }
}
