using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

public class NetGunController : MonoBehaviour
{
    [SerializeField] private NetGunData _data;
    [SerializeField] private NetCaptureController _netPrefab;

    [Header("Capture Settings")]
    [SerializeField] private Transform _shootOrigin;
    [SerializeField] private float _collectRadiusThreshold;

    [Header("Inertia Settings")]
    [SerializeField] private Ease _shootEase;
    [SerializeField][Range(0f, 1f)] private float _dampingTime;

    private readonly List<NetRuntime> _netRuntimeList = new();
    private readonly NetMovementManager _movementManager = new();
    private float _chargingTime;
    private NetRuntime _chargingNet;
    private NetRuntime _retractingNet;

    private enum NetGunState
    {
        Idle, // 대기 상태
        Charging, // 발사 전 충전 중
        Retracting // 회수 중
    }

    private NetGunState _netGunState = NetGunState.Idle;

    public bool IsSwitchable => _netGunState == NetGunState.Idle;

    private void Awake()
    {
        BuildNetPool();
    }

    private void Start()
    {
        ResetAllNetsToIdle();
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

        net.shootTween.Stop();

        float chargeRatio = (_data.chargeTime <= 0f)
            ? 1f
            : Mathf.Clamp01(_chargingTime / _data.chargeTime);
        float shootDistance = (_data.shootRangeRatioWithNoCharge >= 1f)
            ? _data.maxShootRange
            : Mathf.Lerp(_data.shootRangeRatioWithNoCharge, 1f, chargeRatio) * _data.maxShootRange;

        Vector2 startPosition = _shootOrigin.position;
        Vector2 shootDirection = _shootOrigin.up;
        Vector2 endPosition = startPosition + shootDirection * shootDistance;

        net.net.transform.SetPositionAndRotation(startPosition, _shootOrigin.rotation);
        net.net.transform.SetParent(null, true);
        net.net.ResetFolded();
        net.net.PrepareForLaunch();
        net.net.gameObject.SetActive(true);

        float shootDuration = Mathf.Max(_data.shootDuration, 0.01f);

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

    private bool TryStartRetracting(NetRuntime net)
    {
        if (net == null || !net.net.IsRecallable) return false;

        net.shootTween.Stop();
        _retractingNet = net;
        _netGunState = NetGunState.Retracting;
        return true;
    }

    private void UpdateRetracting(float deltaTime)
    {
        if (_retractingNet == null || !_retractingNet.net.IsRecallable)
        {
            _retractingNet = null;
            _netGunState = NetGunState.Idle;
            return;
        }

        Vector2 diff = (Vector2)_shootOrigin.position - (Vector2)_retractingNet.net.transform.position;
        if (diff.sqrMagnitude <= _collectRadiusThreshold * _collectRadiusThreshold)
        {
            FinishRetracting(_retractingNet);
            return;
        }

        Vector2 targetVelocity = _data.collectSpeed * diff.normalized;
        _movementManager.SetTargetVelocity(_retractingNet.net, targetVelocity);
    }

    private void CancelRetracting()
    {
        _retractingNet = null;
        _netGunState = NetGunState.Idle;
    }

    private void FinishRetracting(NetRuntime net)
    {
        net.shootTween.Stop();
        _movementManager.Reset(net.net);

        net.net.BeginFold(CaptureReleaseReason.Collected);
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
            if (_netRuntimeList[i].net.IsFolded)
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
        float radiusSqr = _data.netData.radius * _data.netData.radius;

        for (int i = 0; i < _netRuntimeList.Count; i++)
        {
            NetRuntime net = _netRuntimeList[i];
            if (!net.net.IsRecallable) continue;

            Vector2 toNet = (Vector2)net.net.transform.position - origin;
            float projection = Vector2.Dot(toNet, direction);
            if (projection <= 0f && toNet.sqrMagnitude > radiusSqr) continue;
            if (projection > _data.maxShootRange) continue;

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
        if (net != _retractingNet) return;

        ResetNetToPool(net);
        _retractingNet = null;
        _netGunState = NetGunState.Idle;
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
                float chargeRatio = (_data.chargeTime <= 0f)
                    ? 1f
                    : Mathf.Clamp01(_chargingTime / _data.chargeTime);
                float shootDistance = (_data.shootRangeRatioWithNoCharge >= 1f)
                    ? _data.maxShootRange
                    : Mathf.Lerp(_data.shootRangeRatioWithNoCharge, 1f, chargeRatio) * _data.maxShootRange;

                Gizmos.DrawWireSphere(_shootOrigin.position, shootDistance);
                break;

            case NetGunState.Retracting:
                Gizmos.DrawWireSphere(_shootOrigin.position, _collectRadiusThreshold);
                break;
        }

        Gizmos.DrawLine(
            _shootOrigin.position,
            _shootOrigin.position + _shootOrigin.up * _data.maxShootRange);
    }
#endif
}

[System.Serializable]
public struct NetGunData
{
    [Header("Net Settings")]
    public NetData netData;

    [Header("Shoot Settings")]
    [Min(1)] public int netCount;
    [Range(0.1f, 5f)] public float shootDuration;
    [Min(0.1f)] public float maxShootRange;
    [Range(0f, 1f)] public float shootRangeRatioWithNoCharge;
    [Range(0f, 3f)] public float chargeTime;

    [Header("Collect Settings")]
    public float collectSpeed;
}

[System.Serializable]
public struct NetData
{
    [Min(1)] public int captureCount;
    [Range(0f, 0.5f)] public float spreadDuration;
    [Range(0f, 0.5f)] public float foldDuration;
    [Min(0.1f)] public float radius;
}
