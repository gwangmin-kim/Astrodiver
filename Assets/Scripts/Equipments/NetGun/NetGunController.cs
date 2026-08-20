using System;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

public class NetGunController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private PlayerInventoryController _playerInventory;

    private NetGunData _data;

    [Header("Net Gun Settings")]
    [SerializeField] private NetCaptureController _netPrefab;

    [Header("Capture Settings")]
    [SerializeField] private Transform _shootOrigin;
    [SerializeField] private float _collectRadiusThreshold;

    [Header("Inertia Settings")]
    [SerializeField] private Ease _shootEase;
    [SerializeField][Range(0f, 1f)] private float _dampingTime;

    private readonly List<NetRuntime> _netRuntimeList = new();
    private readonly List<ICapturable> _collectedTargetsBuffer = new();
    private readonly NetMovementManager _movementManager = new();
    private float _chargingTime;
    private NetRuntime _chargingNet;
    private NetRuntime _retractingNet;
    private int _remainingAmmo;

    private enum NetGunState
    {
        Idle,
        Charging,
        Retracting
    }

    [SerializeField] private NetGunState _netGunState = NetGunState.Idle;

    public bool IsUnlocked
    {
        get
        {
            GameDataManager manager = GameDataManager.Instance;
            return manager?.RuntimeData != null
                ? manager.RuntimeData.Equipment.netGun.isUnlocked
                : _data.isUnlocked;
        }
    }

    public bool IsSwitchable => _netGunState == NetGunState.Idle;
    public int RemainingAmmo => _remainingAmmo;
    public int TotalAmmo => Mathf.Max(0, _data.ammoCapacity);
    public bool HasAmmo => _remainingAmmo > 0;
    public event Action<int, int> AmmoChanged;

    private void Start()
    {
        _data = GameDataManager.Instance.GetNetGun();
        BuildNetPool();
        _remainingAmmo = TotalAmmo;

        _playerInventory = PlayerInventoryController.Instance;
        ResetAllNetsToIdle();
        PublishAmmoChanged();
    }

    private void OnDisable()
    {
        for (int i = 0; i < _netRuntimeList.Count; i++)
        {
            _movementManager.Reset(_netRuntimeList[i].net);
            _netRuntimeList[i].shootTween.Stop();
            _netRuntimeList[i].net.ResetFolded();
        }
    }

    public bool OnPressCapture()
    {
        if (!IsUnlocked) return false;

        switch (_netGunState)
        {
            case NetGunState.Idle:
                return TryHandleIdleCapturePress();

            default:
                return false;
        }
    }

    public bool OnReleaseCapture()
    {
        switch (_netGunState)
        {
            case NetGunState.Charging:
                StartShooting();
                return true;

            case NetGunState.Retracting:
                CancelRetracting();
                return true;

            default:
                return false;
        }
    }

    private void Update()
    {
        switch (_netGunState)
        {
            case NetGunState.Charging:
                _chargingTime += Time.deltaTime;
                break;

            case NetGunState.Retracting:
                UpdateRetracting(Time.deltaTime);
                break;
        }

        _movementManager.Update(Time.deltaTime, _dampingTime);
    }

    private void BuildNetPool()
    {
        _netRuntimeList.Clear();
        if (_netPrefab == null) return;

        int maxNetCount = Mathf.Max(1, _data.netCount);

        for (int i = 0; i < maxNetCount; i++)
        {
            NetCaptureController net = Instantiate(_netPrefab, transform);
            net.name = $"{_netPrefab.name} {i + 1:00}";
            RegisterNet(net);
        }
    }

    private void RegisterNet(NetCaptureController net)
    {
        NetRuntime runtime = new()
        {
            net = net
        };

        net.Initialize(_data.netData);
        net.FoldCompleted += () => HandleNetFoldCompleted(runtime);
        _netRuntimeList.Add(runtime);
        _movementManager.Register(net);
    }

    private bool TryHandleIdleCapturePress()
    {
        if (!HasAmmo)
        {
            NetRuntime recallableNet = FindAimedRecallableNet()
                ?? FindClosestRecallableNet();
            return TryStartRetracting(recallableNet);
        }

        if (!HasAvailableNet())
        {
            return TryStartRetracting(FindClosestRecallableNet());
        }

        NetRuntime aimedNet = FindAimedRecallableNet();
        if (aimedNet != null)
        {
            return TryStartRetracting(aimedNet);
        }

        NetRuntime availableNet = FindAvailableNet();
        if (availableNet == null) return false;

        StartCharging(availableNet);
        return true;
    }

    private void StartCharging(NetRuntime net)
    {
        _netGunState = NetGunState.Charging;
        _chargingNet = net;
        _chargingTime = 0f;
    }

    private void StartShooting()
    {
        if (_chargingNet == null)
        {
            _netGunState = NetGunState.Idle;
            return;
        }

        NetRuntime net = _chargingNet;
        _chargingNet = null;
        _netGunState = NetGunState.Idle;
        _remainingAmmo = Mathf.Max(0, _remainingAmmo - 1);
        PublishAmmoChanged();

        net.shootTween.Stop();

        float chargeRatio = (_data.ChargeTime <= 0f)
            ? 1f
            : Mathf.Clamp01(_chargingTime / _data.ChargeTime);
        float minShootRange = 0.1f;
        float shootDistance = Mathf.Lerp(minShootRange, _data.MaxShootRange, chargeRatio);

        Vector2 startPosition = _shootOrigin.position;
        Vector2 shootDirection = _shootOrigin.up;
        Vector2 endPosition = startPosition + shootDirection * shootDistance;

        net.net.transform.SetPositionAndRotation(startPosition, _shootOrigin.rotation);
        net.net.transform.SetParent(null, true);
        net.net.ResetFolded();
        net.net.PrepareForLaunch();
        net.net.gameObject.SetActive(true);

        float shootSpeed = Mathf.Max(0.01f, _data.ShootSpeed);
        float shootDuration = Mathf.Max(0.01f, shootDistance / shootSpeed);

        net.shootTween = Tween.Position(
                net.net.transform,
                endValue: endPosition,
                duration: shootDuration,
                ease: _shootEase)
            .OnComplete(() => StartSpreading(net));
    }

    private void StartSpreading(NetRuntime net)
    {
        net.net.BeginSpread();
    }

    private void PublishAmmoChanged()
    {
        AmmoChanged?.Invoke(RemainingAmmo, TotalAmmo);
    }

    private bool TryStartRetracting(NetRuntime net)
    {
        if (net == null || !net.net.IsRecallable) return false;

        net.shootTween.Stop();
        _movementManager.Reset(net.net);
        _retractingNet = net;
        _netGunState = NetGunState.Retracting;
        return true;
    }

    private void UpdateRetracting(float deltaTime)
    {
        if (_retractingNet == null)
        {
            _netGunState = NetGunState.Idle;
            return;
        }

        if (!_retractingNet.net.IsRecallable)
        {
            return;
        }

        if (_shootOrigin == null)
        {
            StartFold(_retractingNet);
            return;
        }

        Vector2 diff = (Vector2)_shootOrigin.position - (Vector2)_retractingNet.net.transform.position;
        float collectRadius = Mathf.Max(0f, _collectRadiusThreshold);
        if (diff.sqrMagnitude <= collectRadius * collectRadius)
        {
            StartFold(_retractingNet);
            return;
        }

        Vector2 targetVelocity = _data.CollectSpeed * diff.normalized;
        _movementManager.SetTargetVelocity(_retractingNet.net, targetVelocity);
    }

    private void StartFold(NetRuntime net)
    {
        net.shootTween.Stop();
        _movementManager.Reset(net.net);
        net.net.BeginFold(CaptureReleaseReason.Collected);
    }

    private void CancelRetracting()
    {
        // Once folding starts, collection is committed. Clearing _retractingNet here
        // would orphan the FoldCompleted callback and leave an active folded net
        // outside the pool.
        if (_retractingNet != null
            && (_retractingNet.net.IsFolding || _retractingNet.net.IsFolded))
        {
            return;
        }

        if (_retractingNet != null)
        {
            _movementManager.SetTargetVelocity(_retractingNet.net, Vector2.zero);
        }

        _retractingNet = null;
        _netGunState = NetGunState.Idle;
    }

    private void StartFoldedNetRecall(NetRuntime net)
    {
        if (net == null)
        {
            _retractingNet = null;
            _netGunState = NetGunState.Idle;
            return;
        }

        net.shootTween.Stop();
        _movementManager.Reset(net.net);
        CollectCapturedTargets(net.net);

        if (_shootOrigin == null)
        {
            CompleteRetracting(net);
            return;
        }

        float recallDistance = Vector2.Distance(net.net.transform.position, _shootOrigin.position);
        float recallSpeed = Mathf.Max(0.01f, _data.CollectSpeed);
        float recallDuration = Mathf.Max(0.01f, recallDistance / recallSpeed);

        net.shootTween = Tween.Position(
                net.net.transform,
                endValue: _shootOrigin.position,
                duration: recallDuration,
                ease: _shootEase)
            .OnComplete(() => CompleteRetracting(net));
    }

    private void CompleteRetracting(NetRuntime net)
    {
        ResetNetToPool(net);

        if (net == _retractingNet)
        {
            _retractingNet = null;
            _netGunState = NetGunState.Idle;
        }
    }

    private void ResetNetToPool(NetRuntime net)
    {
        net.shootTween.Stop();
        _movementManager.Reset(net.net);
        net.net.transform.SetParent(transform, false);
        net.net.ResetFolded();
        net.net.gameObject.SetActive(false);
    }

    private void ResetAllNetsToIdle()
    {
        for (int i = 0; i < _netRuntimeList.Count; i++)
        {
            ResetNetToPool(_netRuntimeList[i]);
        }

        _chargingNet = null;
        _retractingNet = null;
        _netGunState = NetGunState.Idle;
    }

    private bool HasAvailableNet()
    {
        return FindAvailableNet() != null;
    }

    private NetRuntime FindAvailableNet()
    {
        for (int i = 0; i < _netRuntimeList.Count; i++)
        {
            if (_netRuntimeList[i].net.IsAvailable)
            {
                return _netRuntimeList[i];
            }
        }

        return null;
    }

    private NetRuntime FindClosestRecallableNet()
    {
        NetRuntime closestNet = null;
        float closestSqrDistance = Mathf.Infinity;
        Vector2 origin = _shootOrigin.position;

        for (int i = 0; i < _netRuntimeList.Count; i++)
        {
            NetRuntime net = _netRuntimeList[i];
            if (!net.net.IsRecallable) continue;

            float sqrDistance = ((Vector2)net.net.transform.position - origin).sqrMagnitude;
            if (sqrDistance >= closestSqrDistance) continue;

            closestSqrDistance = sqrDistance;
            closestNet = net;
        }

        return closestNet;
    }

    private NetRuntime FindAimedRecallableNet()
    {
        NetRuntime aimedNet = null;
        float closestProjection = Mathf.Infinity;
        Vector2 origin = _shootOrigin.position;
        Vector2 direction = _shootOrigin.up;
        float radiusSqr = _data.netData.Radius * _data.netData.Radius;

        for (int i = 0; i < _netRuntimeList.Count; i++)
        {
            NetRuntime net = _netRuntimeList[i];
            if (!net.net.IsRecallable) continue;

            Vector2 toNet = (Vector2)net.net.transform.position - origin;
            float projection = Vector2.Dot(toNet, direction);
            if (projection <= 0f && toNet.sqrMagnitude > radiusSqr) continue;
            if (projection > _data.MaxShootRange) continue;

            float perpendicularSqr = Mathf.Max(0f, toNet.sqrMagnitude - projection * projection);
            if (perpendicularSqr > radiusSqr) continue;
            if (projection >= closestProjection) continue;

            closestProjection = projection;
            aimedNet = net;
        }

        return aimedNet;
    }

    private void HandleNetFoldCompleted(NetRuntime net)
    {
        if (net != _retractingNet)
        {
            // Defensive recovery for a fold that completed after its recall was
            // interrupted by another lifecycle or input event.
            if (net != null && net.net != null && net.net.gameObject.activeSelf)
            {
                ResetNetToPool(net);
            }

            return;
        }

        StartFoldedNetRecall(net);
    }

    private void CollectCapturedTargets(NetCaptureController net)
    {
        _collectedTargetsBuffer.Clear();
        net.DrainCapturedTargets(_collectedTargetsBuffer);

        for (int i = 0; i < _collectedTargetsBuffer.Count; i++)
        {
            CollectCapturedTarget(_collectedTargetsBuffer[i]);
        }

        _collectedTargetsBuffer.Clear();
    }

    private void CollectCapturedTarget(ICapturable target)
    {
        if (IsMissing(target)) return;

        CreatureDefinition creature = target.CaptureData.creature;
        if (_playerInventory == null || !_playerInventory.TryAddCreature(creature))
        {
            target.OnCaptureReleased(CaptureReleaseReason.Interrupted);
            return;
        }

        target.OnCaptureReleased(CaptureReleaseReason.Collected);
        Component targetComponent = target as Component;
        if (targetComponent == null)
        {
            return;
        }

        targetComponent.GetComponent<StageSpawnedObject>()
            ?.NotifyRemovedFromStage();

        CaptureAnimationController animationController =
            targetComponent.GetComponent<CaptureAnimationController>()
            ?? targetComponent.gameObject.AddComponent<CaptureAnimationController>();

        Transform collectTarget = _shootOrigin != null ? _shootOrigin : transform;
        animationController.PlayCollectTo(collectTarget, null);
    }

    private static bool IsMissing(ICapturable target)
    {
        return target == null || target is UnityEngine.Object unityObject && unityObject == null;
    }

    private sealed class NetRuntime
    {
        public NetCaptureController net;
        public Tween shootTween;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_shootOrigin == null) return;

        Gizmos.color = Color.orange;
        switch (_netGunState)
        {
            case NetGunState.Charging:
                float chargeRatio = (_data.ChargeTime <= 0f)
                    ? 1f
                    : Mathf.Clamp01(_chargingTime / _data.ChargeTime);
                float minShootRange = 0.1f;
                float shootDistance = Mathf.Lerp(minShootRange, _data.MaxShootRange, chargeRatio);

                Gizmos.DrawWireSphere(_shootOrigin.position, shootDistance);
                break;

            case NetGunState.Retracting:
                Gizmos.DrawWireSphere(_shootOrigin.position, _collectRadiusThreshold);
                break;
        }

        Gizmos.DrawLine(
            _shootOrigin.position,
            _shootOrigin.position + _shootOrigin.up * _data.MaxShootRange);
    }
#endif
}

[System.Serializable]
public struct NetGunData
{
    [Header("Unlock Settings")]
    public bool isUnlocked;

    [Header("Ammo Settings")]
    [Tooltip("Maximum number of nets that can be fired during one exploration session.")]
    [Min(0)] public int ammoCapacity;

    [Header("Net Settings")]
    public NetData netData;

    [Header("Shoot Settings")]
    [Min(1)] public int netCount;
    [Min(0.1f)] public float baseShootSpeed;
    [Min(0f)] public float shootSpeedRatio;
    [Min(0.1f)] public float baseShootRange;
    [Min(0f)] public float shootRangeRatio;
    [Range(0f, 3f)] public float baseChargeTime;
    [Min(0f)] public float chargeTimeRatio;

    [Header("Collect Settings")]
    public float baseCollectSpeed;
    [Min(0f)] public float collectSpeedRatio;

    public float ShootSpeed => Mathf.Max(0f, baseShootSpeed * shootSpeedRatio);
    public float MaxShootRange => Mathf.Max(0f, baseShootRange * shootRangeRatio);
    public float ChargeTime => Mathf.Max(0f, baseChargeTime * chargeTimeRatio);
    public float CollectSpeed => Mathf.Max(0f, baseCollectSpeed * collectSpeedRatio);
}

[System.Serializable]
public struct NetData
{
    [Min(1)] public int captureCount;
    [Range(0f, 0.5f)] public float spreadDuration;
    [Range(0f, 0.5f)] public float foldDuration;
    [Min(0.1f)] public float radius;
    [Min(0f)] public float radiusRatio;

    public float Radius => Mathf.Max(0f, radius * radiusRatio);
}
