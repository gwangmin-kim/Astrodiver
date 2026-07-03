using System;
using UnityEngine;
using PrimeTween;

public class NetController : MonoBehaviour
{
    [SerializeField] private Ease _spreadEase;
    [SerializeField] private float _foldedRadius;

    private Tween _currentTween;

    public void Spread(NetSpreadData spreadData, Action onComplete = null)
    {
        _currentTween.Stop();

        _currentTween = Tween.Scale(
                transform,
                endValue: spreadData.radius,
                duration: spreadData.time,
                ease: _spreadEase)
            .OnComplete(onComplete);
    }

    public void Fold(float duration, Action onComplete = null)
    {
        _currentTween.Stop();

        _currentTween = Tween.Scale(
                transform,
                endValue: _foldedRadius,
                duration: duration)
            .OnComplete(onComplete);
    }

    public void ResetFolded()
    {
        _currentTween.Stop();
        transform.localScale = Vector3.one * _foldedRadius;
    }
}

public struct NetSpreadData
{
    public float radius;
    public float time;
}
