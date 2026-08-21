using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class NetCaptureController : MonoBehaviour
{
    [SerializeField] private Collider2D _captureCollider;
    [SerializeField, Range(0f, 1f)] private float _netThickness;
    private readonly List<CapturableObject> _capturedTargets = new();
    private readonly List<CapturableObject> _activeTargets = new();
    private readonly List<CapturedTargetSnapshot> _snapshots = new();
    private readonly List<ReleaseRequest> _pendingReleases = new();
    private NetData _data;

    private enum NetState { Folded, Spreading, Deployed, Folding }
    [SerializeField] private NetState _netState = NetState.Folded;

    public event Action SpreadStarted;
    public event Action FoldStarted;
    public event Action FoldReset;
    public event Action FoldCompleted;
    public bool IsFolded => _netState == NetState.Folded;
    public bool IsSpreading => _netState == NetState.Spreading;
    public bool IsDeployed => _netState == NetState.Deployed;
    public bool IsFolding => _netState == NetState.Folding;
    public bool IsAvailable => !gameObject.activeSelf;
    public bool IsRecallable => IsDeployed;
    public bool CanCapture => IsDeployed;
    public float Radius => _data.Radius;
    public float SpreadDuration => Mathf.Max(0f, _data.spreadDuration);
    public float FoldDuration => Mathf.Max(0f, _data.foldDuration);

    private void Awake()
    {
        if (_captureCollider == null) _captureCollider = GetComponent<Collider2D>();
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        _captureCollider.isTrigger = true;
        SetColliderEnabled(false);
    }

    private void OnDisable() => Release(CaptureReleaseReason.Interrupted);
    private void OnTriggerEnter2D(Collider2D other) => TryCapture(other);
    private void OnTriggerStay2D(Collider2D other) => TryCapture(other);
    public void Initialize(NetData data) => _data = data;

    public void PrepareForLaunch() { Release(CaptureReleaseReason.Interrupted); _netState = NetState.Spreading; }
    public void BeginSpread() { Release(CaptureReleaseReason.Interrupted); _netState = NetState.Spreading; SpreadStarted?.Invoke(); }
    public void CompleteSpread()
    {
        if (_netState != NetState.Spreading) return;
        _netState = NetState.Deployed;
        if (_captureCollider is CircleCollider2D circle) circle.radius = Radius;
        SetColliderEnabled(true);
    }
    public void BeginFold(CaptureReleaseReason reason)
    {
        SetColliderEnabled(false);
        if (reason != CaptureReleaseReason.Collected) Release(reason);
        _netState = NetState.Folding;
        FoldStarted?.Invoke();
    }
    public void CompleteFold() { if (_netState == NetState.Folding) { _netState = NetState.Folded; FoldCompleted?.Invoke(); } }
    public void ResetFolded() { Release(CaptureReleaseReason.Interrupted); _netState = NetState.Folded; FoldReset?.Invoke(); }

    // The net only supplies capture context and enforces its boundary. Motion is owned by each creature.
    public void UpdateCapturedTargets(Vector2 netVelocity, float deltaTime)
    {
        if (!CanCapture) return;
        CollectActiveTargets();
        BuildSnapshots();
        CaptureMotionContext context = new(transform.position, netVelocity, _snapshots);
        for (int i = 0; i < _activeTargets.Count; i++) _activeTargets[i].OnCapturedMove(context, deltaTime);
        ProcessPendingReleases();
        ClampActiveTargets();
    }

    public void RequestRelease(CapturableObject target, CaptureReleaseReason reason)
    {
        if (target == null || !_capturedTargets.Contains(target)) return;
        for (int i = 0; i < _pendingReleases.Count; i++)
            if (_pendingReleases[i].target == target) return;
        _pendingReleases.Add(new ReleaseRequest(target, reason));
    }

    public void DrainCapturedTargets(List<CapturableObject> drainedTargets)
    {
        SetColliderEnabled(false);
        for (int i = 0; i < _capturedTargets.Count; i++) if (!IsMissing(_capturedTargets[i])) drainedTargets?.Add(_capturedTargets[i]);
        _capturedTargets.Clear();
    }

    public void DrainCapturedTargets(CaptureReleaseReason reason, List<CapturableObject> drainedTargets)
    {
        SetColliderEnabled(false);
        for (int i = _capturedTargets.Count - 1; i >= 0; i--) ReleaseTarget(_capturedTargets[i], reason, drainedTargets);
    }

    private void CollectActiveTargets()
    {
        _activeTargets.Clear();
        for (int i = _capturedTargets.Count - 1; i >= 0; i--)
        {
            CapturableObject target = _capturedTargets[i];
            if (IsMissing(target)) { _capturedTargets.RemoveAt(i); continue; }
            if (!target.isActiveAndEnabled) { ReleaseTarget(target, CaptureReleaseReason.Interrupted, null); continue; }
            _activeTargets.Add(target);
        }
    }

    private void BuildSnapshots()
    {
        _snapshots.Clear();
        for (int i = 0; i < _activeTargets.Count; i++) _snapshots.Add(new CapturedTargetSnapshot(_activeTargets[i]));
    }

    private void ProcessPendingReleases()
    {
        for (int i = 0; i < _pendingReleases.Count; i++) ReleaseTarget(_pendingReleases[i].target, _pendingReleases[i].reason, null);
        _pendingReleases.Clear();
    }

    private void ClampActiveTargets()
    {
        float innerRadius = Mathf.Max(0f, Radius - _netThickness);
        Vector2 center = transform.position;
        for (int i = 0; i < _activeTargets.Count; i++)
        {
            CapturableObject target = _activeTargets[i];
            if (!_capturedTargets.Contains(target)) continue;
            float validRadius = Mathf.Max(0f, innerRadius - target.Radius);
            Vector2 offset = target.Position - center;
            Vector2 position = offset.sqrMagnitude > validRadius * validRadius && offset.sqrMagnitude > Mathf.Epsilon ? center + offset.normalized * validRadius : target.Position;
            target.OnCaptureClamped(position);
        }
    }

    private void Release(CaptureReleaseReason reason) => DrainCapturedTargets(reason, null);
    private void ReleaseTarget(CapturableObject target, CaptureReleaseReason reason, List<CapturableObject> drainedTargets)
    {
        if (target == null) return;
        _capturedTargets.Remove(target);
        if (!IsMissing(target))
        {
            drainedTargets?.Add(target);
            target.OnCaptureReleased(reason);
        }
    }

    private void TryCapture(Collider2D other)
    {
        if (!CanCapture || !other.TryGetComponent<CapturableObject>(out CapturableObject target) || target == null) return;
        if (IsMissing(target) || _capturedTargets.Contains(target) || _capturedTargets.Count >= Mathf.Max(1, _data.captureCount)) return;
        NetCaptureContext context = new(this, transform.position, Radius);
        if (!target.CanBeCaptured(context)) return;
        _capturedTargets.Add(target);
        target.OnCaptureStarted(context);
    }

    private void SetColliderEnabled(bool enabled)
    {
        if (_captureCollider != null) _captureCollider.enabled = enabled;
    }
    private static bool IsMissing(CapturableObject target) => target == null;

    private readonly struct ReleaseRequest
    {
        public readonly CapturableObject target;
        public readonly CaptureReleaseReason reason;
        public ReleaseRequest(CapturableObject target, CaptureReleaseReason reason)
        {
            this.target = target;
            this.reason = reason;
        }
    }
}
