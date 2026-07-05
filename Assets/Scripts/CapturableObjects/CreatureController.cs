using UnityEngine;

public class CreatureController : MonoBehaviour, ICapturable
{
    [SerializeField] private CreatureCaptureData _data;

    private Vector2 _captureSmoothVelocity;
    private bool _isCaptured;

    public Vector2 CapturePosition => transform.position;
    public CreatureCaptureData CaptureData => _data;

    private void OnDisable()
    {
        _isCaptured = false;
        _captureSmoothVelocity = Vector2.zero;
    }

    public bool CanBeCaptured(NetCaptureContext context)
    {
        return isActiveAndEnabled;
    }

    public void OnCaptureStarted(NetCaptureContext context)
    {
        _isCaptured = true;
        _captureSmoothVelocity = Vector2.zero;
    }

    public void OnCapturedMove(Vector2 targetPosition, Vector2 netVelocity, float deltaTime)
    {
        if (!_isCaptured) return;

        float dampingTime = _data.GetFollowDampingTime();
        float maxSpeed = _data.GetMaxFollowSpeed();

        Vector2 nextPosition = (dampingTime <= 0f)
            ? targetPosition
            : Vector2.SmoothDamp(
                transform.position,
                targetPosition,
                ref _captureSmoothVelocity,
                dampingTime,
                maxSpeed,
                deltaTime);

        transform.position = nextPosition;
    }

    public void OnCaptureReleased(CaptureReleaseReason reason)
    {
        _isCaptured = false;
        _captureSmoothVelocity = Vector2.zero;
    }
}

[System.Serializable]
public struct CreatureCaptureData
{
    [SerializeField] private float _followDampingTime;
    [SerializeField] private float _maxFollowSpeed;

    public float GetFollowDampingTime()
    {
        return _followDampingTime > 0f ? _followDampingTime : 0.08f;
    }

    public float GetMaxFollowSpeed()
    {
        return _maxFollowSpeed > 0f ? _maxFollowSpeed : Mathf.Infinity;
    }
}
