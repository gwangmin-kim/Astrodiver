using System;
using UnityEngine;

public sealed class FragmentMagnetManager
{
    private readonly MagnetData _magnetData;
    private ParticleSystem.Particle[] _particles = Array.Empty<ParticleSystem.Particle>();

    public FragmentMagnetManager(MagnetData magnetData)
    {
        _magnetData = magnetData;
    }

    public void Process(
        ParticleSystem particleSystem,
        ResourceDefinition resource,
        Vector3 playerPosition,
        PlayerInventoryController playerInventory)
    {
        EnsureBufferCapacity(particleSystem.main.maxParticles);
        int activeCount = particleSystem.GetParticles(_particles);

        float sqrMagnetRadius = _magnetData.Radius * _magnetData.Radius;
        float sqrCollectRadius = _magnetData.collectRadius * _magnetData.collectRadius;

        for (int i = 0; i < activeCount; i++)
        {
            Vector3 particlePosition = _particles[i].position;
            float sqrDistance = Vector3.SqrMagnitude(particlePosition - playerPosition);

            // 자석 범위 밖의 파티클에는 아무 작업도 하지 않음
            if (sqrDistance > sqrMagnetRadius)
            {
                continue;
            }

            // 자석 범위 내의 파티클은 플레이어 쪽으로 끌어당김
            float distanceFactor = sqrDistance / sqrMagnetRadius;
            float pullSpeed = Mathf.Lerp(
                _magnetData.pullSpeedRange.y,
                _magnetData.pullSpeedRange.x,
                distanceFactor);

            particlePosition = Vector3.MoveTowards(
                particlePosition,
                playerPosition,
                pullSpeed * Time.deltaTime);
            _particles[i].position = particlePosition;

            if (Vector3.SqrMagnitude(particlePosition - playerPosition) > sqrCollectRadius)
            {
                continue;
            }

            // 수집 범위 안의 파티클은 즉시 수집
            if (playerInventory.TryAddResource(resource))
            {
                _particles[i].remainingLifetime = 0f;
            }
        }

        particleSystem.SetParticles(_particles, activeCount);
    }

    private void EnsureBufferCapacity(int requiredCapacity)
    {
        if (_particles.Length >= requiredCapacity)
        {
            return;
        }

        _particles = new ParticleSystem.Particle[requiredCapacity];
    }
}

[Serializable]
public struct MagnetData
{
    [Tooltip("자석 효과가 적용되는 수집 범위")]
    [Min(0.1f)] public float radius;
    [Tooltip("기본 자석 범위에 적용되는 비율 (1 = 100%)")]
    [Min(0f)] public float radiusRatio;

    public float Radius => Mathf.Max(0f, radius * radiusRatio);

    [Tooltip("자석에 이끌리는 속력 범위. X는 최대 거리에서의 속력, Y는 최소 거리에서의 속력. 유도 속력은 거리의 제곱에 반비례")]
    public Vector2 pullSpeedRange;

    [Tooltip("파편을 획득하는 판정 반경")]
    [Range(0.1f, 0.5f)] public float collectRadius;
}
