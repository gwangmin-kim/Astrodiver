using System.Collections.Generic;
using UnityEngine;

public class PlasmaGunController : MonoBehaviour
{
    private PlasmaGunData _data;

    [Header("Detect Settings")]
    [Tooltip("플라즈마 광선 시작점")]
    [SerializeField] private Transform _shootOrigin;
    [SerializeField] private PlasmaGunLaserVisual _laserVisual;
    [SerializeField] private PlasmaGunChargeParticles _chargeParticles;
    [SerializeField] private PlasmaGunVisualPalette _visualPalette;
    [SerializeField] private PlasmaGunParticleEffects _particleEffects;
    [SerializeField] private LayerMask _targetLayer;
    [Tooltip("첫 목표 탐색 시 수행하는 CircleCast의 반지름")]
    [SerializeField, Min(0.01f)] private float _initialCastRadius = 0.05f;

    private ContactFilter2D _targetFilter;
    private readonly List<Collider2D> _overlapBuffer = new(10);

    // 중복 검색 방지
    // 공격 대상의 순서가 필요한 로직이 존재
    // 크기가 작은 컨테이너이므로 List 사용
    private readonly List<Transform> _currentTargetList = new();
    private readonly List<PlasmaGunLaserVisual> _chainLaserVisuals = new();

    private float _attackTickTimer;
    private float _chargeTimer;
    private float _chargedRetentionTimer;
    private int _remainingAmmo;

    // 상태 머신
    private enum ChargeState
    {
        Uncharged,
        Charging,
        Charged
    }
    public bool isAttacking; // 외부 제어 상태
    [SerializeField] private ChargeState _chargeState = ChargeState.Uncharged; // 내부 제어 상태
    public bool IsSwitchable => !isAttacking;
    public int RemainingAmmo => _remainingAmmo;
    public bool HasAmmo => _remainingAmmo > 0;

    private void Awake()
    {
        // 대상 탐색용 필터 초기화
        _targetFilter = new ContactFilter2D();
        _targetFilter.SetLayerMask(_targetLayer);
        _targetFilter.useTriggers = false;
    }

    private void Start()
    {
        _data = GameDataManager.Instance.GetPlasmaGun();
        _remainingAmmo = Mathf.Max(0, _data.ammoCapacity);
        CreateChainLaserVisuals();
        _visualPalette?.ApplyTo(_laserVisual);
        _chargeParticles?.ApplyPalette(_visualPalette);
        _particleEffects?.Initialize(_visualPalette, _data.chainCount + 1);
    }

    private void Update()
    {
        if (_chargeState != ChargeState.Charged || !isAttacking || !HasAmmo)
        {
            HideAttackEffects();
            _particleEffects?.HideAll();
        }

        switch (_chargeState)
        {
            case ChargeState.Uncharged:
                if (isAttacking && HasAmmo)
                {
                    _chargeTimer = _data.ChargeTime;
                    _chargeState = ChargeState.Charging;
                }
                break;

            case ChargeState.Charging:
                if (isAttacking && HasAmmo)
                {
                    _chargeTimer -= Time.deltaTime * _data.ChargeSpeedMultiplier;
                    if (_chargeTimer < 0f)
                    {
                        _chargedRetentionTimer = _data.chargedRetentionTime;
                        _attackTickTimer = 0f;
                        _chargeState = ChargeState.Charged;
                    }
                }
                else _chargeState = ChargeState.Uncharged;
                break;

            case ChargeState.Charged:
                if (isAttacking && HasAmmo)
                {
                    DrawAttackEffect();
                    _particleEffects?.SetMuzzleFiring(true, _shootOrigin);
                    _particleEffects?.SetImpactTargets(_currentTargetList);
                    _chargedRetentionTimer = _data.chargedRetentionTime;
                    _attackTickTimer -= Time.deltaTime * _data.TickSpeedMultiplier;

                    if (_attackTickTimer < 0f)
                    {
                        _attackTickTimer = _data.tickInterval;
                        _remainingAmmo--;
                        ResolveAttack();

                        if (!HasAmmo)
                        {
                            isAttacking = false;
                            _chargeState = ChargeState.Uncharged;
                        }
                    }
                }
                else
                {
                    _chargedRetentionTimer -= Time.deltaTime;
                    if (_chargedRetentionTimer < 0f)
                    {
                        _chargeState = ChargeState.Uncharged;
                    }
                }
                break;
        }

        UpdateChargeParticles();
    }

    private void UpdateChargeParticles()
    {
        if (_chargeParticles == null) return;

        bool isCharging = _chargeState == ChargeState.Charging && isAttacking && HasAmmo;
        float progress = _data.ChargeTime <= Mathf.Epsilon
            ? 1f
            : 1f - Mathf.Clamp01(_chargeTimer / _data.ChargeTime);
        _chargeParticles.SetCharging(isCharging, progress);
    }

    private void ResolveAttack()
    {
        SetTarget();
        AttackTarget();
        _particleEffects?.SetImpactTargets(_currentTargetList);
        _particleEffects?.EmitImpactBursts(_currentTargetList);
        DrawAttackEffect();
    }

    /// <summary>
    /// 공격 대상을 탐색
    /// </summary>
    private void SetTarget()
    {
        // 집합 초기화
        _currentTargetList.Clear();

        // 첫 번째 대상은 CircleCast로 직선 검색
        RaycastHit2D hit = Physics2D.CircleCast(
            _shootOrigin.position,
            _initialCastRadius,
            _shootOrigin.up,
            _data.AttackRange,
            _targetLayer);

        // 아무것도 감지가 안됐다면 그대로 종료
        if (!hit)
        {
            return;
        }

        _currentTargetList.Add(hit.transform);

        // 연쇄 공격이 해금되었다면, 추가 타격 대상 검색
        if (_data.chainCount <= 0) return;

        Vector2 chainOrigin = hit.transform.position;
        for (int i = 0; i < _data.chainCount; i++)
        {
            Transform target = GetNearestTarget(chainOrigin, _data.ChainDetectRange);
            if (target == null) break;

            _currentTargetList.Add(target);
            chainOrigin = target.position;
        }
    }

    /// <summary>
    /// 탐색한 공격 대상에게 실제 피해를 입힘
    /// </summary>
    private void AttackTarget()
    {
        for (int i = 0; i < _currentTargetList.Count; i++)
        {
            Transform target = _currentTargetList[i];

            if (target == null || !target.TryGetComponent<IDamagable>(out var damagable)) continue;

            float damageRate = Mathf.Pow(_data.ChainedDamageRate, i);
            int currentDamage = Mathf.RoundToInt(_data.tickDamage * damageRate);

            AttackData attackData = new()
            {
                damage = currentDamage,
                source = DamageSource.Player
            };

            damagable.ApplyDamage(attackData);
        }
    }

    private void DrawAttackEffect()
    {
        if (_laserVisual == null || _shootOrigin == null) return;

        Vector2 origin = _shootOrigin.position;
        Vector2 fallbackEnd = origin + (Vector2)_shootOrigin.up * _data.AttackRange;
        Vector2 firstEnd = _currentTargetList.Count > 0 && _currentTargetList[0] != null
            ? _currentTargetList[0].position
            : fallbackEnd;
        _laserVisual.Show(origin, firstEnd);

        int chainSegmentCount = Mathf.Min(
            _chainLaserVisuals.Count,
            Mathf.Max(0, _currentTargetList.Count - 1));
        for (int i = 0; i < chainSegmentCount; i++)
        {
            Transform segmentStart = _currentTargetList[i];
            Transform segmentEnd = _currentTargetList[i + 1];
            if (segmentStart != null && segmentEnd != null)
            {
                _chainLaserVisuals[i].Show(segmentStart.position, segmentEnd.position);
            }
            else
            {
                _chainLaserVisuals[i].Hide();
            }
        }

        for (int i = chainSegmentCount; i < _chainLaserVisuals.Count; i++)
        {
            _chainLaserVisuals[i].Hide();
        }
    }

    private void CreateChainLaserVisuals()
    {
        if (_laserVisual == null)
        {
            Debug.LogWarning("Plasma gun needs its preconfigured direct-fire laser visual.", this);
            return;
        }

        int chainCount = Mathf.Max(0, _data.chainCount);
        for (int i = 0; i < chainCount; i++)
        {
            PlasmaGunLaserVisual chainLaser = Instantiate(_laserVisual, transform);
            chainLaser.name = $"Chain Laser {i + 1}";
            _visualPalette?.ApplyTo(chainLaser);
            chainLaser.Hide();
            _chainLaserVisuals.Add(chainLaser);
        }
    }

    private void HideAttackEffects()
    {
        _laserVisual?.Hide();
        for (int i = 0; i < _chainLaserVisuals.Count; i++)
        {
            _chainLaserVisuals[i].Hide();
        }
    }

    private Transform GetNearestTarget(Vector2 point, float radius)
    {
        float minSqrDistance = Mathf.Infinity;
        Transform nearestTarget = null;

        int count = Physics2D.OverlapCircle(
            point, radius, _targetFilter, _overlapBuffer);

        for (int i = 0; i < count; i++)
        {
            Transform candidate = _overlapBuffer[i].transform;

            if (_currentTargetList.Contains(candidate))
            {
                continue;
            }

            float sqrDistance = Vector2.SqrMagnitude((Vector2)candidate.position - point);

            if (minSqrDistance > sqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearestTarget = candidate;
            }
        }

        return nearestTarget;
    }

}

[System.Serializable]
public struct PlasmaGunData
{
    [Header("Ammo Settings")]
    [Tooltip("Maximum number of attack ticks available during one exploration session.")]
    [Min(0)] public int ammoCapacity;

    [Header("Charge Settings")]
    [Tooltip("최초 발사 시까지 필요한 충전 시간")]
    [Min(0f)] public float baseChargeTime;
    [Tooltip("충전 타이머가 흐르는 속도 배율 (1 = 기본 속도)")]
    [Min(0f)] public float chargeSpeedMultiplier;
    [Tooltip("충전 상태가 유지되는 시간")]
    [Min(0f)] public float chargedRetentionTime;

    [Header("Attack Settings")]
    [Tooltip("매 틱 당 입히는 피해량")]
    [Min(0)] public int tickDamage;
    [Tooltip("공격 키 홀드 시 타격 수행 간격")]
    [Range(0.1f, 1f)] public float tickInterval;
    [Tooltip("Attack tick timer speed multiplier (1 = base speed)")]
    [Min(0f)] public float tickSpeedMultiplier;
    [Tooltip("최초 목표 탐지 거리 (CircleCast 거리)")]
    [Min(0.1f)] public float baseAttackRange;
    [Min(0f)] public float attackRangeRatio;

    [Header("Chaining Settings")]
    [Tooltip("첫 타격 이후 연쇄 가능한 최대 횟수")]
    [Min(0)] public int chainCount;
    [Tooltip("매 연쇄 당 변화되는 피해량 비율: 초기엔 감소하지만, 후반엔 오히려 증가하도록 설계")]
    [Min(0.4f)] public float chainedDamageRate;
    [Min(0f)] public float chainedDamageRateRatio;
    [Tooltip("연쇄 대상 탐색 거리")]
    [Min(0.1f)] public float baseChainRange;
    [Min(0f)] public float chainRangeRatio;

    public float ChargeTime => Mathf.Max(0f, baseChargeTime);
    public float ChargeSpeedMultiplier => Mathf.Max(0f, chargeSpeedMultiplier);
    public float TickSpeedMultiplier => Mathf.Max(0f, tickSpeedMultiplier);
    public float AttackRange => Mathf.Max(0f, baseAttackRange * attackRangeRatio);
    public float ChainedDamageRate =>
        Mathf.Max(0f, chainedDamageRate * chainedDamageRateRatio);
    public float ChainDetectRange =>
        Mathf.Max(0f, baseChainRange * chainRangeRatio);
}
