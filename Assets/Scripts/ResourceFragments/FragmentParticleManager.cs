using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FragmentParticleManager : MonoBehaviour
{
    [Header("Particle System Setup")]
    [SerializeField] private ParticleSystem _fragmentParticleSystemPrefab;

    [Header("Magnet Settings")]
    [SerializeField] private MagnetData _magnetData;

    private readonly Dictionary<string, FragmentParticleEntry> _particleSystems =
        new(StringComparer.Ordinal);

    private ParticleSystem.EmitParams _emitParams;
    private FragmentMagnetManager _magnetManager;
    private PlayerInventoryController _playerInventory;

    public static FragmentParticleManager Instance { get; private set; }

    public void DropFragment(Vector3 position, FragmentDropData data)
    {
        if (data.resource == null)
        {
            Debug.LogError("Cannot drop fragments without a resource definition.", this);
            return;
        }

        if (data.count <= 0)
        {
            return;
        }

        if (!TryGetOrCreateParticleSystem(
                data.resource,
                out FragmentParticleEntry entry))
        {
            return;
        }

        ParticleSystem particleSystem = entry.ParticleSystem;
        var shape = particleSystem.shape;
        shape.radius = data.radius;

        _emitParams.position = position;
        _emitParams.startLifetime = data.lifetime;
        _emitParams.applyShapeToPosition = true;

        particleSystem.Emit(_emitParams, data.count);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"{nameof(FragmentParticleManager)} already exists. Destroying the duplicate.",
                this);
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (GameDataManager.Instance != null)
        {
            _magnetData = GameDataManager.Instance.GetOrInitializeMagnet(_magnetData);
        }

        _magnetManager = new FragmentMagnetManager(_magnetData);
    }

    private void Start()
    {
        _playerInventory = PlayerInventoryController.Instance;
    }

    private void Update()
    {
        if (_playerInventory == null)
        {
            _playerInventory = PlayerInventoryController.Instance;
        }

        if (_playerInventory == null || PlayerContext.Instance == null)
        {
            return;
        }

        Vector3 playerPosition = PlayerContext.Instance.transform.position;

        foreach (FragmentParticleEntry entry in _particleSystems.Values)
        {
            if (entry.ParticleSystem.particleCount == 0)
            {
                continue;
            }

            _magnetManager.Process(
                entry.ParticleSystem,
                entry.Resource,
                playerPosition,
                _playerInventory);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool TryGetOrCreateParticleSystem(
        ResourceDefinition resource,
        out FragmentParticleEntry entry)
    {
        entry = null;

        string resourceId = resource.Id?.Trim();
        if (string.IsNullOrEmpty(resourceId))
        {
            Debug.LogError($"Resource '{resource.name}' has an empty id.", resource);
            return false;
        }

        if (_particleSystems.TryGetValue(resourceId, out entry))
        {
            if (entry.Resource == resource)
            {
                return true;
            }

            Debug.LogError(
                $"Fragment resource id '{resourceId}' is already registered by " +
                $"'{entry.Resource.name}', so '{resource.name}' cannot use it.",
                resource);
            entry = null;
            return false;
        }

        if (_fragmentParticleSystemPrefab == null)
        {
            Debug.LogError("Fragment particle system prefab is not assigned.", this);
            return false;
        }

        ParticleSystem particleSystem = Instantiate(_fragmentParticleSystemPrefab, transform);
        particleSystem.gameObject.name = string.IsNullOrWhiteSpace(resource.DisplayName)
            ? resourceId
            : resource.DisplayName;
        particleSystem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        ConfigureParticleSystem(particleSystem, resource);
        entry = new FragmentParticleEntry(resource, particleSystem);
        _particleSystems.Add(resourceId, entry);

        return true;
    }

    private static void ConfigureParticleSystem(
        ParticleSystem particleSystem,
        ResourceDefinition resource)
    {
        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particleSystem.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var textureSheet = particleSystem.textureSheetAnimation;
        textureSheet.enabled = true;
        textureSheet.mode = ParticleSystemAnimationMode.Grid;
        textureSheet.animation = ParticleSystemAnimationType.SingleRow;
        textureSheet.rowMode = ParticleSystemAnimationRowMode.Custom;
        textureSheet.rowIndex = resource.RowIndex;
        textureSheet.frameOverTime = 0f;

        if (resource.RowIndex >= textureSheet.numTilesY)
        {
            Debug.LogError(
                $"Resource '{resource.name}' uses particle row {resource.RowIndex}, " +
                $"but the particle sheet only has {textureSheet.numTilesY} rows.",
                resource);
        }

        particleSystem.Play();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (PlayerContext.Instance == null)
        {
            return;
        }

        Vector3 playerPosition = PlayerContext.Instance.transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerPosition, _magnetData.radius);

        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(playerPosition, _magnetData.collectRadius);
    }
#endif

    private sealed class FragmentParticleEntry
    {
        public FragmentParticleEntry(
            ResourceDefinition resource,
            ParticleSystem particleSystem)
        {
            Resource = resource;
            ParticleSystem = particleSystem;
        }

        public ResourceDefinition Resource { get; }
        public ParticleSystem ParticleSystem { get; }
    }
}
