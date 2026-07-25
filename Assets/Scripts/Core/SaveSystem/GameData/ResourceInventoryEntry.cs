using UnityEngine;

[System.Serializable]
public sealed class ResourceInventoryEntry
{
    [SerializeField] private string _definitionId;
    [SerializeField] private int _amount;

    public string DefinitionId => _definitionId;
    public int Amount => _amount;
    public bool IsEmpty => string.IsNullOrEmpty(_definitionId) || _amount <= 0;

    public ResourceInventoryEntry()
    {
    }

    public ResourceInventoryEntry(string definitionId, int amount)
    {
        _definitionId = definitionId;
        _amount = amount;
        Repair();
    }

    internal void SetAmount(int value)
    {
        _amount = Mathf.Max(0, value);
    }

    internal void Repair()
    {
        _definitionId = _definitionId?.Trim();
        _amount = Mathf.Max(0, _amount);
    }
}
