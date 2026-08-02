using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StagePopulationDefinition
{
    [SerializeField, Min(0)] private int _maxCount;
    [SerializeField, Range(0f, 1f)] private float _respawnProbability = 0.5f;
    [SerializeField] private StageSpawnEntry[] _entries =
        Array.Empty<StageSpawnEntry>();

    public int MaxCount => Mathf.Max(0, _maxCount);
    public float RespawnProbability => Mathf.Clamp01(_respawnProbability);
    public IReadOnlyList<StageSpawnEntry> Entries =>
        _entries ?? Array.Empty<StageSpawnEntry>();

    public StageRuntimePopulationConfig CreateRuntimeCopy()
    {
        StageRuntimeSpawnEntry[] entries =
            new StageRuntimeSpawnEntry[Entries.Count];
        for (int i = 0; i < Entries.Count; i++)
        {
            entries[i] = Entries[i]?.CreateRuntimeCopy();
        }

        return new StageRuntimePopulationConfig(
            MaxCount,
            RespawnProbability,
            entries);
    }
}

[CreateAssetMenu(
    fileName = "StageDefinition",
    menuName = "Astrodiver/Stage/Stage Definition")]
public sealed class StageDefinition : ScriptableObject
{
    [SerializeField] private string _stageId;
    [SerializeField, Min(0.1f)] private float _respawnIntervalSeconds = 5f;
    [SerializeField] private StagePopulationDefinition _creatures = new();
    [SerializeField] private StagePopulationDefinition _resourceFloatages = new();

    public string StageId => _stageId;
    public float RespawnIntervalSeconds =>
        Mathf.Max(0.1f, _respawnIntervalSeconds);
    public StagePopulationDefinition Creatures => _creatures;
    public StagePopulationDefinition ResourceFloatages => _resourceFloatages;

    public StageRuntimeConfig CreateRuntimeConfig()
    {
        return new StageRuntimeConfig(
            _stageId,
            RespawnIntervalSeconds,
            _creatures?.CreateRuntimeCopy(),
            _resourceFloatages?.CreateRuntimeCopy());
    }

    public bool TryValidate(out string error)
    {
        List<string> errors = new();
        if (string.IsNullOrWhiteSpace(_stageId))
        {
            errors.Add($"Stage definition '{name}' has an empty stage id.");
        }

        if (_respawnIntervalSeconds < 0.1f)
        {
            errors.Add("Respawn interval must be at least 0.1 seconds.");
        }

        HashSet<string> entryIds = new(StringComparer.Ordinal);
        ValidatePopulation(
            _creatures,
            StageSpawnCategory.Creature,
            entryIds,
            errors);
        ValidatePopulation(
            _resourceFloatages,
            StageSpawnCategory.ResourceFloatage,
            entryIds,
            errors);

        error = string.Join(Environment.NewLine, errors);
        return errors.Count == 0;
    }

    private static void ValidatePopulation(
        StagePopulationDefinition population,
        StageSpawnCategory category,
        ISet<string> entryIds,
        ICollection<string> errors)
    {
        if (population == null)
        {
            errors.Add($"{category} population is not assigned.");
            return;
        }

        float totalWeight = 0f;
        IReadOnlyList<StageSpawnEntry> entries = population.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            StageSpawnEntry entry = entries[i];
            string label = $"{category} entry {i}";
            if (entry == null)
            {
                errors.Add($"{label} is null.");
                continue;
            }

            string entryId = entry.EntryId?.Trim();
            if (string.IsNullOrEmpty(entryId))
            {
                errors.Add($"{label} has an empty entry id.");
            }
            else if (!entryIds.Add(entryId))
            {
                errors.Add($"Duplicate stage spawn entry id '{entryId}'.");
            }

            totalWeight += entry.SpawnWeight;
            if (entry.Prefab == null)
            {
                errors.Add($"{label} has no prefab.");
                continue;
            }

            if (category == StageSpawnCategory.Creature &&
                entry.Prefab.GetComponent<CreatureController>() == null)
            {
                errors.Add(
                    $"Creature entry '{entryId}' prefab '{entry.Prefab.name}' " +
                    "does not contain CreatureController.");
            }

            if (category == StageSpawnCategory.ResourceFloatage &&
                entry.Prefab.GetComponent<FloatageController>() == null)
            {
                errors.Add(
                    $"Resource entry '{entryId}' prefab '{entry.Prefab.name}' " +
                    "does not contain FloatageController.");
            }
        }

        if (population.MaxCount > 0 && entries.Count == 0)
        {
            errors.Add($"{category} population has no spawn entries.");
        }
        else if (population.MaxCount > 0 && totalWeight <= Mathf.Epsilon)
        {
            errors.Add($"{category} population has no positive spawn weight.");
        }
    }
}

public sealed class StageRuntimeConfig
{
    public StageRuntimeConfig(
        string stageId,
        float respawnIntervalSeconds,
        StageRuntimePopulationConfig creatures,
        StageRuntimePopulationConfig resourceFloatages)
    {
        StageId = stageId;
        RespawnIntervalSeconds = Mathf.Max(0.1f, respawnIntervalSeconds);
        Creatures = creatures ?? StageRuntimePopulationConfig.Empty();
        ResourceFloatages =
            resourceFloatages ?? StageRuntimePopulationConfig.Empty();
    }

    public string StageId { get; }
    public float RespawnIntervalSeconds { get; }
    public StageRuntimePopulationConfig Creatures { get; }
    public StageRuntimePopulationConfig ResourceFloatages { get; }
}

public sealed class StageRuntimePopulationConfig
{
    private int _maxCount;
    private float _respawnProbability;

    public StageRuntimePopulationConfig(
        int maxCount,
        float respawnProbability,
        StageRuntimeSpawnEntry[] entries)
    {
        MaxCount = maxCount;
        RespawnProbability = respawnProbability;
        Entries = entries ?? Array.Empty<StageRuntimeSpawnEntry>();
    }

    public int MaxCount
    {
        get => _maxCount;
        set => _maxCount = Mathf.Max(0, value);
    }

    public float RespawnProbability
    {
        get => _respawnProbability;
        set => _respawnProbability = Mathf.Clamp01(value);
    }

    public StageRuntimeSpawnEntry[] Entries { get; }

    public static StageRuntimePopulationConfig Empty()
    {
        return new StageRuntimePopulationConfig(
            0,
            0f,
            Array.Empty<StageRuntimeSpawnEntry>());
    }
}
