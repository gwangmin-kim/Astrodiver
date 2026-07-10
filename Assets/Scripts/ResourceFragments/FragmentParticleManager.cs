using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FragmentParticleManager : MonoBehaviour
{
    [SerializeField] private ResourceDefinition _resource;

    private ParticleSystem _masterParticleSystem;
    private ParticleSystem.EmitParams _emitParams;

    public static FragmentParticleManager Instance { get; private set; }

    public ResourceDefinition Resource => _resource;

    public void DropFragment(Vector3 position, FragmentDropData data)
    {
        var shapeModule = _masterParticleSystem.shape;
        shapeModule.radius = data.radius;

        _emitParams.position = position;
        _emitParams.startLifetime = data.lifetime;
        _emitParams.applyShapeToPosition = true;

        _masterParticleSystem.Emit(_emitParams, data.count);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"{nameof(FragmentParticleManager)} already exists. Replacing singleton instance.", this);
        }

        Instance = this;
        _masterParticleSystem = GetComponent<ParticleSystem>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
