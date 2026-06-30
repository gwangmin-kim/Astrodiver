using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FragmentParticleManager : MonoBehaviour
{
    // 싱글톤
    public static FragmentParticleManager Instance { get; private set; }

    private ParticleSystem _masterParticleSystem;
    private ParticleSystem.EmitParams _emitParams;

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
        if (Instance == null)
        {
            Instance = this;

            _masterParticleSystem = GetComponent<ParticleSystem>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
