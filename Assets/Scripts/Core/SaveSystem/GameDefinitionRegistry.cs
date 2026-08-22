using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameDefinitionRegistry
{
    private readonly Dictionary<string, ResourceDefinition> _resources;
    private readonly Dictionary<string, CreatureDefinition> _creatures;
    private readonly Dictionary<string, UpgradeNodeDefinition> _upgrades;
    private readonly TutorialGuideDefinition[] _tutorialGuides;
    private readonly ResourceDefinition[] _orderedResources;

    public IReadOnlyList<ResourceDefinition> OrderedResources =>
        _orderedResources;
    public IReadOnlyList<TutorialGuideDefinition> TutorialGuides =>
        _tutorialGuides;

    public GameDefinitionRegistry(GameDefinitionCatalog catalog)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (!catalog.TryValidate(out string validationError))
        {
            Debug.LogError(
                $"Game definition catalog '{catalog.name}' is invalid.\n{validationError}",
                catalog);
        }

        _resources = BuildLookup(catalog.Resources, definition => definition.Id);
        _creatures = BuildLookup(catalog.Creatures, definition => definition.Id);
        _upgrades = BuildLookup(catalog.Upgrades, definition => definition.Id);
        _tutorialGuides = BuildOrderedDefinitions(catalog.TutorialGuides);
        _orderedResources = BuildOrderedResources(_resources.Values);
    }

    public bool TryGetResource(string id, out ResourceDefinition definition)
    {
        return _resources.TryGetValue(id ?? string.Empty, out definition);
    }

    public bool TryGetCreature(string id, out CreatureDefinition definition)
    {
        return _creatures.TryGetValue(id ?? string.Empty, out definition);
    }

    public bool TryGetUpgrade(string id, out UpgradeNodeDefinition definition)
    {
        return _upgrades.TryGetValue(id ?? string.Empty, out definition);
    }

    private static Dictionary<string, T> BuildLookup<T>(
        IReadOnlyList<T> definitions,
        Func<T, string> getId)
        where T : ScriptableObject
    {
        Dictionary<string, T> lookup = new(StringComparer.Ordinal);

        foreach (T definition in definitions)
        {
            if (definition == null)
            {
                continue;
            }

            string id = getId(definition)?.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            if (!lookup.TryAdd(id, definition))
            {
                continue;
            }
        }

        return lookup;
    }

    private static ResourceDefinition[] BuildOrderedResources(
        ICollection<ResourceDefinition> definitions)
    {
        ResourceDefinition[] ordered =
            new ResourceDefinition[definitions.Count];
        definitions.CopyTo(ordered, 0);
        Array.Sort(ordered, ResourceDisplayOrder.Compare);
        return ordered;
    }

    private static TutorialGuideDefinition[] BuildOrderedDefinitions(
        IReadOnlyList<TutorialGuideDefinition> definitions)
    {
        List<TutorialGuideDefinition> ordered = new();
        for (int index = 0; index < definitions.Count; index++)
        {
            if (definitions[index] != null)
            {
                ordered.Add(definitions[index]);
            }
        }

        ordered.Sort((left, right) =>
        {
            int sortOrder = left.SortOrder.CompareTo(right.SortOrder);
            return sortOrder != 0
                ? sortOrder
                : string.Compare(left.Key, right.Key, StringComparison.Ordinal);
        });
        return ordered.ToArray();
    }
}
