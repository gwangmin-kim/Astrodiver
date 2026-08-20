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

    [Header("Tooltip")]
    [SerializeField] private UpgradeTooltipUI _tooltip;

    [Header("Connections")]
    [SerializeField, Min(1f)] private float _connectionWidth = 4f;

    private readonly Dictionary<UpgradeNodeDefinition, UpgradeNodeUI> _nodeDict = new();
    private readonly List<UpgradeConnectionUI> _connectionList = new();
    private UpgradeService _upgradeService;
    private PlayerInventoryController _playerInventory;
    private bool _purchaseInProgress;
    private UpgradeNodeUI _focusedNode;

    public event Action<UpgradeNodeUI, UpgradePurchaseResult> PurchaseAttempted;

    private void OnEnable()
    {
        Initialize();
    }

    private void OnDisable()
    {
        _focusedNode = null;
        if (_tooltip != null)
        {
            _tooltip.Hide();
        }

        Unsubscribe();
        UnsubscribeNodes();
    }

    public void Open()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }

    public void Close()
    {
        if (_purchaseInProgress || !gameObject.activeSelf)
        {
            return;
        }

        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        for (int i = 0; i < _connectionList.Count; i++)
        {
            _connectionList[i].RefreshGeometry();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying || _nodeLayer == null)
        {
            return;
        }

        UpgradeNodeUI[] nodes = _nodeLayer.GetComponentsInChildren<UpgradeNodeUI>(true);
        Dictionary<UpgradeNodeDefinition, UpgradeNodeUI> nodeLookup = new();
        for (int i = 0; i < nodes.Length; i++)
        {
            UpgradeNodeUI node = nodes[i];
            if (node.Definition != null)
            {
                nodeLookup.TryAdd(node.Definition, node);
            }
        }

        Gizmos.color = Color.cyan;
        foreach (KeyValuePair<UpgradeNodeDefinition, UpgradeNodeUI> pair in nodeLookup)
        {
            UpgradeNodeDefinition parent = pair.Key.Parent;
            if (parent != null && nodeLookup.TryGetValue(parent, out UpgradeNodeUI parentNode))
            {
                Gizmos.DrawLine(parentNode.transform.position, pair.Value.transform.position);
            }
        }
    }
#endif

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

        for (int i = 0; i < _connectionList.Count; i++)
        {
            _connectionList[i].RefreshColor();
        }

        RefreshTooltip();
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

        _playerInventory = PlayerInventoryController.Instance;
        if (_playerInventory != null)
        {
            _playerInventory.Changed += RefreshAll;
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
                pair.Value,
                _connectionLayer,
                _connectionWidth);
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

        UpgradePurchaseStatus purchaseStatus = _upgradeService != null
            ? _upgradeService.GetPurchaseStatus(definition)
            : UpgradePurchaseStatus.InventoryUnavailable;
        return purchaseStatus == UpgradePurchaseStatus.Success
            ? UpgradeNodeVisualState.Purchasable
            : UpgradeNodeVisualState.Unavailable;
    }

    private void SubscribeNodes()
    {
        foreach (UpgradeNodeUI node in _nodeDict.Values)
        {
            node.Clicked += HandleNodeClicked;
            node.Focused += HandleNodeFocused;
            node.Unfocused += HandleNodeUnfocused;
        }
    }

    private void UnsubscribeNodes()
    {
        foreach (UpgradeNodeUI node in _nodeDict.Values)
        {
            if (node != null)
            {
                node.Clicked -= HandleNodeClicked;
                node.Focused -= HandleNodeFocused;
                node.Unfocused -= HandleNodeUnfocused;
            }
        }
    }

    private void HandleNodeFocused(UpgradeNodeUI node)
    {
        if (node == null)
        {
            return;
        }

        _focusedNode = node;
        RefreshTooltip();
    }

    private void HandleNodeUnfocused(UpgradeNodeUI node)
    {
        if (_focusedNode != node)
        {
            return;
        }

        _focusedNode = FindFallbackFocusedNode();
        if (_focusedNode != null)
        {
            RefreshTooltip();
        }
        else if (_tooltip != null)
        {
            _tooltip.Hide();
        }
    }

    private UpgradeNodeUI FindFallbackFocusedNode()
    {
        UpgradeNodeUI selected = null;
        foreach (UpgradeNodeUI node in _nodeDict.Values)
        {
            if (node == null || !node.IsFocused)
            {
                continue;
            }

            if (node.IsPointerInside)
            {
                return node;
            }

            if (node.IsSelected)
            {
                selected = node;
            }
        }

        return selected;
    }

    private void RefreshTooltip()
    {
        if (_focusedNode == null || _tooltip == null)
        {
            return;
        }

        GameDataManager gameData = GameDataManager.Instance;
        int level = _upgradeService != null && _focusedNode.Definition != null
            ? _upgradeService.GetLevel(_focusedNode.Definition.Id)
            : 0;
        _tooltip.Show(
            _focusedNode,
            level,
            gameData != null ? gameData.RuntimeData : null);
    }

    private void Unsubscribe()
    {
        if (_playerInventory != null)
        {
            _playerInventory.Changed -= RefreshAll;
            _playerInventory = null;
        }

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
            node.NotifyPurchaseResult(result);

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
