using UnityEngine;

/// <summary>
/// Pulls particles emitted around the plasma muzzle into its centre while the
/// gun charges. All particle motion is in local space, so it follows the gun.
/// </summary>
public sealed class PlasmaGunChargeParticles : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private ParticleSystem _particleSystem;

    [Header("Charge Particle Settings")]
    [SerializeField, Min(0.01f)] private float _spawnRadius = 0.45f;
    [SerializeField, Min(0f)] private float _emissionRate = 24f;
    [SerializeField, Min(0.01f)] private float _particleLifetime = 0.65f;
    [SerializeField, Min(0.001f)] private float _particleSize = 0.05f;
    [SerializeField, Min(0f)] private float _initialConvergeSpeed = 0.4f;
    [SerializeField, Min(0f)] private float _finalConvergeSpeed = 3.5f;
    [SerializeField] private AnimationCurve _convergeSpeedOverCharge =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Color _particleColor = new(0.2f, 1f, 1f, 1f);
    [SerializeField, Min(0f)] private float _glowIntensity = 2f;

    private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[64];
    private bool _isCharging;
    private float _convergeSpeed;

    private void Awake()
    {
        if (_particleSystem == null)
        {
            Debug.LogWarning("Plasma charge particles need a preconfigured ParticleSystem.", this);
            enabled = false;
            return;
        }

        ConfigureParticleSystem();
        _particleSystem.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!_isCharging || _particleSystem == null)
        {
            return;
        }

        int aliveCount = _particleSystem.GetParticles(_particles);
        for (int i = 0; i < aliveCount; i++)
        {
            Vector3 toMuzzle = -_particles[i].position;
            _particles[i].velocity = toMuzzle.sqrMagnitude > 0.0001f
                ? toMuzzle.normalized * _convergeSpeed
                : Vector3.zero;
        }

        _particleSystem.SetParticles(_particles, aliveCount);
    }

    public void SetCharging(bool isCharging, float normalizedProgress)
    {
        if (_particleSystem == null || !enabled)
        {
            return;
        }

        if (!isCharging)
        {
            if (_isCharging)
            {
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _particleSystem.gameObject.SetActive(false);
            }

            _isCharging = false;
            return;
        }

        _convergeSpeed = Mathf.Lerp(
            _initialConvergeSpeed,
            _finalConvergeSpeed,
            _convergeSpeedOverCharge.Evaluate(Mathf.Clamp01(normalizedProgress)));

        if (!_isCharging)
        {
            _particleSystem.gameObject.SetActive(true);
            _particleSystem.Play(true);
            _isCharging = true;
        }
    }

    private void ConfigureParticleSystem()
    {
        ParticleSystem.MainModule main = _particleSystem.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = _particleLifetime;
        main.startSize = _particleSize;
        main.startSpeed = 0f;
        main.startColor = ToHdr(_particleColor);

        ParticleSystem.EmissionModule emission = _particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = _emissionRate;

        ParticleSystem.ShapeModule shape = _particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = _spawnRadius;
    }

    private Color ToHdr(Color color)
    {
        return new Color(
            color.r * _glowIntensity,
            color.g * _glowIntensity,
            color.b * _glowIntensity,
            color.a);
    }
}
