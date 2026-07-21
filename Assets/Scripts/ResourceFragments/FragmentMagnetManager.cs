using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class FragmentMagnetManager : MonoBehaviour
{
    [Header("Magnet Settings")]
    [SerializeField] private MagnetData _magnetData;

    private ParticleSystem _particleSystem;
    private ParticleSystem.Particle[] _particles;

    private PlayerInventoryController _playerInventory;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        _particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
        _magnetData = GameDataManager.Instance.GetOrInitializeMagnet(_magnetData);
    }

    private void Start()
    {
        _playerInventory = PlayerInventoryController.Instance;
    }

    private void Update()
    {
        if (_playerInventory == null || PlayerContext.Instance == null) return;

        int activeCount = _particleSystem.GetParticles(_particles);

        Vector3 playerPosition = PlayerContext.Instance.transform.position;

        float sqrMagnetRadius = _magnetData.radius * _magnetData.radius;
        float sqrCollectRadius = _magnetData.collectRadius * _magnetData.collectRadius;

        for (int i = 0; i < activeCount; i++)
        {
            Vector3 particlePosition = _particles[i].position;
            float sqrDistance = Vector3.SqrMagnitude(particlePosition - playerPosition);

            // 자석 범위 밖의 파티클에는 아무 작업도 하지 않음
            if (sqrDistance > sqrMagnetRadius) continue;

            // 자석 범위 내의 파티클은 플레이어 쪽으로 끌어당김
            float distanceFactor = sqrDistance / sqrMagnetRadius;
            float pullSpeed = Mathf.Lerp(_magnetData.pullSpeedRange.y, _magnetData.pullSpeedRange.x, distanceFactor);
            _particles[i].position = Vector3.MoveTowards(particlePosition, playerPosition, pullSpeed * Time.deltaTime);

            // 수집 범위 안의 파티클은 즉시 수집
            if (sqrDistance < sqrCollectRadius)
            {
                FragmentResourceData data = new()
                {
                    definition = FragmentParticleManager.Instance != null
                        ? FragmentParticleManager.Instance.Resource
                        : null,
                    amount = 1
                };
                _playerInventory.CollectResourceFragment(data);
                _particles[i].remainingLifetime = 0f;
            }
        }

        _particleSystem.SetParticles(_particles, activeCount);
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
}

[System.Serializable]
public struct MagnetData
{
    [Tooltip("자석 효과가 적용되는 수집 범위")]
    [Min(0.1f)] public float radius;

    [Tooltip("자석에 이끌리는 속력 범위. 유도 속력은 거리의 제곱에 반비례")]
    public Vector2 pullSpeedRange;

    [Tooltip("파편을 획득하는 판정 반경")]
    [Range(0.1f, 0.5f)] public float collectRadius;
}
