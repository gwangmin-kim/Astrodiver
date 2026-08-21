using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WorktableRecipe
{
    [SerializeField] private CreatureDefinition _creature;
    [SerializeField] private ResourceDefinition _resource;
    [SerializeField, Min(1)] private int _baseAmount = 1;

    public CreatureDefinition Creature => _creature;
    public ResourceDefinition Resource => _resource;
    public int BaseAmount => Mathf.Max(1, _baseAmount);
}

[CreateAssetMenu(
    fileName = "WorktableRecipeTable",
    menuName = "Astrodiver/Facilities/Worktable Recipe Table")]
public sealed class WorktableRecipeTable : ScriptableObject
{
    [SerializeField] private WorktableRecipe[] _recipes =
        Array.Empty<WorktableRecipe>();

    public IReadOnlyList<WorktableRecipe> Recipes =>
        _recipes ?? Array.Empty<WorktableRecipe>();

    public bool TryGetRecipe(string creatureId, out WorktableRecipe recipe)
    {
        string normalizedId = creatureId?.Trim();
        IReadOnlyList<WorktableRecipe> recipes = Recipes;
        for (int i = 0; i < recipes.Count; i++)
        {
            WorktableRecipe candidate = recipes[i];
            if (candidate?.Creature != null && string.Equals(
                    candidate.Creature.Id,
                    normalizedId,
                    StringComparison.Ordinal))
            {
                recipe = candidate;
                return true;
            }
        }

        recipe = null;
        return false;
    }

    public bool TryValidate(out string error)
    {
        HashSet<string> creatureIds = new(StringComparer.Ordinal);
        IReadOnlyList<WorktableRecipe> recipes = Recipes;
        for (int i = 0; i < recipes.Count; i++)
        {
            WorktableRecipe recipe = recipes[i];
            if (recipe?.Creature == null || recipe.Resource == null)
            {
                error = $"Worktable recipe {i} requires a creature and resource.";
                return false;
            }

            if (!creatureIds.Add(recipe.Creature.Id))
            {
                error = $"Duplicate worktable recipe for '{recipe.Creature.Id}'.";
                return false;
            }

            if (recipe.BaseAmount <= 0)
            {
                error = $"Worktable recipe {i} requires a positive amount.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private void OnValidate()
    {
        _recipes ??= Array.Empty<WorktableRecipe>();
    }
}
