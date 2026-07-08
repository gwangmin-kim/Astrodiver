using System;
using UnityEngine;
using PrimeTween;

public class CaptureAnimationController : MonoBehaviour
{
    [SerializeField][Min(0.01f)] private float _collectSpeed = 8f;
    [SerializeField][Min(0.01f)] private float _minCollectDuration = 0.1f;
    [SerializeField] private Ease _collectEase = Ease.InQuad;

    private Collider2D[] _colliders;
    private Rigidbody2D _body;
    private Tween _collectTween;

    private void Awake()
    {
        _colliders = GetComponentsInChildren<Collider2D>();
        _body = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        _collectTween.Stop();
    }

    public void PlayCollectTo(Transform target, Action onComplete)
    {
        _collectTween.Stop();
        SetPhysicsEnabled(false);

        if (target == null)
        {
            CompleteCollect(onComplete);
            return;
        }

        Vector3 targetPosition = target.position;
        targetPosition.z = transform.position.z;

        float distance = Vector3.Distance(transform.position, targetPosition);
        float duration = Mathf.Max(_minCollectDuration, distance / Mathf.Max(0.01f, _collectSpeed));

        _collectTween = Tween.Position(
                transform,
                endValue: targetPosition,
                duration: duration,
                ease: _collectEase)
            .OnComplete(() => CompleteCollect(onComplete));
    }

    private void CompleteCollect(Action onComplete)
    {
        onComplete?.Invoke();
        gameObject.SetActive(false);
    }

    private void SetPhysicsEnabled(bool isEnabled)
    {
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                {
                    _colliders[i].enabled = isEnabled;
                }
            }
        }

        if (_body != null)
        {
            _body.simulated = isEnabled;
        }
    }
}
