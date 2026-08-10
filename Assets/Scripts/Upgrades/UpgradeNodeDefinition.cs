using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "UpgradeNodeDefinition",
    menuName = "Astrodiver/Upgrades/Upgrade Node Definition")]
public sealed class UpgradeNodeDefinition : GameDefinition
{
    [Header("Identity")]
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private Sprite _icon;

    [Header("Tree")]
    [SerializeField] private UpgradeNodeDefinition _parent;
    [SerializeField, Range(1, 10)] private int _maxLevel = 1;

    [Header("Cost: base + current level * increase")]
    [SerializeField]
    private UpgradeResourceCost[] _baseCosts =
        Array.Empty<UpgradeResourceCost>();
    [SerializeField]
    private UpgradeResourceCost[] _costIncreases =
        Array.Empty<UpgradeResourceCost>();

    [Header("Effects applied once per purchased level")]
    [SerializeReference] private UpgradeEffect[] _effects = Array.Empty<UpgradeEffect>();

    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public UpgradeNodeDefinition Parent => _parent;
    public int MaxLevel => Mathf.Max(1, _maxLevel);
    public IReadOnlyList<UpgradeResourceCost> BaseCosts => _baseCosts;
    public IReadOnlyList<UpgradeResourceCost> CostIncreases => _costIncreases;
    public IReadOnlyList<UpgradeEffect> Effects => _effects;

    public void GetCostForNextLevel(
        int currentLevel,
        ICollection<UpgradeResourceCost> destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        int levelMultiplier = Mathf.Max(0, currentLevel);
        Dictionary<ResourceDefinition, int> totals = new();
        AddCosts(_baseCosts, 1, totals);
        AddCosts(_costIncreases, levelMultiplier, totals);

        foreach (KeyValuePair<ResourceDefinition, int> pair in totals)
        {
            if (pair.Key != null && pair.Value > 0)
            {
                destination.Add(new UpgradeResourceCost(pair.Key, pair.Value));
            }
        }
    }

    public bool TryValidate(out string error)
    {
        List<string> errors = new();
        if (string.IsNullOrWhiteSpace(Id))
        {
            errors.Add($"Upgrade node '{name}' has an empty id.");
        }

        if (_parent == this)
        {
            errors.Add($"Upgrade node '{name}' cannot be its own parent.");
        }

        ValidateCosts(_baseCosts, "base", errors);
        ValidateCosts(_costIncreases, "increase", errors);

        for (int i = 0; i < _effects.Length; i++)
        {
            if (_effects[i] == null)
            {
                errors.Add($"Effect {i} is null.");
                continue;
            }

            if (!_effects[i].TryValidate(out string effectError))
            {
                errors.Add($"Effect {i}: {effectError}");
            }
        }

        error = string.Join(Environment.NewLine, errors);
        return errors.Count == 0;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        string id,
        UpgradeNodeDefinition parent,
        int maxLevel,
        UpgradeResourceCost[] baseCosts,
        UpgradeResourceCost[] costIncreases,
        UpgradeEffect[] effects)
    {
        ConfigureIdentityForEditor(id);
        _parent = parent;
        _maxLevel = maxLevel;
        _baseCosts = baseCosts ?? Array.Empty<UpgradeResourceCost>();
        _costIncreases = costIncreases ?? Array.Empty<UpgradeResourceCost>();
        _effects = effects ?? Array.Empty<UpgradeEffect>();
    }
#endif

    private static void AddCosts(
        IReadOnlyList<UpgradeResourceCost> costs,
        int multiplier,
        IDictionary<ResourceDefinition, int> totals)
    {
        if (multiplier <= 0)
        {
            return;
        }

        for (int i = 0; i < costs.Count; i++)
        {
            UpgradeResourceCost cost = costs[i];
            if (cost.Resource == null || cost.Amount <= 0)
            {
                continue;
            }

            int scaled = cost.Amount > int.MaxValue / multiplier
                ? int.MaxValue
                : cost.Amount * multiplier;
            int current = totals.TryGetValue(cost.Resource, out int amount) ? amount : 0;
            totals[cost.Resource] = current > int.MaxValue - scaled
                ? int.MaxValue
                : current + scaled;
        }
    }

    private static void ValidateCosts(
        IReadOnlyList<UpgradeResourceCost> costs,
        string label,
        ICollection<string> errors)
    {
        for (int i = 0; i < costs.Count; i++)
        {
            if (costs[i].Resource == null)
            {
                errors.Add($"The {label} cost at index {i} has no resource.");
            }
            else if (costs[i].Amount < 0)
            {
                errors.Add($"The {label} cost at index {i} cannot be negative.");
            }
        }
    }
}

[Serializable]
public struct UpgradeResourceCost
{
    [SerializeField] private ResourceDefinition _resource;
    [SerializeField, Min(0)] private int _amount;

    public UpgradeResourceCost(ResourceDefinition resource, int amount)
    {
        _resource = resource;
        _amount = Mathf.Max(0, amount);
    }

    public ResourceDefinition Resource => _resource;
    public int Amount => Mathf.Max(0, _amount);
}
