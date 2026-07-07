using UnityEngine;
using PrimeTween;

public class NetAnimationController : MonoBehaviour
{
    [SerializeField] private Transform _netSprite;
    [SerializeField] private NetCaptureController _captureController;
    [SerializeField] private Ease _spreadEase;
    [SerializeField] private float _foldedRadius;

    private Tween _currentTween;

    private void Awake()
    {
        if (_netSprite == null)
        {
            _netSprite = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        if (_captureController == null)
        {
            _captureController = GetComponent<NetCaptureController>();
        }
    }

    private void OnEnable()
    {
        if (_captureController == null) return;

        _captureController.SpreadStarted += HandleSpreadStarted;
        _captureController.FoldStarted += HandleFoldStarted;
        _captureController.FoldReset += HandleFoldReset;
    }

    private void OnDisable()
    {
        if (_captureController != null)
        {
            _captureController.SpreadStarted -= HandleSpreadStarted;
            _captureController.FoldStarted -= HandleFoldStarted;
            _captureController.FoldReset -= HandleFoldReset;
        }

        _currentTween.Stop();
    }

    private void HandleSpreadStarted()
    {
        _currentTween.Stop();

        _currentTween = Tween.Scale(
                _netSprite,
                endValue: _captureController.Radius,
                duration: Mathf.Max(_captureController.SpreadDuration, 0.01f),
                ease: _spreadEase)
            .OnComplete(_captureController.CompleteSpread);
    }

    private void HandleFoldStarted()
    {
        _currentTween.Stop();

        _currentTween = Tween.Scale(
                _netSprite,
                endValue: _foldedRadius,
                duration: Mathf.Max(_captureController.FoldDuration, 0.01f))
            .OnComplete(_captureController.CompleteFold);
    }

    private void HandleFoldReset()
    {
        _currentTween.Stop();
        _netSprite.localScale = Vector3.one * _foldedRadius;
    }
}
