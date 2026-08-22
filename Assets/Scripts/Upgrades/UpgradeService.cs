using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradePurchaseStatus
{
    Success,
    NodeNotFound,
    InvalidNode,
    ParentLocked,
    MaxLevelReached,
    InventoryUnavailable,
    InsufficientResources,
    EffectFailed,
    SaveFailed
}

public readonly struct UpgradePurchaseResult
{
    public UpgradePurchaseResult(
        UpgradePurchaseStatus status,
        string nodeId,
        int previousLevel,
        int currentLevel,
        string message = null)
    {
        Status = status;
        NodeId = nodeId;
        PreviousLevel = previousLevel;
        CurrentLevel = currentLevel;
        Message = message;
    }

    public UpgradePurchaseStatus Status { get; }
    public string NodeId { get; }
    public int PreviousLevel { get; }
    public int CurrentLevel { get; }
    public string Message { get; }
    public bool Succeeded => Status == UpgradePurchaseStatus.Success;
}

public sealed class UpgradeService
{
    private readonly GameDataManager _gameData;
    private readonly List<UpgradeResourceCost> _costBuffer = new();

    public UpgradeService(GameDataManager gameData)
    {
        if (gameData == null)
        {
            throw new ArgumentNullException(nameof(gameData));
        }

        _gameData = gameData;
    }

    public event Action<UpgradeNodeDefinition, int> UpgradePurchased;

    public int GetLevel(string nodeId)
    {
        return _gameData.GetUpgradeLevel(nodeId);
    }

    public bool IsUnlocked(string nodeId)
    {
        return GetLevel(nodeId) > 0;
    }

    public UpgradePurchaseStatus GetPurchaseStatus(string nodeId)
    {
        if (!_gameData.Definitions.TryGetUpgrade(nodeId, out UpgradeNodeDefinition node))
        {
            return UpgradePurchaseStatus.NodeNotFound;
        }

        return GetPurchaseStatus(node);
    }

    public UpgradePurchaseStatus GetPurchaseStatus(UpgradeNodeDefinition node)
    {
        if (node == null || !node.TryValidate(out _))
        {
            return UpgradePurchaseStatus.InvalidNode;
        }

        if (!_gameData.Definitions.TryGetUpgrade(node.Id, out UpgradeNodeDefinition registered) ||
            registered != node)
        {
            return UpgradePurchaseStatus.NodeNotFound;
        }

        int currentLevel = GetLevel(node.Id);
        if (currentLevel >= node.MaxLevel)
        {
            return UpgradePurchaseStatus.MaxLevelReached;
        }

        if (node.Parent != null && GetLevel(node.Parent.Id) <= 0)
        {
            return UpgradePurchaseStatus.ParentLocked;
        }

        _costBuffer.Clear();
        node.GetCostForNextLevel(currentLevel, _costBuffer);
        PlayerInventoryController inventory = PlayerInventoryController.Instance;
        return inventory != null && inventory.CanAfford(_costBuffer)
            ? UpgradePurchaseStatus.Success
            : inventory == null
                ? UpgradePurchaseStatus.InventoryUnavailable
                : UpgradePurchaseStatus.InsufficientResources;
    }

    public UpgradePurchaseResult TryPurchase(string nodeId)
    {
        if (!_gameData.Definitions.TryGetUpgrade(nodeId, out UpgradeNodeDefinition node))
        {
            return Result(UpgradePurchaseStatus.NodeNotFound, nodeId, 0);
        }

        return TryPurchase(node);
    }

    public UpgradePurchaseResult TryPurchase(UpgradeNodeDefinition node)
    {
        string validationError = null;
        if (node == null || !node.TryValidate(out validationError))
        {
            return Result(
                UpgradePurchaseStatus.InvalidNode,
                node != null ? node.Id : null,
                0,
                validationError);
        }

        if (!_gameData.Definitions.TryGetUpgrade(node.Id, out UpgradeNodeDefinition registered) ||
            registered != node)
        {
            return Result(UpgradePurchaseStatus.NodeNotFound, node.Id, 0);
        }

        PlayerInventoryController inventory = PlayerInventoryController.Instance;
        if (inventory == null)
        {
            return Result(UpgradePurchaseStatus.InventoryUnavailable, node.Id, 0);
        }

        int currentLevel = GetLevel(node.Id);
        if (currentLevel >= node.MaxLevel)
        {
            return Result(UpgradePurchaseStatus.MaxLevelReached, node.Id, currentLevel);
        }

        if (node.Parent != null && GetLevel(node.Parent.Id) <= 0)
        {
            return Result(UpgradePurchaseStatus.ParentLocked, node.Id, currentLevel);
        }

        _costBuffer.Clear();
        node.GetCostForNextLevel(currentLevel, _costBuffer);
        if (!inventory.CanAfford(_costBuffer))
        {
            return Result(UpgradePurchaseStatus.InsufficientResources, node.Id, currentLevel);
        }

        GameSaveData snapshot = _gameData.SaveData.Clone();
        bool wasDirty = _gameData.HasUnsavedChanges;

        if (!inventory.TrySpendResourcesForTransaction(_costBuffer))
        {
            return Result(UpgradePurchaseStatus.InsufficientResources, node.Id, currentLevel);
        }

        int nextLevel = currentLevel + 1;
        _gameData.SetUpgradeLevel(node.Id, nextLevel);
        if (!_gameData.RebuildRuntimeData(out string effectError))
        {
            _gameData.RestoreTransactionSnapshot(snapshot, wasDirty);
            return Result(
                UpgradePurchaseStatus.EffectFailed,
                node.Id,
                currentLevel,
                effectError);
        }

        _gameData.MarkDirty();

        if (!_gameData.SaveNow())
        {
            _gameData.RestoreTransactionSnapshot(snapshot, wasDirty);
            return Result(UpgradePurchaseStatus.SaveFailed, node.Id, currentLevel);
        }

        inventory.NotifyTransactionCommitted();
        CompleteTutorialEventForFirstUnlock(node.Id, nextLevel);
        UpgradePurchased?.Invoke(node, nextLevel);
        return new UpgradePurchaseResult(
            UpgradePurchaseStatus.Success,
            node.Id,
            currentLevel,
            nextLevel);
    }

    private void CompleteTutorialEventForFirstUnlock(string upgradeId, int level)
    {
        if (level != 1)
        {
            return;
        }

        GameProgressEventId eventId = upgradeId switch
        {
            "upgrade.root" => GameProgressEventId.UnlockBattery,
            "upgrade.net_unlock" => GameProgressEventId.UnlockNetgun,
            "upgrade.worktable_unlock" => GameProgressEventId.UnlockWorktable,
            _ => GameProgressEventId.None
        };

        if (eventId != GameProgressEventId.None)
        {
            _gameData.CompleteEventAndSave(eventId);
        }
    }

    private static UpgradePurchaseResult Result(
        UpgradePurchaseStatus status,
        string nodeId,
        int level,
        string message = null)
    {
        return new UpgradePurchaseResult(status, nodeId, level, level, message);
    }
}
