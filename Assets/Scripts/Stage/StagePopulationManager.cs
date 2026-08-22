using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StagePopulationManager : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private StageDefinition _definition;

    [Header("Scene-owned Spawn Areas")]
    [SerializeField] private StageSpawnAreaCollection _spawnAreas = new();

    [Header("Runtime Hierarchy")]
    [SerializeField] private Transform _creatureRuntimeRoot;
    [SerializeField] private Transform _resourceRuntimeRoot;

    [Header("Initialization")]
    [SerializeField] private bool _spawnOnStart = true;
    [SerializeField] private bool _useFixedSeed;
    [SerializeField] private int _fixedSeed = 12345;

    private readonly List<float> _areaWeights = new();
    private readonly List<float> _entryWeights = new();
    private readonly HashSet<StageSpawnedObject> _creatures = new();
    private readonly HashSet<StageSpawnedObject> _resourceFloatages = new();

    private StageRuntimeConfig _runtimeConfig;
    private GameDataManager _gameDataManager;
    private System.Random _random;
    private Coroutine _respawnRoutine;
    private int _creatureSequence;
    private int _resourceSequence;
    private bool _hasSpawned;

    public StageDefinition Definition => _definition;
    public StageSpawnAreaCollection SpawnAreas => _spawnAreas;
    public StageRuntimeConfig RuntimeConfig => _runtimeConfig;
    public bool HasSpawned => _hasSpawned;
    public int CreatureCount => GetAliveCount(StageSpawnCategory.Creature);
    public int ResourceFloatageCount =>
        GetAliveCount(StageSpawnCategory.ResourceFloatage);
    public event Action InitialSpawnCompleted;
    public event Action RespawnTickCompleted;

    private void Start()
    {
        BindGameDataManager();
        if (_spawnOnStart)
        {
            SpawnInitialPopulation();
        }
    }

    private void OnEnable()
    {
        BindGameDataManager();
        if (_hasSpawned)
        {
            StartRespawnLoop();
        }
    }

    private void OnDisable()
    {
        UnbindGameDataManager();
        if (_respawnRoutine == null)
        {
            return;
        }

        StopCoroutine(_respawnRoutine);
        _respawnRoutine = null;
    }

    public void SpawnInitialPopulation()
    {
        if (_hasSpawned)
        {
            return;
        }

        if (!TryPrepare(out StageRuntimeConfig runtimeConfig))
        {
            return;
        }

        _runtimeConfig = runtimeConfig;
        _random = _useFixedSeed
            ? new System.Random(_fixedSeed)
            : new System.Random(Guid.NewGuid().GetHashCode());

        SpawnBatch(
            _runtimeConfig.StageId,
            _runtimeConfig.Creatures,
            StageSpawnCategory.Creature,
            _spawnAreas.CreatureAreas,
            _creatureRuntimeRoot,
            _runtimeConfig.Creatures.MaxCount);
        SpawnBatch(
            _runtimeConfig.StageId,
            _runtimeConfig.ResourceFloatages,
            StageSpawnCategory.ResourceFloatage,
            _spawnAreas.ResourceAreas,
            _resourceRuntimeRoot,
            _runtimeConfig.ResourceFloatages.MaxCount);

        _hasSpawned = true;
        InitialSpawnCompleted?.Invoke();
        StartRespawnLoop();
    }

    public void ProcessRespawnTick()
    {
        if (!_hasSpawned || _runtimeConfig == null || _random == null)
        {
            return;
        }

        ProcessPopulationRespawn(
            _runtimeConfig.Creatures,
            StageSpawnCategory.Creature,
            _spawnAreas.CreatureAreas,
            _creatureRuntimeRoot);
        ProcessPopulationRespawn(
            _runtimeConfig.ResourceFloatages,
            StageSpawnCategory.ResourceFloatage,
            _spawnAreas.ResourceAreas,
            _resourceRuntimeRoot);
        RespawnTickCompleted?.Invoke();
    }

    internal void Register(StageSpawnedObject spawnedObject)
    {
        if (spawnedObject == null)
        {
            return;
        }

        GetRegistry(spawnedObject.Category).Add(spawnedObject);
    }

    internal void Unregister(StageSpawnedObject spawnedObject)
    {
        if (ReferenceEquals(spawnedObject, null))
        {
            return;
        }

        GetRegistry(spawnedObject.Category).Remove(spawnedObject);
    }

    private bool TryPrepare(out StageRuntimeConfig runtimeConfig)
    {
        runtimeConfig = null;
        if (_definition == null)
        {
            Debug.LogError(
                "StagePopulationManager: StageDefinition is not assigned.",
                this);
            return false;
        }

        if (!_definition.TryValidate(out string definitionError))
        {
            Debug.LogError(
                $"StagePopulationManager: Invalid definition.\n{definitionError}",
                _definition);
            return false;
        }

        if (_spawnAreas == null)
        {
            Debug.LogError(
                "StagePopulationManager: Spawn areas are not assigned.",
                this);
            return false;
        }

        if (!_spawnAreas.TryValidate(out string areaError))
        {
            Debug.LogError(
                $"StagePopulationManager: Invalid spawn areas.\n{areaError}",
                this);
            return false;
        }

        if (_definition.Creatures.MaxCount > 0 &&
            _spawnAreas.CreatureAreas.Count == 0)
        {
            Debug.LogError(
                "StagePopulationManager: No valid creature spawn areas found.",
                this);
            return false;
        }

        if (_definition.ResourceFloatages.MaxCount > 0 &&
            _spawnAreas.ResourceAreas.Count == 0)
        {
            Debug.LogError(
                "StagePopulationManager: No valid resource spawn areas found.",
                this);
            return false;
        }

        runtimeConfig = _definition.CreateRuntimeConfig();
        runtimeConfig.SetRespawnProbabilityBonus(
            _gameDataManager?.RuntimeData?.StageRespawnProbabilityBonuses
                .GetBonus(_definition) ?? 0f);
        return true;
    }

    private void BindGameDataManager()
    {
        GameDataManager manager = GameDataManager.Instance;
        if (_gameDataManager == manager)
        {
            return;
        }

        UnbindGameDataManager();
        _gameDataManager = manager;
        if (_gameDataManager != null)
        {
            _gameDataManager.RuntimeDataChanged += HandleRuntimeDataChanged;
            HandleRuntimeDataChanged(_gameDataManager.RuntimeData);
        }
    }

    private void UnbindGameDataManager()
    {
        if (_gameDataManager != null)
        {
            _gameDataManager.RuntimeDataChanged -= HandleRuntimeDataChanged;
            _gameDataManager = null;
        }
    }

    private void HandleRuntimeDataChanged(GameRuntimeData runtimeData)
    {
        if (_runtimeConfig == null || _definition == null)
        {
            return;
        }

        _runtimeConfig.SetRespawnProbabilityBonus(
            runtimeData?.StageRespawnProbabilityBonuses.GetBonus(_definition) ??
            0f);
    }

    private void ProcessPopulationRespawn(
        StageRuntimePopulationConfig population,
        StageSpawnCategory category,
        IReadOnlyList<StageSpawnRect> areas,
        Transform runtimeRoot)
    {
        int guaranteedCount = StageSpawnPlanner.SampleGuaranteedCount(
            population.MaxCount,
            population.RespawnProbability,
            _random);
        int deficit = guaranteedCount - GetAliveCount(category);
        if (deficit <= 0)
        {
            return;
        }

        SpawnBatch(
            _runtimeConfig.StageId,
            population,
            category,
            areas,
            runtimeRoot,
            deficit);
    }

    private void SpawnBatch(
        string stageId,
        StageRuntimePopulationConfig population,
        StageSpawnCategory category,
        IReadOnlyList<StageSpawnRect> areas,
        Transform runtimeRoot,
        int count)
    {
        if (count <= 0 || population == null || areas.Count == 0)
        {
            return;
        }

        _entryWeights.Clear();
        float totalEntryWeight = 0f;
        for (int i = 0; i < population.Entries.Length; i++)
        {
            StageRuntimeSpawnEntry entry = population.Entries[i];
            float weight = entry?.SpawnWeight ?? 0f;
            _entryWeights.Add(weight);
            totalEntryWeight += weight;
        }

        if (totalEntryWeight <= Mathf.Epsilon)
        {
            Debug.LogWarning(
                $"StagePopulationManager: {category} has no positive " +
                "runtime spawn weight.",
                this);
            return;
        }

        _areaWeights.Clear();
        for (int i = 0; i < areas.Count; i++)
        {
            _areaWeights.Add(areas[i].Area);
        }

        int[] allocations = StageSpawnPlanner.AllocateByWeight(
            count,
            _areaWeights,
            _random);
        for (int areaIndex = 0; areaIndex < areas.Count; areaIndex++)
        {
            for (int i = 0; i < allocations[areaIndex]; i++)
            {
                int entryIndex = StageSpawnPlanner.SelectWeightedIndex(
                    _entryWeights,
                    _random);
                if (entryIndex < 0)
                {
                    return;
                }

                StageRuntimeSpawnEntry entry = population.Entries[entryIndex];
                if (entry == null || entry.Prefab == null)
                {
                    continue;
                }

                SpawnOne(
                    stageId,
                    entry,
                    category,
                    areas[areaIndex],
                    areaIndex,
                    runtimeRoot);
            }
        }
    }

    private void SpawnOne(
        string stageId,
        StageRuntimeSpawnEntry entry,
        StageSpawnCategory category,
        StageSpawnRect area,
        int areaIndex,
        Transform runtimeRoot)
    {
        GameObject instance = Instantiate(
            entry.Prefab,
            transform.TransformPoint(area.GetRandomLocalPoint(_random)),
            entry.Prefab.transform.rotation,
            runtimeRoot);
        instance.name = $"{entry.Prefab.name}_{NextSequence(category):000}";

        StageSpawnedObject spawnedObject =
            instance.GetComponent<StageSpawnedObject>() ??
            instance.AddComponent<StageSpawnedObject>();
        spawnedObject.Initialize(
            this,
            stageId,
            entry.EntryId,
            category,
            areaIndex);
    }

    private int GetAliveCount(StageSpawnCategory category)
    {
        HashSet<StageSpawnedObject> registry = GetRegistry(category);
        registry.RemoveWhere(
            item => item == null || item.IsRemovedFromStage);
        return registry.Count;
    }

    private HashSet<StageSpawnedObject> GetRegistry(
        StageSpawnCategory category)
    {
        return category == StageSpawnCategory.Creature
            ? _creatures
            : _resourceFloatages;
    }

    private int NextSequence(StageSpawnCategory category)
    {
        if (category == StageSpawnCategory.Creature)
        {
            return ++_creatureSequence;
        }

        return ++_resourceSequence;
    }

    private void StartRespawnLoop()
    {
        if (!isActiveAndEnabled || !_hasSpawned || _runtimeConfig == null ||
            _respawnRoutine != null)
        {
            return;
        }

        _respawnRoutine = StartCoroutine(RespawnLoop());
    }

    private IEnumerator RespawnLoop()
    {
        WaitForSeconds wait =
            new(_runtimeConfig.RespawnIntervalSeconds);
        while (true)
        {
            yield return wait;
            ProcessRespawnTick();
        }
    }
}
