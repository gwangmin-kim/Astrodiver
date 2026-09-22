using System;
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

    [Header("Charge UI")]
    [SerializeField] private PlasmaGunChargeUI _chargeUI;

    private readonly PlasmaMiningTargeting _targeting = new();
    private readonly List<MiningHit> _currentTargetList = new();
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
    public int TotalAmmo => Mathf.Max(0, _data.ammoCapacity);
    public bool HasAmmo => _remainingAmmo > 0;
    public event Action<int, int> AmmoChanged;

    private void Start()
    {
        _data = GameDataManager.Instance.GetPlasmaGun();
        _remainingAmmo = TotalAmmo;

        CreateChainLaserVisuals();

        if (_visualPalette != null) _visualPalette.ApplyTo(_laserVisual);
        if (_chargeParticles != null) _chargeParticles.ApplyPalette(_visualPalette);
        if (_particleEffects != null) _particleEffects.Initialize(_visualPalette, _data.chainCount + 1);

        PublishAmmoChanged();
    }

    private void Update()
    {
        if (_chargeState != ChargeState.Charged || !isAttacking || !HasAmmo)
        {
            HideAttackEffects();
            if (_particleEffects != null) _particleEffects.HideAll();
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
                    // 타격 대상 설정
                    SetTarget();

                    // 시각 효과 설정
                    DrawAttackEffect();
                    if (_particleEffects != null)
                    {
                        _particleEffects.SetMuzzleFiring(true, _shootOrigin);
                        _particleEffects.SetImpactTargets(_currentTargetList);
                    }

                    _chargedRetentionTimer = _data.chargedRetentionTime;
                    _attackTickTimer -= Time.deltaTime * _data.TickSpeedMultiplier;

                    if (_attackTickTimer < 0f)
                    {
                        _attackTickTimer = _data.tickInterval;
                        _remainingAmmo = Mathf.Max(0, _remainingAmmo - 1);
                        PublishAmmoChanged();
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

        UpdateChargeVisuals();
    }

    private void UpdateChargeVisuals()
    {
        bool isCharging = _chargeState == ChargeState.Charging && isAttacking && HasAmmo;
        float progress = _data.ChargeTime <= Mathf.Epsilon
            ? 1f
            : 1f - Mathf.Clamp01(_chargeTimer / _data.ChargeTime);

        if (_chargeParticles != null) _chargeParticles.SetCharging(isCharging, progress);
        if (_chargeUI != null) _chargeUI.SetCharging(isCharging, progress);
    }

    private void PublishAmmoChanged()
    {
        AmmoChanged?.Invoke(RemainingAmmo, TotalAmmo);
    }

    private void ResolveAttack()
    {
        AttackTarget();
        if (_particleEffects != null)
        {
            _particleEffects.SetImpactTargets(_currentTargetList);
            _particleEffects.EmitImpactBursts(_currentTargetList);
        }
        DrawAttackEffect();
    }

    /// <summary>
    /// 공격 대상을 탐색
    /// </summary>
    private void SetTarget()
    {
        _currentTargetList.Clear();
        if (_shootOrigin == null || !_targeting.TryFindFirstTarget(
                _shootOrigin.position, _shootOrigin.up, _initialCastRadius,
                _data.AttackRange, _targetLayer, out MiningHit first)) return;

        _currentTargetList.Add(first);
        for (int i = 0; i < _data.chainCount; i++)
        {
            if (!_targeting.TryFindNextMiningTarget(_currentTargetList[^1],
                    _data.ChainDetectRange, _targetLayer, _currentTargetList, out MiningHit next)) break;
            _currentTargetList.Add(next);
        }
    }

    private void AttackTarget()
    {
        // The full chain is selected before damage changes any cell/collider.
        for (int i = 0; i < _currentTargetList.Count; i++)
        {
            MiningHit target = _currentTargetList[i];
            if (target.Map == null) continue;
            float damageRate = Mathf.Pow(_data.ChainedDamageRate, i);
            int currentDamage = Mathf.RoundToInt(_data.tickDamage * damageRate);
            target.Map.TryApplyMiningDamage(target.Cell, currentDamage, out _);
        }
    }
    private void DrawAttackEffect()
    {
        if (_laserVisual == null || _shootOrigin == null) return;

        Vector2 origin = _shootOrigin.position;
        Vector2 fallbackEnd = origin + (Vector2)_shootOrigin.up * _data.AttackRange;
        Vector2 firstEnd = _currentTargetList.Count > 0
            ? _currentTargetList[0].Position
            : fallbackEnd;
        _laserVisual.Show(origin, firstEnd);

        int chainSegmentCount = Mathf.Min(
            _chainLaserVisuals.Count,
            Mathf.Max(0, _currentTargetList.Count - 1));
        for (int i = 0; i < chainSegmentCount; i++)
        {
            _chainLaserVisuals[i].Show(
                _currentTargetList[i].Position, _currentTargetList[i + 1].Position);
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
            if (_visualPalette != null) _visualPalette.ApplyTo(chainLaser);
            chainLaser.Hide();
            _chainLaserVisuals.Add(chainLaser);
        }
    }

    private void HideAttackEffects()
    {
        if (_laserVisual != null) _laserVisual.Hide();
        for (int i = 0; i < _chainLaserVisuals.Count; i++)
        {
            _chainLaserVisuals[i].Hide();
        }
    }

    private void OnDisable()
    {
        _currentTargetList.Clear();
        HideAttackEffects();
        if (_particleEffects != null) _particleEffects.HideAll();
    }
}

[Serializable]
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
