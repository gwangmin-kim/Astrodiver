using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class MineralFloatageMovement : MonoBehaviour
{
    [Header("Initial Linear Motion")]
    [SerializeField, Min(0f)] private float _minSpeed = 0.15f;
    [SerializeField, Min(0f)] private float _maxSpeed = 0.4f;

    [Header("Initial Angular Motion")]
    [SerializeField, Min(0f)] private float _minAngularSpeed = 15f;
    [SerializeField, Min(0f)] private float _maxAngularSpeed = 45f;

    [Header("Initial Orientation")]
    [SerializeField] private float _minInitialRotation = -180f;
    [SerializeField] private float _maxInitialRotation = 180f;

    private Rigidbody2D _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        InitializeRandomRotation();
        InitializeRandomMotion();
    }

    private void InitializeRandomRotation()
    {
        _rigidbody.rotation = Random.Range(
            Mathf.Min(_minInitialRotation, _maxInitialRotation),
            Mathf.Max(_minInitialRotation, _maxInitialRotation));
    }

    private void InitializeRandomMotion()
    {
        float speed = Random.Range(
            Mathf.Min(_minSpeed, _maxSpeed),
            Mathf.Max(_minSpeed, _maxSpeed));

        float angle = Random.Range(0f, Mathf.PI * 2f);
        _rigidbody.linearVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

        float angularSpeed = Random.Range(
            Mathf.Min(_minAngularSpeed, _maxAngularSpeed),
            Mathf.Max(_minAngularSpeed, _maxAngularSpeed));

        _rigidbody.angularVelocity = angularSpeed * (Random.value < 0.5f ? -1f : 1f);
    }
}
