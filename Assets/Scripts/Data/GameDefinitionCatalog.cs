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

    public IReadOnlyList<ResourceDefinition> Resources => _resources;
    public IReadOnlyList<CreatureDefinition> Creatures => _creatures;

    public bool TryValidate(out string error)
    {
        List<string> errors = new();
        ValidateDefinitions(_resources, definition => definition.Id, errors);
        ValidateDefinitions(_creatures, definition => definition.Id, errors);
        error = string.Join(Environment.NewLine, errors);
        return errors.Count == 0;
    }

#if UNITY_EDITOR
    public void SetDefinitionsForEditor(
        ResourceDefinition[] resources,
        CreatureDefinition[] creatures)
    {
        _resources = resources ?? Array.Empty<ResourceDefinition>();
        _creatures = creatures ?? Array.Empty<CreatureDefinition>();
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
}
