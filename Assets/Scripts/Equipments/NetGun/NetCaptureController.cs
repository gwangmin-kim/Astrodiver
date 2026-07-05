using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class NetCaptureController : MonoBehaviour
{
    [SerializeField] private Collider2D _captureCollider;

    private readonly List<ICapturable> _capturedTargets = new();

    private Vector2 _lastNetCenter;
    private float _netRadius;

    private enum NetState
    {
        Folded,
        Spreading,
        Deployed
    }

    [SerializeField] private NetState _netState = NetState.Folded;

    public event Action<NetSpreadData> SpreadStarted;
    public event Action<NetFoldData> FoldStarted;
    public event Action FoldReset;
    public event Action FoldCompleted;

    public bool IsFolded => _netState == NetState.Folded;
    public bool IsSpreading => _netState == NetState.Spreading;
    public bool IsDeployed => _netState == NetState.Deployed;
    public bool IsRecallable => IsDeployed;
    public bool CanCapture => IsDeployed;

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

    public void PrepareForLaunch()
    {
        Release(CaptureReleaseReason.Interrupted);
        _netState = NetState.Spreading;
    }

    public void BeginSpread(NetSpreadData spreadData)
    {
        Release(CaptureReleaseReason.Interrupted);
        _netRadius = Mathf.Max(0f, spreadData.radius);
        _netState = NetState.Spreading;
        gameObject.SetActive(true);
        SpreadStarted?.Invoke(spreadData);
    }

    public void CompleteSpread()
    {
        if (_netState != NetState.Spreading) return;

        _lastNetCenter = transform.position;
        _netState = NetState.Deployed;
        ApplyColliderRadius(_netRadius);
        SetColliderEnabled(true);
    }

    public void BeginFold(float duration, CaptureReleaseReason releaseReason)
    {
        Release(releaseReason);
        _netState = NetState.Folded;
        gameObject.SetActive(true);

        FoldStarted?.Invoke(new NetFoldData
        {
            duration = duration
        });
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
        float radiusSqr = _netRadius * _netRadius;
        NetCaptureContext context = new()
        {
            netCenter = netCenter,
            netRadius = _netRadius
        };

        for (int i = _capturedTargets.Count - 1; i >= 0; i--)
        {
            ICapturable target = _capturedTargets[i];
            if (IsMissing(target))
            {
                _capturedTargets.RemoveAt(i);
                continue;
            }

            if (!target.CanBeCaptured(context))
            {
                target.OnCaptureReleased(CaptureReleaseReason.Interrupted);
                _capturedTargets.RemoveAt(i);
                continue;
            }

            Vector2 targetPosition = target.CapturePosition + netDelta;
            Vector2 offset = targetPosition - netCenter;

            if (offset.sqrMagnitude > radiusSqr)
            {
                targetPosition = netCenter + offset.normalized * _netRadius;
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

        if (IsMissing(target) || _capturedTargets.Contains(target)) return;

        NetCaptureContext context = new()
        {
            netCenter = transform.position,
            netRadius = _netRadius
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
}

public struct NetSpreadData
{
    public float radius;
    public float time;
}

public struct NetFoldData
{
    public float duration;
}
