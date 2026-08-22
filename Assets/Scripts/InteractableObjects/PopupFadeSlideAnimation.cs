using PrimeTween;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class PopupFadeSlideAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform _popupTransform;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField, Min(0.01f)] private float _showDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float _hideDuration = 0.15f;
    [SerializeField, Min(0f)] private float _verticalOffset = 0.2f;
    [SerializeField] private Ease _showEase = Ease.OutQuad;
    [SerializeField] private Ease _hideEase = Ease.InQuad;

    private Vector2 _shownPosition;
    private Tween _alphaTween;
    private Tween _positionTween;
    private bool _isInitialized;

    public bool IsShown { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        IsShown = false;
        StopTweens();
    }

    public void Show()
    {
        Initialize();
        if (IsShown)
        {
            return;
        }

        IsShown = true;
        StopTweens();
        gameObject.SetActive(true);
        _popupTransform.anchoredPosition = HiddenPosition;
        _canvasGroup.alpha = 0f;

        _alphaTween = Tween.Alpha(
            _canvasGroup,
            1f,
            _showDuration,
            _showEase);
        _positionTween = Tween.UIAnchoredPosition(
            _popupTransform,
            _shownPosition,
            _showDuration,
            _showEase);
    }

    public void Hide()
    {
        Initialize();
        if (!IsShown)
        {
            HideImmediate();
            return;
        }

        IsShown = false;
        StopTweens();

        _alphaTween = Tween.Alpha(
            _canvasGroup,
            0f,
            _hideDuration,
            _hideEase);
        _positionTween = Tween.UIAnchoredPosition(
                _popupTransform,
                HiddenPosition,
                _hideDuration,
                _hideEase)
            .OnComplete(CompleteHide);
    }

    public void HideImmediate()
    {
        Initialize();
        IsShown = false;
        StopTweens();
        _canvasGroup.alpha = 0f;
        _popupTransform.anchoredPosition = HiddenPosition;
        gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_popupTransform == null)
        {
            _popupTransform = (RectTransform)transform;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        _shownPosition = _popupTransform.anchoredPosition;
        _isInitialized = true;
    }

    private Vector2 HiddenPosition =>
        _shownPosition + Vector2.down * _verticalOffset;

    private void CompleteHide()
    {
        if (!IsShown)
        {
            gameObject.SetActive(false);
        }
    }

    private void StopTweens()
    {
        _alphaTween.Stop();
        _positionTween.Stop();
    }
}
