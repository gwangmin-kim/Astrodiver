using System;
using UnityEngine;

public enum FloatageLifecycleState
{
    Spawning,
    Active,
    Destroyed
}
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]

[RequireComponent(typeof(FloatageAnimationController))]

public class FloatageController : MonoBehaviour, IDamagable
{
    [SerializeField] private FloatageDefinition _definition;
    [SerializeField, Min(1)] private int _maxHp = 100;
    [SerializeField, Min(0f)] private float _dropRadius;
    [SerializeField, Min(1)] private int _dropCount = 1;

    private int _hp;
    private Collider2D[] _colliders;
    private FloatageAnimationController _animationController;
    private FloatageLifecycleState _lifecycleState;

    public FloatageDefinition Definition => _definition;
    public FloatageLifecycleState LifecycleState => _lifecycleState;

    // events
    public event Action Spawned;
    public event Action Activated;
    public event Action Damaged;

    private void Awake()
    {
        if (_definition == null)
        {
            Debug.LogError($"{nameof(FloatageController)} requires a floatage definition.", this);
            enabled = false;
            return;
        }

        _hp = Mathf.Max(1, _maxHp);
        _colliders = GetComponentsInChildren<Collider2D>(true);
        _animationController = GetComponent<FloatageAnimationController>();
    }

    private void OnEnable()
    {
        if (_animationController != null)
        {
            _animationController.RespawnPresentationCompleted +=
                HandleRespawnPresentationCompleted;
        }
    }

    private void OnDisable()
    {
        if (_animationController != null)
        {
            _animationController.RespawnPresentationCompleted -=
                HandleRespawnPresentationCompleted;
        }
    }

    private void Start()
    {
        BeginSpawn();
    }

    /// <summary>
    /// Starts a new spawn cycle. Gameplay collision stays disabled until the
    /// visual presentation explicitly reports completion.
    /// </summary>
    public void BeginSpawn()
    {
        if (_lifecycleState == FloatageLifecycleState.Destroyed)
        {
            return;
        }

        _lifecycleState = FloatageLifecycleState.Spawning;
        SetCollidersEnabled(false);
        Spawned?.Invoke();

        if (_animationController == null)
        {
            HandleRespawnPresentationCompleted();
        }
    }

    private void HandleRespawnPresentationCompleted()
    {
        if (_lifecycleState != FloatageLifecycleState.Spawning)
        {
            return;
        }

        _lifecycleState = FloatageLifecycleState.Active;
        SetCollidersEnabled(true);
        Activated?.Invoke();
    }

    public void ApplyDamage(AttackData data)
    {
        if (_definition == null ||
            _lifecycleState != FloatageLifecycleState.Active)
        {
            return;
        }

        _hp -= data.damage;
        Damaged?.Invoke();
        if (_hp <= 0)
        {
            ResolveDestroy();
        }
    }

    private void ResolveDestroy()
    {
        _lifecycleState = FloatageLifecycleState.Destroyed;

        if (FragmentParticleManager.Instance != null)
        {
            FragmentParticleManager.Instance.DropFragment(
                transform.position,
                _definition.DropResource,
                _dropRadius,
                _dropCount);
        }

        GetComponent<StageSpawnedObject>()?.NotifyRemovedFromStage();
        Destroy(gameObject);
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (_colliders == null)
        {
            return;
        }

        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null)
            {
                _colliders[i].enabled = isEnabled;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_definition == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _dropRadius);
    }
#endif
}
