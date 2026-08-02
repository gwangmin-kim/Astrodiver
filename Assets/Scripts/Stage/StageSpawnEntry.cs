using UnityEngine;

[System.Serializable]
public sealed class StageSpawnEntry
{
    [SerializeField] private string _entryId;
    [SerializeField] private GameObject _prefab;
    [SerializeField, Range(0f, 1f)] private float _spawnWeight = 1f;

    public string EntryId => _entryId;
    public GameObject Prefab => _prefab;
    public float SpawnWeight => Mathf.Clamp01(_spawnWeight);

    public StageRuntimeSpawnEntry CreateRuntimeCopy()
    {
        return new StageRuntimeSpawnEntry(
            _entryId,
            _prefab,
            SpawnWeight);
    }
}

public sealed class StageRuntimeSpawnEntry
{
    private float _spawnWeight;

    public StageRuntimeSpawnEntry(
        string entryId,
        GameObject prefab,
        float spawnWeight)
    {
        EntryId = entryId;
        Prefab = prefab;
        SpawnWeight = spawnWeight;
    }

    public string EntryId { get; }
    public GameObject Prefab { get; }

    public float SpawnWeight
    {
        get => _spawnWeight;
        set => _spawnWeight = Mathf.Clamp01(value);
    }
}
