using UnityEngine;

public interface ICapturable
{
    public CaptureData CaptureData { get; }
    public Vector2 Position { get; }
    public float Radius { get; }
    public Vector2 BehaviorVector { get; } // 대상의 행동패턴에 따른 움직임 벡터

    public bool CanBeCaptured(NetCaptureContext context);
    public void OnCaptureStarted(NetCaptureContext context);
    public void OnCapturedMove(Vector2 targetPosition, Vector2 netVelocity, float deltaTime);
    public void OnCaptureReleased(CaptureReleaseReason reason);
}

[System.Serializable]
public struct CaptureData
{
    public CreatureResourceData resourceData;

    [Tooltip("대략적인 생물체의 크기를 결정\n"
            + "그물에 잡혔을 때 이 값을 고려하여 그물 내부에 위치하도록 조정됨")]
    [Min(0f)] public float radius;

    [Tooltip("그물에 잡혔을 때 그물에 부드럽게 끌려가는 관성 시간")]
    [Range(0f, 1f)] public float followDampingTime;
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
