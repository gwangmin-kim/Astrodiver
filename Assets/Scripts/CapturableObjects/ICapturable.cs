using UnityEngine;

public interface ICapturable
{
    public CreatureCaptureData CaptureData { get; }
    public Vector2 Position { get; }
    public float Radius { get; }
    public Vector2 BehaviorVector { get; } // 대상의 행동패턴에 따른 움직임 벡터

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
