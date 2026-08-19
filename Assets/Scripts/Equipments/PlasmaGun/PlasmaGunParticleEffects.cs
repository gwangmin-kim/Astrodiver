using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns the pooled muzzle and impact particle effects for one gun.</summary>
public sealed class PlasmaGunParticleEffects : MonoBehaviour
{
    [SerializeField] private ParticleSystem _muzzleParticles;
    [SerializeField] private ParticleSystem _impactParticleTemplate;
    [SerializeField, Min(1)] private int _hitBurstCount = 12;
    [SerializeField, Min(0f)] private float _hitBurstSpeed = 2f;

    private readonly List<ParticleSystem> _impactParticles = new();
    private PlasmaGunVisualPalette _palette;

    public void Initialize(PlasmaGunVisualPalette palette, int maximumTargetCount)
    {
        _palette = palette;
        ApplyPalette(_muzzleParticles);

        for (int i = 0; i < maximumTargetCount; i++)
        {
            ParticleSystem impact = Instantiate(_impactParticleTemplate, transform);
            impact.name = $"Impact Particles {i + 1}";
            ApplyPalette(impact);
            impact.gameObject.SetActive(false);
            _impactParticles.Add(impact);
        }

        _impactParticleTemplate.gameObject.SetActive(false);
    }

    public void SetMuzzleFiring(bool isFiring, Transform muzzle)
    {
        if (_muzzleParticles == null) return;
        if (isFiring)
        {
            if (muzzle == null) return;
            _muzzleParticles.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
            if (!_muzzleParticles.isPlaying) _muzzleParticles.Play(true);
        }
        else if (_muzzleParticles.isPlaying)
        {
            _muzzleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void SetImpactTargets(IReadOnlyList<Transform> targets)
    {
        int activeCount = Mathf.Min(targets.Count, _impactParticles.Count);
        for (int i = 0; i < activeCount; i++)
        {
            ParticleSystem impact = _impactParticles[i];
            Transform target = targets[i];
            if (target == null) { impact.gameObject.SetActive(false); continue; }
            impact.transform.position = target.position;
            if (!impact.gameObject.activeSelf) impact.gameObject.SetActive(true);
            if (!impact.isPlaying) impact.Play(true);
        }
        for (int i = activeCount; i < _impactParticles.Count; i++)
        {
            ParticleSystem impact = _impactParticles[i];
            if (!impact.gameObject.activeSelf) continue;
            impact.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            impact.gameObject.SetActive(false);
        }
    }

    public void EmitImpactBursts(IReadOnlyList<Transform> targets)
    {
        int burstCount = Mathf.Min(targets.Count, _impactParticles.Count);
        for (int i = 0; i < burstCount; i++)
        {
            if (targets[i] == null) continue;

            ParticleSystem impact = _impactParticles[i];
            Vector3 centre = targets[i].position;
            for (int particleIndex = 0; particleIndex < _hitBurstCount; particleIndex++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                ParticleSystem.EmitParams emitParams = new()
                {
                    position = centre,
                    velocity = direction * Random.Range(_hitBurstSpeed * 0.65f, _hitBurstSpeed)
                };
                impact.Emit(emitParams, 1);
            }
        }
    }

    public void HideAll()
    {
        SetMuzzleFiring(false, null);
        SetImpactTargets(System.Array.Empty<Transform>());
    }

    private void ApplyPalette(ParticleSystem particles) => _palette?.ApplyTo(particles);

}
