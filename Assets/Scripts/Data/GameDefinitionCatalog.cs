using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameDefinitionCatalog",
    menuName = "Astrodiver/Data/Game Definition Catalog")]
public sealed class GameDefinitionCatalog : ScriptableObject
{
    [SerializeField] private ResourceDefinition[] _resources = Array.Empty<ResourceDefinition>();
    [SerializeField] private CreatureDefinition[] _creatures = Array.Empty<CreatureDefinition>();
    [SerializeField] private FloatageDefinition[] _floatages = Array.Empty<FloatageDefinition>();
    [SerializeField] private MiningTileDefinition[] _miningTiles = Array.Empty<MiningTileDefinition>();
    [SerializeField] private UpgradeNodeDefinition[] _upgrades = Array.Empty<UpgradeNodeDefinition>();
    [SerializeField] private TutorialGuideDefinition[] _tutorialGuides =
        Array.Empty<TutorialGuideDefinition>();

    public IReadOnlyList<ResourceDefinition> Resources => _resources;
    public IReadOnlyList<CreatureDefinition> Creatures => _creatures;
    public IReadOnlyList<FloatageDefinition> Floatages =>
        _floatages ?? Array.Empty<FloatageDefinition>();
    public IReadOnlyList<MiningTileDefinition> MiningTiles =>
        _miningTiles ?? Array.Empty<MiningTileDefinition>();
    public IReadOnlyList<UpgradeNodeDefinition> Upgrades =>
        _upgrades ?? Array.Empty<UpgradeNodeDefinition>();
    public IReadOnlyList<TutorialGuideDefinition> TutorialGuides =>
        _tutorialGuides ?? Array.Empty<TutorialGuideDefinition>();

    public bool TryValidate(out string error)
    {
        List<string> errors = new();
        ValidateDefinitions(_resources, definition => definition.Id, errors);
        ValidateResources(errors);
        ValidateDefinitions(_creatures, definition => definition.Id, errors);
        ValidateCreatures(errors);
        _floatages ??= Array.Empty<FloatageDefinition>();
        ValidateDefinitions(_floatages, definition => definition.Id, errors);
        ValidateFloatages(errors);
        _miningTiles ??= Array.Empty<MiningTileDefinition>();
        ValidateMiningTiles(errors);
        _upgrades ??= Array.Empty<UpgradeNodeDefinition>();
        ValidateDefinitions(_upgrades, definition => definition.Id, errors);
        ValidateUpgradeTree(errors);
        _tutorialGuides ??= Array.Empty<TutorialGuideDefinition>();
        ValidateDefinitions(_tutorialGuides, definition => definition.Id, errors);
        error = string.Join(Environment.NewLine, errors);
        return errors.Count == 0;
    }

#if UNITY_EDITOR
    public void SetDefinitionsForEditor(
        ResourceDefinition[] resources,
        CreatureDefinition[] creatures,
        FloatageDefinition[] floatages,
        UpgradeNodeDefinition[] upgrades,
        TutorialGuideDefinition[] tutorialGuides,
        MiningTileDefinition[] miningTiles)
    {
        _miningTiles = miningTiles ?? Array.Empty<MiningTileDefinition>();
        _resources = resources ?? Array.Empty<ResourceDefinition>();
        _creatures = creatures ?? Array.Empty<CreatureDefinition>();
        _floatages = floatages ?? Array.Empty<FloatageDefinition>();
        _upgrades = upgrades ?? Array.Empty<UpgradeNodeDefinition>();
        _tutorialGuides = tutorialGuides ?? Array.Empty<TutorialGuideDefinition>();
    }
#endif

    private static void ValidateDefinitions<T>(
        IReadOnlyList<T> definitions,
        Func<T, string> getId,
        ICollection<string> errors)
        where T : ScriptableObject
    {
        HashSet<string> ids = new(StringComparer.Ordinal);

        for (int i = 0; i < definitions.Count; i++)
        {
            T definition = definitions[i];
            if (definition == null)
            {
                errors.Add($"{typeof(T).Name} entry at index {i} is null.");
                continue;
            }

            string id = getId(definition)?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                errors.Add($"{typeof(T).Name} '{definition.name}' has an empty id.");
                continue;
            }

            if (!ids.Add(id))
            {
                errors.Add($"Duplicate {typeof(T).Name} id '{id}'.");
            }
        }
    }

    private void ValidateUpgradeTree(ICollection<string> errors)
    {
        HashSet<UpgradeNodeDefinition> nodes = new(_upgrades);
        HashSet<ResourceDefinition> resources = new(_resources);
        HashSet<MiningTileDefinition> miningTiles = new(_miningTiles);
        for (int i = 0; i < _upgrades.Length; i++)
        {
            UpgradeNodeDefinition node = _upgrades[i];
            if (node == null)
            {
                continue;
            }

            if (!node.TryValidate(out string nodeError))
            {
                errors.Add(nodeError);
            }

            ValidateCostResources(node.BaseCosts, node, resources, errors);
            ValidateCostResources(node.CostIncreases, node, resources, errors);
            ValidateMiningTileEffects(node, miningTiles, errors);

            if (node.Parent != null && !nodes.Contains(node.Parent))
            {
                errors.Add(
                    $"Upgrade node '{node.Id}' references parent '{node.Parent.Id}', " +
                    "but the parent is not in this catalog.");
            }

            HashSet<UpgradeNodeDefinition> ancestors = new();
            UpgradeNodeDefinition current = node;
            while (current != null)
            {
                if (!ancestors.Add(current))
                {
                    errors.Add($"Upgrade tree cycle detected at node '{node.Id}'.");
                    break;
                }

                current = current.Parent;
            }
        }
    }

    private static void ValidateMiningTileEffects(
        UpgradeNodeDefinition node,
        ISet<MiningTileDefinition> catalogMiningTiles,
        ICollection<string> errors)
    {
        for (int i = 0; i < node.Effects.Count; i++)
        {
            if (node.Effects[i] is not MiningTileDropMultiplierUpgradeEffect &&
                node.Effects[i] is not MiningTileDropBonusUpgradeEffect)
            {
                continue;
            }

            MiningTileDefinition miningTile = node.Effects[i] switch
            {
                MiningTileDropMultiplierUpgradeEffect multiplier => multiplier.MiningTile,
                MiningTileDropBonusUpgradeEffect bonus => bonus.MiningTile,
                _ => null
            };

            if (miningTile != null && !catalogMiningTiles.Contains(miningTile))
            {
                errors.Add(
                    $"Upgrade node '{node.Id}' references miningTile " +
                    $"'{miningTile.name}', but it is not in this catalog.");
            }
        }
    }

    private void ValidateMiningTiles(ICollection<string> errors)
    {
        HashSet<MiningTileDefinition> seen = new();
        HashSet<ResourceDefinition> resources = new(_resources);
        for (int i = 0; i < _miningTiles.Length; i++)
        {
            MiningTileDefinition tile = _miningTiles[i];
            if (tile == null)
            {
                errors.Add($"Mining tile entry at index {i} is null.");
                continue;
            }
            if (!seen.Add(tile)) errors.Add($"Duplicate mining tile '{tile.name}'.");
            if (!tile.TryValidate(out string error)) errors.Add(error);
            if (tile.DropResource != null && !resources.Contains(tile.DropResource))
                errors.Add($"Mining tile '{tile.name}' uses a resource outside this catalog.");
        }
    }

    private void ValidateFloatages(ICollection<string> errors)
    {
        HashSet<ResourceDefinition> resources = new(_resources);
        for (int i = 0; i < _floatages.Length; i++)
        {
            FloatageDefinition definition = _floatages[i];
            if (definition == null)
            {
                continue;
            }

            if (!definition.TryValidate(out string definitionError))
            {
                errors.Add(definitionError);
            }

            ResourceDefinition resource = definition.DropResource;
            if (resource != null && !resources.Contains(resource))
            {
                errors.Add(
                    $"Floatage definition '{definition.Id}' uses resource " +
                    $"'{resource.Id}', but that resource is not in this catalog.");
            }
        }
    }

    private void ValidateResources(ICollection<string> errors)
    {
        for (int i = 0; i < _resources.Length; i++)
        {
            ResourceDefinition resource = _resources[i];
            if (resource != null && !resource.TryValidate(out string resourceError))
            {
                errors.Add(resourceError);
            }
        }
    }

    private void ValidateCreatures(ICollection<string> errors)
    {
        for (int i = 0; i < _creatures.Length; i++)
        {
            CreatureDefinition creature = _creatures[i];
            if (creature != null && !creature.TryValidate(out string creatureError))
            {
                errors.Add(creatureError);
            }
        }
    }

    private static void ValidateCostResources(
        IReadOnlyList<UpgradeResourceCost> costs,
        UpgradeNodeDefinition node,
        ISet<ResourceDefinition> catalogResources,
        ICollection<string> errors)
    {
        for (int i = 0; i < costs.Count; i++)
        {
            ResourceDefinition resource = costs[i].Resource;
            if (resource != null && !catalogResources.Contains(resource))
            {
                errors.Add(
                    $"Upgrade node '{node.Id}' uses resource '{resource.Id}', " +
                    "but that resource is not in this catalog.");
            }
        }
    }
}
