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
    [Tooltip("포획된 생물들이 서로 떨어져 있으려고 하는 거리 보정값")]
    [SerializeField][Min(0f)] private float _repulsionDistancePadding = 0.1f;
    [Tooltip("포획된 생물 간 척력의 강도")]
    [SerializeField][Range(0f, 1f)] private float _repulsionStrength = 0.5f;
    [Tooltip("한 프레임 안에서 생물 간 척력 보정을 반복하는 횟수")]
    [SerializeField][Range(1, 8)] private int _repulsionIterations = 2;

    private readonly List<ICapturable> _capturedTargets = new();
    private readonly List<ICapturable> _activeTargets = new();
    private readonly List<Vector2> _targetPositions = new();

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

        CollectActiveCapturedTargets();
        CalculateBehaviorTargetPositions(deltaTime);
        ApplyRepulsionToTargetPositions();
        ApplyNetFollowToTargetPositions(netDelta);
        ClampTargetPositionsInsideNet(netCenter, innerRadius);
        MoveCapturedTargets(netVelocity, deltaTime);

        _lastNetCenter = netCenter;
    }

    private void CollectActiveCapturedTargets()
    {
        _activeTargets.Clear();

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

            _activeTargets.Add(target);
        }
    }

    private void CalculateBehaviorTargetPositions(float deltaTime)
    {
        _targetPositions.Clear();

        for (int i = 0; i < _activeTargets.Count; i++)
        {
            ICapturable target = _activeTargets[i];
            _targetPositions.Add(target.Position + CalculateBehaviorMovement(target, deltaTime));
        }
    }

    private Vector2 CalculateBehaviorMovement(ICapturable target, float deltaTime)
    {
        if (target == null || deltaTime <= 0f)
        {
            return Vector2.zero;
        }

        return target.BehaviorVector * deltaTime;
    }

    private Vector2 CalculateNetFollowMovement(Vector2 netDelta)
    {
        return _followDampingRatio * netDelta;
    }

    private void ApplyRepulsionToTargetPositions()
    {
        int targetCount = _activeTargets.Count;
        if (targetCount <= 1 || _repulsionStrength <= 0f) return;

        int iterationCount = Mathf.Max(1, _repulsionIterations);
        for (int iteration = 0; iteration < iterationCount; iteration++)
        {
            for (int i = 0; i < targetCount; i++)
            {
                for (int j = i + 1; j < targetCount; j++)
                {
                    Vector2 repulsion = CalculateRepulsionMovement(i, j);
                    _targetPositions[i] += repulsion;
                    _targetPositions[j] -= repulsion;
                }
            }
        }
    }

    private Vector2 CalculateRepulsionMovement(int indexA, int indexB)
    {
        ICapturable targetA = _activeTargets[indexA];
        ICapturable targetB = _activeTargets[indexB];
        float minDistance = Mathf.Max(0f, targetA.Radius + targetB.Radius + _repulsionDistancePadding);
        if (minDistance <= 0f) return Vector2.zero;

        Vector2 offset = _targetPositions[indexA] - _targetPositions[indexB];
        float distance = offset.magnitude;
        if (distance >= minDistance) return Vector2.zero;

        Vector2 direction = distance > Mathf.Epsilon
            ? offset / distance
            : GetFallbackRepulsionDirection(indexA, indexB);

        float penetration = minDistance - distance;
        return direction * (0.5f * penetration * _repulsionStrength);
    }

    private void ApplyNetFollowToTargetPositions(Vector2 netDelta)
    {
        Vector2 netFollowMovement = CalculateNetFollowMovement(netDelta);
        if (netFollowMovement == Vector2.zero) return;

        for (int i = 0; i < _targetPositions.Count; i++)
        {
            _targetPositions[i] += netFollowMovement;
        }
    }

    private void ClampTargetPositionsInsideNet(Vector2 netCenter, float innerRadius)
    {
        for (int i = 0; i < _activeTargets.Count; i++)
        {
            _targetPositions[i] = ClampInsideNet(_activeTargets[i], _targetPositions[i], netCenter, innerRadius);
        }
    }

    private void MoveCapturedTargets(Vector2 netVelocity, float deltaTime)
    {
        for (int i = 0; i < _activeTargets.Count; i++)
        {
            _activeTargets[i].OnCapturedMove(_targetPositions[i], netVelocity, deltaTime);
        }
    }

    private static Vector2 ClampInsideNet(ICapturable target, Vector2 position, Vector2 netCenter, float innerRadius)
    {
        float validRadius = Mathf.Max(0f, innerRadius - target.Radius);
        Vector2 offset = position - netCenter;
        float radiusSqr = validRadius * validRadius;

        if (offset.sqrMagnitude <= radiusSqr)
        {
            return position;
        }

        if (offset.sqrMagnitude <= Mathf.Epsilon)
        {
            return netCenter;
        }

        return netCenter + offset.normalized * validRadius;
    }

    private static Vector2 GetFallbackRepulsionDirection(int indexA, int indexB)
    {
        float angle = (indexA + 1) * 2.399963f + indexB * 0.916298f;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
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

