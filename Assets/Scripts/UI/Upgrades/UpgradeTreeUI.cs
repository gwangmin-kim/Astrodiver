using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UpgradeTreeUI : MonoBehaviour
{
    [Header("Hierarchy")]
    [SerializeField] private RectTransform _treeContent;
    [SerializeField] private RectTransform _connectionLayer;
    [SerializeField] private RectTransform _nodeLayer;

    [Header("Prefabs")]
    [SerializeField] private UpgradeConnectionUI _connectionPrefab;

    [Header("Connections")]
    [SerializeField, Min(1f)] private float _connectionWidth = 4f;
    [SerializeField]
    private Color _connectionColor =
        new(0.42f, 0.68f, 0.92f, 0.8f);

    private readonly Dictionary<UpgradeNodeDefinition, UpgradeNodeUI> _nodeDict = new();
    private readonly List<UpgradeConnectionUI> _connectionList = new();
    private UpgradeService _upgradeService;
    private bool _purchaseInProgress;

    public event Action<UpgradeNodeUI, UpgradePurchaseResult> PurchaseAttempted;

    private void OnEnable()
    {
        Initialize();
    }

    private void OnDisable()
    {
        Unsubscribe();
        UnsubscribeNodes();
    }

    private void LateUpdate()
    {
        for (int i = 0; i < _connectionList.Count; i++)
        {
            _connectionList[i].RefreshGeometry();
        }
    }

    public void RefreshAll()
    {
        foreach (KeyValuePair<UpgradeNodeDefinition, UpgradeNodeUI> pair in _nodeDict)
        {
            UpgradeNodeDefinition definition = pair.Key;
            UpgradeNodeUI node = pair.Value;
            int level = _upgradeService != null
                ? _upgradeService.GetLevel(definition.Id)
                : 0;

            node.SetLevel(level);
            node.SetVisualState(GetVisualState(definition, level));
        }
    }

    private void Initialize()
    {
        Unsubscribe();
        UnsubscribeNodes();
        BuildNodeLookup();
        BuildConnections();

        GameDataManager gameData = GameDataManager.Instance;
        _upgradeService = gameData != null ? gameData.Upgrades : null;
        if (_upgradeService != null)
        {
            _upgradeService.UpgradePurchased += HandleUpgradePurchased;
        }
        else
        {
            Debug.LogError("UpgradeService is not available.", this);
        }

        SubscribeNodes();
        RefreshAll();
    }

    private void BuildNodeLookup()
    {
        _nodeDict.Clear();
        UpgradeNodeUI[] placedNodes = _nodeLayer.GetComponentsInChildren<UpgradeNodeUI>(true);
        for (int i = 0; i < placedNodes.Length; i++)
        {
            UpgradeNodeUI node = placedNodes[i];
            if (node.Definition == null)
            {
                Debug.LogWarning($"Upgrade node '{node.name}' has no definition.", node);
                continue;
            }

            if (!_nodeDict.TryAdd(node.Definition, node))
            {
                Debug.LogWarning(
                    $"Duplicate placed upgrade definition '{node.Definition.Id}'.",
                    node);
            }
        }
    }

    private void BuildConnections()
    {
        for (int i = _connectionLayer.childCount - 1; i >= 0; i--)
        {
            Destroy(_connectionLayer.GetChild(i).gameObject);
        }

        _connectionList.Clear();
        if (_connectionPrefab == null)
        {
            return;
        }

        foreach (KeyValuePair<UpgradeNodeDefinition, UpgradeNodeUI> pair in _nodeDict)
        {
            UpgradeNodeDefinition childDefinition = pair.Key;
            if (childDefinition.Parent == null ||
                !_nodeDict.TryGetValue(childDefinition.Parent, out UpgradeNodeUI parentNode))
            {
                continue;
            }

            UpgradeConnectionUI connection = Instantiate(
                _connectionPrefab,
                _connectionLayer);
            connection.name = $"{childDefinition.Parent.Id} -> {childDefinition.Id}";
            connection.SetNodes(
                (RectTransform)parentNode.transform,
                (RectTransform)pair.Value.transform,
                _connectionLayer,
                _connectionWidth,
                _connectionColor);
            _connectionList.Add(connection);
        }
    }

    private UpgradeNodeVisualState GetVisualState(
        UpgradeNodeDefinition definition,
        int level)
    {
        if (level >= definition.MaxLevel)
        {
            return UpgradeNodeVisualState.Completed;
        }

        if (definition.Parent != null)
        {
            int parentLevel = _upgradeService != null
                ? _upgradeService.GetLevel(definition.Parent.Id)
                : 0;
            if (parentLevel <= 0)
            {
                return UpgradeNodeVisualState.Locked;
            }
        }

        return level <= 0
            ? UpgradeNodeVisualState.Unlocked
            : UpgradeNodeVisualState.Purchased;
    }

    private void SubscribeNodes()
    {
        foreach (UpgradeNodeUI node in _nodeDict.Values)
        {
            node.Clicked += HandleNodeClicked;
        }
    }

    private void UnsubscribeNodes()
    {
        foreach (UpgradeNodeUI node in _nodeDict.Values)
        {
            if (node != null)
            {
                node.Clicked -= HandleNodeClicked;
            }
        }
    }

    private void Unsubscribe()
    {
        if (_upgradeService != null)
        {
            _upgradeService.UpgradePurchased -= HandleUpgradePurchased;
            _upgradeService = null;
        }
    }

    private void HandleNodeClicked(UpgradeNodeUI node)
    {
        if (_purchaseInProgress || node == null)
        {
            return;
        }

        if (_upgradeService == null)
        {
            Debug.LogError("Cannot purchase an upgrade because UpgradeService is unavailable.", this);
            return;
        }

        if (node.Definition == null)
        {
            Debug.LogError($"Upgrade node '{node.name}' has no definition.", node);
            return;
        }

        _purchaseInProgress = true;
        try
        {
            UpgradePurchaseResult result =
                _upgradeService.TryPurchase(node.Definition);
            PurchaseAttempted?.Invoke(node, result);

            if (!result.Succeeded)
            {
                LogPurchaseFailure(result, node);
            }
        }
        finally
        {
            _purchaseInProgress = false;
        }
    }

    private void HandleUpgradePurchased(UpgradeNodeDefinition definition, int level)
    {
        RefreshAll();
    }

    private static void LogPurchaseFailure(
        UpgradePurchaseResult result,
        UpgradeNodeUI node)
    {
        string message = !string.IsNullOrWhiteSpace(result.Message)
            ? result.Message
            : result.Status switch
            {
                UpgradePurchaseStatus.NodeNotFound =>
                    $"Upgrade '{result.NodeId}' is not registered.",
                UpgradePurchaseStatus.InvalidNode =>
                    $"Upgrade '{result.NodeId}' has an invalid definition.",
                UpgradePurchaseStatus.ParentLocked =>
                    $"Upgrade '{result.NodeId}' requires its parent upgrade.",
                UpgradePurchaseStatus.MaxLevelReached =>
                    $"Upgrade '{result.NodeId}' is already at maximum level.",
                UpgradePurchaseStatus.InventoryUnavailable =>
                    "Player inventory is unavailable.",
                UpgradePurchaseStatus.InsufficientResources =>
                    $"Not enough resources to purchase upgrade '{result.NodeId}'.",
                UpgradePurchaseStatus.EffectFailed =>
                    $"Failed to apply upgrade '{result.NodeId}'.",
                UpgradePurchaseStatus.SaveFailed =>
                    $"Failed to save upgrade '{result.NodeId}'.",
                _ => $"Upgrade purchase failed: {result.Status}."
            };

        if (result.Status is UpgradePurchaseStatus.ParentLocked or
            UpgradePurchaseStatus.MaxLevelReached or
            UpgradePurchaseStatus.InsufficientResources)
        {
            Debug.Log(message, node);
            return;
        }

        Debug.LogError(message, node);
    }
}
