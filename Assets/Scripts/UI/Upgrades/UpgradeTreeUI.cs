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
    [SerializeField] private Color _connectionColor =
        new(0.42f, 0.68f, 0.92f, 0.8f);

    private readonly Dictionary<UpgradeNodeDefinition, UpgradeNodeUI> _nodes = new();
    private readonly List<UpgradeConnectionUI> _connections = new();
    private UpgradeService _upgradeService;

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
        for (int i = 0; i < _connections.Count; i++)
        {
            _connections[i].RefreshGeometry();
        }
    }

    public void RefreshAll()
    {
        foreach (KeyValuePair<UpgradeNodeDefinition, UpgradeNodeUI> pair in _nodes)
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

        SubscribeNodes();
        RefreshAll();
    }

    private void BuildNodeLookup()
    {
        _nodes.Clear();
        UpgradeNodeUI[] placedNodes = _nodeLayer.GetComponentsInChildren<UpgradeNodeUI>(true);
        for (int i = 0; i < placedNodes.Length; i++)
        {
            UpgradeNodeUI node = placedNodes[i];
            if (node.Definition == null)
            {
                Debug.LogWarning($"Upgrade node '{node.name}' has no definition.", node);
                continue;
            }

            if (!_nodes.TryAdd(node.Definition, node))
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

        _connections.Clear();
        if (_connectionPrefab == null)
        {
            return;
        }

        foreach (KeyValuePair<UpgradeNodeDefinition, UpgradeNodeUI> pair in _nodes)
        {
            UpgradeNodeDefinition childDefinition = pair.Key;
            if (childDefinition.Parent == null ||
                !_nodes.TryGetValue(childDefinition.Parent, out UpgradeNodeUI parentNode))
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
            _connections.Add(connection);
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

        if (definition.Parent == null)
        {
            return UpgradeNodeVisualState.Unlocked;
        }

        int parentLevel = _upgradeService != null
            ? _upgradeService.GetLevel(definition.Parent.Id)
            : 0;
        return parentLevel > 0
            ? UpgradeNodeVisualState.Unlocked
            : UpgradeNodeVisualState.Locked;
    }

    private void SubscribeNodes()
    {
        foreach (UpgradeNodeUI node in _nodes.Values)
        {
            node.Clicked += HandleNodeClicked;
        }
    }

    private void UnsubscribeNodes()
    {
        foreach (UpgradeNodeUI node in _nodes.Values)
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
        if (_upgradeService == null || node.Definition == null)
        {
            return;
        }

        UpgradePurchaseResult result = _upgradeService.TryPurchase(node.Definition);
        if (!result.Succeeded && !string.IsNullOrWhiteSpace(result.Message))
        {
            Debug.LogWarning(result.Message, node);
        }

        RefreshAll();
    }

    private void HandleUpgradePurchased(UpgradeNodeDefinition definition, int level)
    {
        RefreshAll();
    }
}
