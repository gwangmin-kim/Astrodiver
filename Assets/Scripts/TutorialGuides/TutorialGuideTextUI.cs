using System.Collections;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialGuideTextUI : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TutorialGuideTextMaterialAnimation _materialAnimation;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float _popDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float _moveDuration = 0.55f;
    [SerializeField, Min(0.01f)] private float _completionDuration = 0.2f;
    [SerializeField, Min(1f)] private float _popPeakScale = 2f;

    private Sequence _presentationSequence;
    private Vector3 _baseScale;
    private bool _initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    public void Bind(string text)
    {
        Initialize();
        if (_text == null)
        {
            return;
        }

        _text.text = text;
    }

    public IEnumerator PlayPresentation(
        RectTransform guideListRoot,
        RectTransform centerPresentationRoot,
        int siblingIndex)
    {
        Initialize();
        StopAnimation();
        bool hasPresentationFlash = _materialAnimation != null
            && _materialAnimation.BeginPresentationFlash();

        RectTransform itemTransform = (RectTransform)transform;
        itemTransform.SetParent(guideListRoot, false);
        itemTransform.SetSiblingIndex(siblingIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(guideListRoot);
        Vector3 listWorldPosition = itemTransform.position;

        itemTransform.SetParent(centerPresentationRoot, false);
        itemTransform.position = GetPresentationWorldPosition(
            itemTransform, centerPresentationRoot);
        itemTransform.localScale = _baseScale;
        _canvasGroup.alpha = 0f;

        _presentationSequence = Sequence.Create()
            .Group(Tween.Alpha(
                _canvasGroup, 1f, _popDuration, Ease.OutCubic))
            .Group(Tween.Scale(
                itemTransform, _baseScale * _popPeakScale,
                _popDuration, Ease.OutCubic))
            .Chain(Tween.Position(
                itemTransform, listWorldPosition, _moveDuration, Ease.InOutCubic))
            .Group(Tween.Scale(
                itemTransform, _baseScale, _moveDuration, Ease.InOutCubic));
        if (hasPresentationFlash)
        {
            _presentationSequence.Group(
                _materialAnimation.CreatePresentationFadeTween(_moveDuration));
        }

        yield return _presentationSequence.ToYieldInstruction();
        if (_materialAnimation != null)
        {
            _materialAnimation.ReleasePresentationMaterial();
        }

        itemTransform.SetParent(guideListRoot, true);
        itemTransform.SetSiblingIndex(siblingIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(guideListRoot);
        itemTransform.localScale = _baseScale;
        _canvasGroup.alpha = 1f;
    }

    public void PlaceInListImmediately(
        RectTransform guideListRoot,
        int siblingIndex)
    {
        Initialize();
        StopAnimation();

        RectTransform itemTransform = (RectTransform)transform;
        itemTransform.SetParent(guideListRoot, true);
        itemTransform.SetSiblingIndex(siblingIndex);
        itemTransform.localScale = _baseScale;
        _canvasGroup.alpha = 1f;
    }

    public IEnumerator PlayCompletionAnimation()
    {
        Initialize();
        StopAnimation();
        _presentationSequence = Sequence.Create()
            .Group(Tween.Alpha(
                _canvasGroup, 0f, _completionDuration, Ease.InQuad))
            .Group(Tween.Scale(
                transform, _baseScale * 0.82f,
                _completionDuration, Ease.InQuad));
        yield return _presentationSequence.ToYieldInstruction();
    }

    public void StopAnimation()
    {
        _presentationSequence.Stop();
        if (_materialAnimation != null)
        {
            _materialAnimation.ReleasePresentationMaterial();
        }
    }

    private void Initialize()
    {
        if (_initialized) return;
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_text == null)
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        if (_materialAnimation == null)
        {
            _materialAnimation = GetComponent<TutorialGuideTextMaterialAnimation>();
        }

        _baseScale = transform.localScale;
        _initialized = true;
    }

    private static Vector3 GetPresentationWorldPosition(
        RectTransform itemTransform,
        RectTransform centerPresentationRoot)
    {
        Vector3 centerWorldPosition = centerPresentationRoot.TransformPoint(
            centerPresentationRoot.rect.center);
        Vector3 itemCenterOffset = itemTransform.TransformVector(
            itemTransform.rect.center);
        return centerWorldPosition - itemCenterOffset;
    }

}
