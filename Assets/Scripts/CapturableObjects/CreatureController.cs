using UnityEngine;

public class CreatureController : MonoBehaviour, ICapturable
{
    [SerializeField] private CreatureCaptureData _data;

    private Vector2 _captureSmoothVelocity;
    private bool _isCaptured;

    public CreatureCaptureData CaptureData => _data;
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

[System.Serializable]
public struct CreatureCaptureData
{
    [Tooltip("대략적인 생물체의 크기를 결정\n"
            + "그물에 잡혔을 때 이 값을 고려하여 그물 내부에 위치하도록 조정됨")]
    [Min(0f)] public float radius;

    [Tooltip("그물에 잡혔을 때 그물에 부드럽게 끌려가는 관성 시간")]
    [Range(0f, 1f)] public float followDampingTime;
}
