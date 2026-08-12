using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FloatageController : MonoBehaviour, IDamagable
{
    [SerializeField] private FloatageDefinition _definition;

    private int _hp;

    public FloatageDefinition Definition => _definition;

    private void Awake()
    {
        if (_definition == null)
        {
            Debug.LogError($"{nameof(FloatageController)} requires a floatage definition.", this);
            enabled = false;
            return;
        }

        _hp = _definition.Hp;
    }

    public void ApplyDamage(AttackData data)
    {
        if (_definition == null)
        {
            return;
        }

        _hp -= data.damage;
        if (_hp <= 0)
        {
            ResolveDestroy();
        }
    }

    private void ResolveDestroy()
    {
        if (FragmentParticleManager.Instance != null)
        {
            FragmentParticleManager.Instance.DropFragment(
                transform.position,
                _definition.DropData);
        }

        GetComponent<StageSpawnedObject>()?.NotifyRemovedFromStage();
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_definition == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _definition.DropData.radius);
    }
#endif
}
