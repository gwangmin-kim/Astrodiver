using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class NetCaptureController : MonoBehaviour
{
    [Header("Capture Settings")]
    [SerializeField] private Collider2D _captureCollider;
    [Tooltip("그물 두께. 생물이 충분히 그물 안쪽으로 들어가도록 해주는 장치")]
    [SerializeField][Range(0f, 1f)] private float _netThickness;
    [Tooltip("그물이 움직일 때 내부 생물들이 그물에 딸려가는 정도\n"
            + "그물의 속도에 이 비율이 곱해진 속도가 내부 생물에게 적용됨")]
    [SerializeField][Range(0f, 1f)] private float _followDampingRatio;

    private readonly List<ICapturable> _capturedTargets = new();

    private Vector2 _lastNetCenter; // 움직임 delta를 계산하기 위한 포지션 캐싱
    private NetData _data;

    private enum NetState
    {
        Folded, // 접힌 상태
        Spreading, // 펼쳐지는 중
        Deployed // 펼쳐짐
    }

    [SerializeField] private NetState _netState = NetState.Folded;

    public event Action SpreadStarted;
    public event Action FoldStarted;
    public event Action FoldReset;
    public event Action FoldCompleted;

    public bool IsFolded => _netState == NetState.Folded;
    public bool IsSpreading => _netState == NetState.Spreading;
    public bool IsDeployed => _netState == NetState.Deployed;
    public bool IsRecallable => IsDeployed;
    public bool CanCapture => IsDeployed;
    public float Radius => Mathf.Max(0f, _data.radius);
    public float SpreadDuration => Mathf.Max(0f, _data.spreadDuration);
    public float FoldDuration => Mathf.Max(0f, _data.foldDuration);

    private void Awake()
    {
        if (_captureCollider == null)
        {
            _captureCollider = GetComponent<Collider2D>();
        }

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.simulated = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        _captureCollider.isTrigger = true;
        SetColliderEnabled(false);
    }

    private void OnDisable()
    {
        Release(CaptureReleaseReason.Interrupted);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCapture(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCapture(other);
    }

    public void Initialize(NetData data)
    {
        _data = data;
    }

    public void PrepareForLaunch()
    {
        Release(CaptureReleaseReason.Interrupted);
        _netState = NetState.Spreading;
    }

    public void BeginSpread()
    {
        Release(CaptureReleaseReason.Interrupted);
        _netState = NetState.Spreading;
        SpreadStarted?.Invoke();
    }

    public void CompleteSpread()
    {
        if (_netState != NetState.Spreading) return;

        _lastNetCenter = transform.position;
        _netState = NetState.Deployed;
        ApplyColliderRadius(Radius);
        SetColliderEnabled(true);
    }

    public void BeginFold(CaptureReleaseReason releaseReason)
    {
        Release(releaseReason);
        _netState = NetState.Folded;

        FoldStarted?.Invoke();
    }

    public void CompleteFold()
    {
        if (_netState != NetState.Folded) return;

        FoldCompleted?.Invoke();
    }

    public void ResetFolded()
    {
        Release(CaptureReleaseReason.Interrupted);
        _netState = NetState.Folded;
        FoldReset?.Invoke();
    }

    public void UpdateCapturedTargets(Vector2 netVelocity, float deltaTime)
    {
        if (!CanCapture) return;

        Vector2 netCenter = transform.position;
        Vector2 netDelta = netCenter - _lastNetCenter;
        float innerRadius = Radius - _netThickness;

        for (int i = _capturedTargets.Count - 1; i >= 0; i--)
        {
            ICapturable target = _capturedTargets[i];
            if (IsMissing(target))
            {
                _capturedTargets.RemoveAt(i);
                continue;
            }

            if (IsInactive(target))
            {
                target.OnCaptureReleased(CaptureReleaseReason.Interrupted);
                _capturedTargets.RemoveAt(i);
                continue;
            }

            Vector2 targetPosition = target.Position + _followDampingRatio * netDelta;
            Vector2 offset = targetPosition - netCenter;

            float validRadius = Mathf.Max(0f, innerRadius - target.Radius);
            float radiusSqr = validRadius * validRadius;

            if (offset.sqrMagnitude > radiusSqr)
            {
                targetPosition = netCenter + offset.normalized * validRadius;
            }

            target.OnCapturedMove(targetPosition, netVelocity, deltaTime);
        }

        _lastNetCenter = netCenter;
    }

    private void Release(CaptureReleaseReason reason)
    {
        SetColliderEnabled(false);

        if (_capturedTargets.Count == 0)
        {
            return;
        }

        for (int i = _capturedTargets.Count - 1; i >= 0; i--)
        {
            ICapturable target = _capturedTargets[i];
            if (!IsMissing(target))
            {
                target.OnCaptureReleased(reason);
            }
        }

        _capturedTargets.Clear();
    }

    private void TryCapture(Collider2D other)
    {
        if (!CanCapture) return;

        ICapturable target = other.GetComponent<ICapturable>();
        if (target == null)
        {
            target = other.GetComponentInParent<ICapturable>();
        }

        if (target == null) return;

        if (IsMissing(target) || _capturedTargets.Contains(target)) return;
        if (_capturedTargets.Count >= GetCaptureCapacity()) return;

        NetCaptureContext context = new()
        {
            netCenter = transform.position,
            netRadius = Radius
        };

        if (!target.CanBeCaptured(context)) return;

        _capturedTargets.Add(target);
        target.OnCaptureStarted(context);
    }

    private void SetColliderEnabled(bool isEnabled)
    {
        if (_captureCollider != null)
        {
            _captureCollider.enabled = isEnabled;
        }
    }

    private void ApplyColliderRadius(float radius)
    {
        if (_captureCollider is CircleCollider2D circleCollider)
        {
            circleCollider.radius = radius;
        }
    }

    private static bool IsMissing(ICapturable target)
    {
        return target == null || target is UnityEngine.Object unityObject && unityObject == null;
    }

    private static bool IsInactive(ICapturable target)
    {
        return target is Behaviour behaviour && !behaviour.isActiveAndEnabled;
    }

    private int GetCaptureCapacity()
    {
        return Mathf.Max(1, _data.captureCount);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_netState == NetState.Deployed)
        {
            Gizmos.color = Color.darkRed;
            Gizmos.DrawWireSphere(transform.position, Radius);
            Gizmos.color = Color.softRed;
            Gizmos.DrawWireSphere(transform.position, Radius - _netThickness);
        }
    }
#endif
}

