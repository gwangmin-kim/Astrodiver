using System;
using UnityEngine;

[Serializable]
public sealed class WorktableSaveData
{
    [SerializeField] private InventoryData _inventory = new();
    [SerializeField] private string _processingCreatureId;
    [SerializeField, Min(0f)] private float _remainingBaseProcessSeconds;

    public InventoryData Inventory => _inventory;
    public string ProcessingCreatureId => _processingCreatureId;
    public float RemainingBaseProcessSeconds =>
        Mathf.Max(0f, _remainingBaseProcessSeconds);

    public void RepairAfterLoad()
    {
        _inventory ??= new InventoryData();
        _inventory.RepairAfterLoad();
        _processingCreatureId = _processingCreatureId?.Trim();
        _remainingBaseProcessSeconds = Mathf.Max(
            0f,
            _remainingBaseProcessSeconds);

        if (string.IsNullOrEmpty(_processingCreatureId) ||
            _remainingBaseProcessSeconds <= 0f)
        {
            ClearProcessing();
        }
    }

    public WorktableSaveData Clone()
    {
        WorktableSaveData clone = new();
        clone.CopyFrom(this);
        return clone;
    }

    public void CopyFrom(WorktableSaveData source)
    {
        _inventory ??= new InventoryData();
        if (source == null)
        {
            _inventory.CopyFrom(null);
            ClearProcessing();
            return;
        }

        _inventory.CopyFrom(source._inventory);
        _processingCreatureId = source._processingCreatureId;
        _remainingBaseProcessSeconds =
            source._remainingBaseProcessSeconds;
        RepairAfterLoad();
    }

    internal void SetProcessing(string creatureId, float remainingSeconds)
    {
        _processingCreatureId = creatureId?.Trim();
        _remainingBaseProcessSeconds = Mathf.Max(0f, remainingSeconds);
        if (string.IsNullOrEmpty(_processingCreatureId) ||
            _remainingBaseProcessSeconds <= 0f)
        {
            ClearProcessing();
        }
    }

    internal void ClearProcessing()
    {
        _processingCreatureId = string.Empty;
        _remainingBaseProcessSeconds = 0f;
    }

    public bool TryValidate(out string error)
    {
        error = _inventory == null ? "Inventory is null." : null;
        if (_inventory == null || !_inventory.TryValidate(out error))
        {
            error = $"Worktable inventory is invalid: {error}";
            return false;
        }

        if (_inventory.ResourceAmounts.Count > 0)
        {
            error = "Worktable inventory must contain creatures only.";
            return false;
        }

        if (_remainingBaseProcessSeconds < 0f ||
            float.IsNaN(_remainingBaseProcessSeconds) ||
            float.IsInfinity(_remainingBaseProcessSeconds))
        {
            error = "Worktable remaining process time is invalid.";
            return false;
        }

        if ((_remainingBaseProcessSeconds > 0f) !=
            !string.IsNullOrWhiteSpace(_processingCreatureId))
        {
            error = "Worktable processing creature and remaining time do not match.";
            return false;
        }

        error = null;
        return true;
    }
}
