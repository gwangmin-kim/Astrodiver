using PrimeTween;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CreatureController))]
public sealed class CreatureSpawnAnimation : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _duration = 0.5f;
    [SerializeField] private Ease _ease = Ease.OutBounce;

    private Tween _scaleTween;
    private Vector3 _targetScale;

    private void Awake()
    {
        _targetScale = transform.localScale;
    }

    private void OnValidate()
    {
        _targetScale = transform.localScale;
    }

    private void OnEnable()
    {
        Play();
    }

    private void OnDisable()
    {
        _scaleTween.Stop();
    }

    public void Play()
    {
        _scaleTween.Stop();
        transform.localScale = Vector3.zero;
        _scaleTween = Tween.Scale(transform, _targetScale, _duration, _ease);
    }
}
