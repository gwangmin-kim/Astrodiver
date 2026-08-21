using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base component for objects that can be captured by a net.
/// </summary>
public abstract class CapturableObject : MonoBehaviour
{
    public abstract CaptureData CaptureData { get; }
    public abstract Vector2 Position { get; }
    public abstract float Radius { get; }

    public abstract bool CanBeCaptured(NetCaptureContext context);
    public abstract void OnCaptureStarted(NetCaptureContext context);
    public abstract void OnCapturedMove(CaptureMotionContext context, float deltaTime);
    public abstract void OnCaptureClamped(Vector2 position);
    public abstract void OnCaptureReleased(CaptureReleaseReason reason);
}

[System.Serializable]
public struct CaptureData
{
    public CreatureDefinition creature;
    [Min(0f)] public float radius;
}

public readonly struct NetCaptureContext
{
    public readonly NetCaptureController net;
    public readonly Vector2 netCenter;
    public readonly float netRadius;

    public NetCaptureContext(NetCaptureController net, Vector2 center, float radius)
    {
        this.net = net;
        netCenter = center;
        netRadius = radius;
    }
}

public readonly struct CapturedTargetSnapshot
{
    public readonly CapturableObject target;
    public readonly Vector2 position;
    public readonly float radius;

    public CapturedTargetSnapshot(CapturableObject target)
    {
        this.target = target;
        position = target.Position;
        radius = target.Radius;
    }
}

public readonly struct CaptureMotionContext
{
    public readonly Vector2 netCenter;
    public readonly Vector2 netVelocity;
    public readonly IReadOnlyList<CapturedTargetSnapshot> targets;

    public CaptureMotionContext(Vector2 center, Vector2 velocity, IReadOnlyList<CapturedTargetSnapshot> targets)
    {
        netCenter = center;
        netVelocity = velocity;
        this.targets = targets;
    }
}

public enum CaptureReleaseReason
{
    Interrupted,
    Escaped,
    Collected
}
