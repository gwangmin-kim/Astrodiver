using UnityEngine;

public interface ICapturable
{
    public Vector2 Position { get; }
    public float Radius { get; }
    public CreatureCaptureData CaptureData { get; }

    public bool CanBeCaptured(NetCaptureContext context);
    public void OnCaptureStarted(NetCaptureContext context);
    public void OnCapturedMove(Vector2 targetPosition, Vector2 netVelocity, float deltaTime);
    public void OnCaptureReleased(CaptureReleaseReason reason);
}

public struct NetCaptureContext
{
    public Vector2 netCenter;
    public float netRadius;
}

public enum CaptureReleaseReason
{
    Interrupted,
    Escaped,
    Collected
}
