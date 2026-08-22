using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public sealed class PlayerVisualTiltController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private Transform _visualRoot;

    [Header("Tilt Settings")]
    [Tooltip("X축 이동 입력의 절댓값이 이 값 이상일 때 몸을 기울입니다.")]
    [SerializeField][Min(0f)] private float _inputThreshold = 0.1f;
    [Tooltip("몸을 기울이는 Z축 회전 각도의 절댓값입니다. 양의 X 입력은 시계 방향으로 기웁니다.")]
    [SerializeField][Min(0f)] private float _rotationAngle = 10f;

    private Quaternion _defaultLocalRotation;
    private bool _hasDefaultLocalRotation;

    private void Awake()
    {
        ResolveReferences();
        CacheDefaultLocalRotation();
    }

    private void LateUpdate()
    {
        if (_inputHandler == null || _visualRoot == null)
        {
            return;
        }

        float horizontalInput = _inputHandler.MoveInput.x;
        float tiltAngle = Mathf.Abs(horizontalInput) >= _inputThreshold
            ? -Mathf.Sign(horizontalInput) * _rotationAngle
            : 0f;

        _visualRoot.localRotation = _defaultLocalRotation * Quaternion.Euler(0f, 0f, tiltAngle);
    }

    private void OnDisable()
    {
        if (_hasDefaultLocalRotation && _visualRoot != null)
        {
            _visualRoot.localRotation = _defaultLocalRotation;
        }
    }

    private void OnValidate()
    {
        _inputThreshold = Mathf.Abs(_inputThreshold);
        _rotationAngle = Mathf.Abs(_rotationAngle);
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (_inputHandler == null)
        {
            _inputHandler = GetComponent<PlayerInputHandler>();
        }

        if (_visualRoot == null)
        {
            Transform visualRoot = transform.Find("VisualRoot");
            if (visualRoot != null)
            {
                _visualRoot = visualRoot;
            }
        }
    }

    private void CacheDefaultLocalRotation()
    {
        if (_visualRoot == null)
        {
            return;
        }

        _defaultLocalRotation = _visualRoot.localRotation;
        _hasDefaultLocalRotation = true;
    }
}
