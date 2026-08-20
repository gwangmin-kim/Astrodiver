using PrimeTween;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class InteractionPromptSprite : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField, Min(0.01f)] private float _showDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float _hideDuration = 0.15f;
    [SerializeField, Min(0f)] private float _verticalOffset = 0.2f;
    [SerializeField] private Ease _showEase = Ease.OutQuad;
    [SerializeField] private Ease _hideEase = Ease.InQuad;

    private Vector3 _shownLocalPosition;
    private Tween _alphaTween;
    private Tween _positionTween;
    private bool _isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        StopTweens();
    }

    public void Show()
    {
        Initialize();
        StopTweens();
        transform.localPosition = HiddenLocalPosition;
        SetAlpha(0f);

        _alphaTween = Tween.Alpha(_spriteRenderer, 1f, _showDuration, _showEase);
        _positionTween = Tween.LocalPosition(transform, _shownLocalPosition, _showDuration, _showEase);
    }

    public void Hide()
    {
        Initialize();
        StopTweens();

        _alphaTween = Tween.Alpha(_spriteRenderer, 0f, _hideDuration, _hideEase);
        _positionTween = Tween.LocalPosition(transform, HiddenLocalPosition, _hideDuration, _hideEase);
    }

    private void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        _shownLocalPosition = transform.localPosition;
        SetAlpha(0f);
        transform.localPosition = HiddenLocalPosition;
        _isInitialized = true;
    }

    private Vector3 HiddenLocalPosition => _shownLocalPosition + Vector3.down * _verticalOffset;

    private void StopTweens()
    {
        _alphaTween.Stop();
        _positionTween.Stop();
    }

    private void SetAlpha(float alpha)
    {
        Color color = _spriteRenderer.color;
        color.a = alpha;
        _spriteRenderer.color = color;
    }
}
