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
    [SerializeField] private float _retractDampingTime;

    private readonly List<NetRuntime> _netList = new();
    private float _chargingTime;
    private NetRuntime _chargingNet;
    private NetRuntime _retractingNet;

    private enum NetGunState
    {
        Idle,
        Charging,
        Retracting
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
        for (int i = 0; i < _netList.Count; i++)
        {
            _netList[i].shootTween.Stop();
            _netList[i].net.ResetFolded();
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

        UpdateDeployedNets(Time.deltaTime);
    }

    private void BuildNetPool()
    {
        _netList.Clear();
        if (_netPrefab == null) return;

        int maxNetCount = _data.maxNetCount;

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

        net.FoldCompleted += () => HandleNetFoldCompleted(runtime);
        _netList.Add(runtime);
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
        net.velocity = Vector2.zero;
        net.smoothDampVelocity = Vector2.zero;
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
        float shootDistance = Mathf.Lerp(0.5f, 1f, chargeRatio) * _data.shootRange;

        Vector2 startPosition = _shootOrigin.position;
        Vector2 shootDirection = _shootOrigin.up;
        Vector2 endPosition = startPosition + shootDirection * shootDistance;

        net.net.transform.position = startPosition;
        net.net.transform.rotation = _shootOrigin.rotation;
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
        NetSpreadData spreadData = new()
        {
            radius = _data.netRadius,
            time = _data.spreadDelay
        };

        net.net.BeginSpread(spreadData);
    }

    private bool TryStartRetracting(NetRuntime net)
    {
        if (net == null || !net.net.IsRecallable) return false;

        net.shootTween.Stop();
        _retractingNet = net;
        _netGunState = NetGunState.Retracting;
        net.smoothDampVelocity = Vector2.zero;
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
        Vector2 direction = diff.normalized;
        Vector2 targetVelocity = _data.collectSpeed * direction;

        _retractingNet.velocity = Vector2.SmoothDamp(
            _retractingNet.velocity,
            targetVelocity,
            ref _retractingNet.smoothDampVelocity,
            _retractDampingTime,
            Mathf.Infinity,
            deltaTime);

        _retractingNet.net.transform.position += (Vector3)(_retractingNet.velocity * deltaTime);
        _retractingNet.net.UpdateCapturedTargets(_retractingNet.velocity, deltaTime);

        if (diff.sqrMagnitude <= _collectRadiusThreshold * _collectRadiusThreshold)
        {
            FinishRetracting(_retractingNet);
        }
    }

    private void FinishRetracting(NetRuntime net)
    {
        net.shootTween.Stop();
        net.velocity = Vector2.zero;
        net.smoothDampVelocity = Vector2.zero;

        net.net.BeginFold(0.1f, CaptureReleaseReason.Collected);
    }

    private void ResetNetToPool(NetRuntime net)
    {
        net.shootTween.Stop();
        net.net.transform.SetParent(transform, false);
        net.net.ResetFolded();
        net.net.gameObject.SetActive(false);
    }

    private void ResetAllNetsToIdle()
    {
        for (int i = 0; i < _netList.Count; i++)
        {
            ResetNetToPool(_netList[i]);
        }

        _chargingNet = null;
        _retractingNet = null;
        _netGunState = NetGunState.Idle;
    }

    private void UpdateDeployedNets(float deltaTime)
    {
        for (int i = 0; i < _netList.Count; i++)
        {
            NetRuntime net = _netList[i];
            if (net == _retractingNet || !net.net.IsDeployed) continue;

            net.net.UpdateCapturedTargets(Vector2.zero, deltaTime);
        }
    }

    private bool HasAvailableNet()
    {
        return FindAvailableNet() != null;
    }

    private NetRuntime FindAvailableNet()
    {
        for (int i = 0; i < _netList.Count; i++)
        {
            if (_netList[i].net.IsFolded)
            {
                return _netList[i];
            }
        }

        return null;
    }

    private NetRuntime FindClosestRecallableNet()
    {
        NetRuntime closestNet = null;
        float closestSqrDistance = Mathf.Infinity;
        Vector2 origin = _shootOrigin.position;

        for (int i = 0; i < _netList.Count; i++)
        {
            NetRuntime net = _netList[i];
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
        float radiusSqr = _data.netRadius * _data.netRadius;

        for (int i = 0; i < _netList.Count; i++)
        {
            NetRuntime net = _netList[i];
            if (!net.net.IsRecallable) continue;

            Vector2 toNet = (Vector2)net.net.transform.position - origin;
            float projection = Vector2.Dot(toNet, direction);
            if (projection <= 0f && toNet.sqrMagnitude > radiusSqr) continue;
            if (projection > _data.shootRange) continue;

            float perpendicularSqr = Mathf.Max(0f, toNet.sqrMagnitude - projection * projection);
            if (perpendicularSqr > radiusSqr) continue;
            if (projection >= closestProjection) continue;

            closestProjection = projection;
            aimedNet = net;
        }

        return aimedNet;
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
                float shootDistance = Mathf.Lerp(0.5f, 1f, chargeRatio) * _data.shootRange;
                Gizmos.DrawWireSphere(_shootOrigin.position, shootDistance);
                break;

            case NetGunState.Retracting:
                Gizmos.DrawWireSphere(_shootOrigin.position, _collectRadiusThreshold);
                break;
        }

        Gizmos.DrawLine(
            _shootOrigin.position,
            _shootOrigin.position + _shootOrigin.up * _data.shootRange);
    }
#endif

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
        public Vector2 velocity;
        public Vector2 smoothDampVelocity;
    }
}

[System.Serializable]
public struct NetGunData
{
    [Header("Shoot Settings")]
    [Min(1)] public int maxNetCount;
    public float shootDuration;
    public float shootRange;
    public float chargeTime;
    public float spreadDelay;
    public float netRadius;

    [Header("Collect Settings")]
    public float collectSpeed;
}
