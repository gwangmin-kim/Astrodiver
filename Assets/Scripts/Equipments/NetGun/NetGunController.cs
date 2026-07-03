using UnityEngine;
using PrimeTween;

public class NetGunController : MonoBehaviour
{
    [SerializeField] private NetGunData _data;
    [SerializeField] private NetController _net;

    [Header("Capture Settings")]
    [SerializeField] private Transform _shootOrigin;
    [SerializeField] private float _collectRadiusThreshold;

    [Header("Inertia Settings")]
    [SerializeField] private Ease _shootEase;
    [SerializeField] private float _deployedDampingTime;
    [SerializeField] private float _retractDampingTime;

    private Tween _currentTween;
    private float _chargingTime;

    // 완전히 자유롭게 떠다니는 상태에 한해서, 부드러운 속도 전환을 위해 속도를 캐싱하는 변수
    // 발사 시점에는 '속도'가 아닌 '좌표'를 기준으로 위치를 부드럽게 변경하니 _currentVelocity를 반영하지 않음
    private Vector2 _currentVelocity;
    private Vector2 _smoothDampVelocity;

    private enum NetState
    {
        Idle,
        Charging,
        Shooting,
        Spreading,
        Deployed,
        Retracting
    }

    [SerializeField] private NetState _netState = NetState.Idle;

    public bool IsSwitchable => _netState == NetState.Idle;

    private void Start()
    {
        ResetNetToIdle();
    }

    private void OnDisable()
    {
        _currentTween.Stop();
    }

    public bool OnPressCapture()
    {
        switch (_netState)
        {
            case NetState.Idle:
                StartCharging();
                return true;

            case NetState.Deployed:
                StartRetracting();
                return true;

            default:
                return false;
        }
    }

    public bool OnReleaseCapture()
    {
        switch (_netState)
        {
            case NetState.Charging:
                StartShooting();
                return true;

            default:
                return false;
        }
    }

    private void Update()
    {
        switch (_netState)
        {
            case NetState.Charging:
                _chargingTime += Time.deltaTime;
                break;

            case NetState.Deployed:
                UpdateDeployed(Time.deltaTime);
                break;

            case NetState.Retracting:
                UpdateRetracting(Time.deltaTime);
                break;
        }
    }

    private void StartCharging()
    {
        _netState = NetState.Charging;
        _chargingTime = 0f;
        _currentVelocity = Vector2.zero;
        _smoothDampVelocity = Vector2.zero;
    }

    private void StartShooting()
    {
        _currentTween.Stop();
        _netState = NetState.Shooting;

        float chargeRatio = (_data.chargeTime <= 0f)
            ? 1f
            : _chargingTime / _data.chargeTime;
        float shootDistance = Mathf.Lerp(0.5f, 1f, chargeRatio) * _data.shootRange;

        Vector2 startPosition = _shootOrigin.position;
        Vector2 shootDirection = _shootOrigin.up;
        Vector2 endPosition = startPosition + shootDirection * shootDistance;

        _net.transform.position = startPosition;
        _net.transform.rotation = _shootOrigin.rotation;
        _net.transform.SetParent(null, true);
        _net.gameObject.SetActive(true);

        float shootDuration = Mathf.Max(_data.shootDuration, 0.01f);

        _currentTween = Tween.Position(
                _net.transform,
                endValue: endPosition,
                duration: shootDuration,
                ease: _shootEase)
            .OnComplete(StartSpreading);
    }

    private void StartSpreading()
    {
        _netState = NetState.Spreading;

        NetSpreadData spreadData = new()
        {
            radius = _data.netRadius,
            time = _data.spreadDelay
        };

        _net.Spread(spreadData, () => _netState = NetState.Deployed);
    }

    private void StartRetracting()
    {
        _currentTween.Stop();
        _netState = NetState.Retracting;
        _smoothDampVelocity = Vector2.zero;
    }

    private void UpdateDeployed(float deltaTime)
    {
        _currentVelocity = Vector2.SmoothDamp(
            _currentVelocity,
            Vector2.zero,
            ref _smoothDampVelocity,
            _deployedDampingTime,
            Mathf.Infinity,
            deltaTime);

        _net.transform.position += (Vector3)(_currentVelocity * deltaTime);
    }

    private void UpdateRetracting(float deltaTime)
    {
        Vector2 diff = (Vector2)_shootOrigin.position - (Vector2)_net.transform.position;
        Vector2 direction = diff.normalized;
        Vector2 targetVelocity = _data.collectSpeed * direction;

        _currentVelocity = Vector2.SmoothDamp(
            _currentVelocity,
            targetVelocity,
            ref _smoothDampVelocity,
            _retractDampingTime,
            Mathf.Infinity,
            deltaTime);

        _net.transform.position += (Vector3)(_currentVelocity * deltaTime);

        if (diff.sqrMagnitude <= _collectRadiusThreshold * _collectRadiusThreshold)
        {
            FinishRetracting();
        }
    }

    private void FinishRetracting()
    {
        _currentTween.Stop();
        _currentVelocity = Vector2.zero;
        _smoothDampVelocity = Vector2.zero;

        _net.Fold(0.1f, ResetNetToIdle);
    }

    private void ResetNetToIdle()
    {
        _currentTween.Stop();
        _net.transform.SetParent(transform, false);
        _net.ResetFolded();
        _net.gameObject.SetActive(false);
        _netState = NetState.Idle;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_shootOrigin == null || _net == null) return;

        Gizmos.color = Color.orange;
        switch (_netState)
        {
            case NetState.Charging:
                float chargeRatio = (_data.chargeTime <= 0f)
                    ? 1f
                    : Mathf.Clamp01(_chargingTime / _data.chargeTime);
                float shootDistance = Mathf.Lerp(0.5f, 1f, chargeRatio) * _data.shootRange;
                Gizmos.DrawWireSphere(_shootOrigin.position, shootDistance);
                break;

            case NetState.Deployed:
                Gizmos.DrawWireSphere(_net.transform.position, _data.netRadius);
                break;

            case NetState.Retracting:
                Gizmos.DrawWireSphere(_shootOrigin.position, _collectRadiusThreshold);
                break;
        }

        if (_net.gameObject.activeSelf)
        {
            Gizmos.DrawLine(_shootOrigin.position, _net.transform.position);
        }
    }
#endif
}

[System.Serializable]
public struct NetGunData
{
    [Header("Shoot Settings")]
    public float shootDuration;
    public float shootRange;
    public float chargeTime;
    public float spreadDelay;
    public float netRadius;

    [Header("Collect Settings")]
    public float collectSpeed;
}
