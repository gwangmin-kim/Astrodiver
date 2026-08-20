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
    [SerializeField]
    private AnimationCurve _chargeProgressCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float _initialConvergeSpeed = 0.4f;
    [SerializeField, Min(0f)] private float _finalConvergeSpeed = 3.5f;
    [SerializeField, Min(0f)] private float _initialSizeMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float _finalSizeMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float _initialSpawnRadiusMultiplier = 1f;
    [SerializeField, Min(0f)] private float _finalSpawnRadiusMultiplier = 1.5f;

    private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[64];
    private bool _isCharging;
    private float _convergeSpeed;
    private float _baseStartSizeMultiplier;
    private float _baseSpawnRadius;

    private void Awake()
    {
        if (_particleSystem == null)
        {
            Debug.LogWarning("Plasma charge particles need a preconfigured ParticleSystem.", this);
            enabled = false;
            return;
        }

        _baseStartSizeMultiplier = _particleSystem.main.startSizeMultiplier;
        _baseSpawnRadius = _particleSystem.shape.radius;
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

        float chargeProgress = _chargeProgressCurve.Evaluate(Mathf.Clamp01(normalizedProgress));
        _convergeSpeed = Mathf.Lerp(
            _initialConvergeSpeed,
            _finalConvergeSpeed,
            chargeProgress);
        ParticleSystem.MainModule main = _particleSystem.main;
        main.startSizeMultiplier = _baseStartSizeMultiplier * Mathf.Lerp(
            _initialSizeMultiplier, _finalSizeMultiplier, chargeProgress);
        ParticleSystem.ShapeModule shape = _particleSystem.shape;
        shape.radius = _baseSpawnRadius * Mathf.Lerp(
            _initialSpawnRadiusMultiplier, _finalSpawnRadiusMultiplier, chargeProgress);

        if (!_isCharging)
        {
            _particleSystem.gameObject.SetActive(true);
            _particleSystem.Play(true);
            _isCharging = true;
        }
    }

    public void ApplyPalette(PlasmaGunVisualPalette palette)
    {
        if (palette != null) palette.ApplyTo(_particleSystem);
    }
}
