using UnityEngine;

public class CreatureController : MonoBehaviour, ICapturable
{
    [SerializeField] private CaptureData _data;

    private Vector2 _captureSmoothVelocity;
    private bool _isCaptured;

    public CaptureData CaptureData => _data;
    public Vector2 Position => transform.position;
    public float Radius => _data.radius;

    public Vector2 BehaviorVector => Vector2.zero;


    private void OnDisable()
    {
        _isCaptured = false;
        _captureSmoothVelocity = Vector2.zero;
    }

    public bool CanBeCaptured(NetCaptureContext context)
    {
        return isActiveAndEnabled && !_isCaptured;
    }

    public void OnCaptureStarted(NetCaptureContext context)
    {
        _isCaptured = true;
        _captureSmoothVelocity = Vector2.zero;
    }

    public void OnCapturedMove(Vector2 targetPosition, Vector2 netVelocity, float deltaTime)
    {
        if (!_isCaptured) return;

        float dampingTime = _data.followDampingTime;

        Vector2 nextPosition = (dampingTime <= 0f)
            ? targetPosition
            : Vector2.SmoothDamp(
                transform.position,
                targetPosition,
                ref _captureSmoothVelocity,
                dampingTime,
                Mathf.Infinity,
                deltaTime);

        transform.position = nextPosition;
    }

    public void OnCaptureReleased(CaptureReleaseReason reason)
    {
        _isCaptured = false;
        _captureSmoothVelocity = Vector2.zero;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (isActiveAndEnabled)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _data.radius);
        }
    }
#endif
}
