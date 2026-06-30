using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FloatageController : MonoBehaviour, IDamagable
{
    [Header("Durability Settings")]
    [SerializeField] private float _hp;

    [Header("Drop Settings")]
    [SerializeField] private FragmentDropData _dropData;

    public void ApplyDamage(AttackData data)
    {
        _hp -= data.damage;
        if (_hp < 0f)
        {
            ResolveDestroy();
        }
    }

    private void ResolveDestroy()
    {
        if (FragmentParticleManager.Instance != null)
        {
            FragmentParticleManager.Instance.DropFragment(transform.position, _dropData);
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _dropData.radius);
    }
#endif
}

[System.Serializable]
public struct FragmentDropData
{
    [Tooltip("파티클 생성 범위 반지름")]
    [Min(0f)] public float radius;

    [Tooltip("파티클 생성 개수")]
    public short count;

    [Tooltip("파티클 유지 시간")]
    [Min(1f)] public float lifetime;
}
