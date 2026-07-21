using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameDefinitionRegistry
{
    private readonly Dictionary<string, ResourceDefinition> _resources;
    private readonly Dictionary<string, CreatureDefinition> _creatures;

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
    }

    public bool TryGetResource(string id, out ResourceDefinition definition)
    {
        return _resources.TryGetValue(id ?? string.Empty, out definition);
    }

    public bool TryGetCreature(string id, out CreatureDefinition definition)
    {
        return _creatures.TryGetValue(id ?? string.Empty, out definition);
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
}
