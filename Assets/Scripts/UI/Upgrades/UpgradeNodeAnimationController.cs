using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owns presentation-only motion for an <see cref="UpgradeNodeUI"/>.
/// Attach this component to an upgrade node (or its visual child) and keep
/// gameplay/purchase logic in <see cref="UpgradeTreeUI"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class UpgradeNodeAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UpgradeNodeUI _node;
    [SerializeField] private Transform _animatedTransform;
    [SerializeField] private Image _successFlashOverlay;

    [Header("Appearance")]
    [SerializeField, Range(0.01f, 1f)] private float _appearStartScale = 0.5f;
    [SerializeField, Min(0.01f)] private float _appearDuration = 0.28f;
    [SerializeField] private Ease _appearEase = Ease.OutBack;

    [Header("Focus")]
    [SerializeField, Min(1f)] private float _focusScaleMultiplier = 1.2f;
    [SerializeField, Min(0.01f)] private float _focusDuration = 0.16f;
    [SerializeField] private Ease _focusEase = Ease.OutBack;

    [Header("Purchase Success")]
    [SerializeField, Min(1f)] private float _successScaleMultiplier = 1.5f;
    [SerializeField, Min(0.01f)] private float _successExpandDuration = 0.08f;
    [SerializeField] private Ease _successExpandEase = Ease.OutQuad;
    [SerializeField, Min(0.01f)] private float _successShrinkDuration = 0.28f;
    [SerializeField] private Ease _successShrinkEase = Ease.InOutQuad;
    [SerializeField, Range(0f, 1f)] private float _successFlashAlpha = 0.65f;

    [Header("Purchase Failure")]
    [SerializeField, Range(1f, 45f)] private float _failureAngle = 5f;
    [SerializeField, Min(1)] private int _failureOscillationCount = 3;
    [SerializeField, Min(0.01f)] private float _failureHalfSwingDuration = 0.07f;
    [SerializeField, Min(0.01f)] private float _failureReturnDuration = 0.12f;
    [SerializeField] private Ease _failureEase = Ease.OutBack;

    private Tween _scaleTween;
    private Sequence _successScaleSequence;
    private Sequence _failureSequence;
    private Vector3 _baseScale;
    private Quaternion _baseLocalRotation;

    private void Reset()
    {
        _node = GetComponent<UpgradeNodeUI>();
        _animatedTransform = transform;
    }

    private void Awake()
    {
        if (_node == null)
        {
            _node = GetComponent<UpgradeNodeUI>();
        }

        if (_animatedTransform == null)
        {
            _animatedTransform = transform;
        }

        _baseScale = _animatedTransform.localScale;
        _baseLocalRotation = _animatedTransform.localRotation;
    }

    private void OnEnable()
    {
        Subscribe();
        PlayAppearance();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopTweensAndRestore();
    }

    /// <summary>Plays the scale-in animation used when this node becomes visible.</summary>
    public void PlayAppearance()
    {
        if (_animatedTransform == null)
        {
            return;
        }

        _scaleTween.Stop();
        _successScaleSequence.Stop();
        SetSuccessFlashAlpha(0f);
        _animatedTransform.localScale = _baseScale * _appearStartScale;
        _scaleTween = Tween.Scale(
            _animatedTransform,
            _baseScale,
            _appearDuration,
            _appearEase);
    }

    /// <summary>Plays a fast scale bounce after a successful purchase.</summary>
    public void PlayPurchaseSucceeded()
    {
        if (!CanPlayInteractionAnimation())
        {
            RestoreInteractionBaseline();
            return;
        }

        _scaleTween.Stop();
        _successScaleSequence.Stop();
        _failureSequence.Stop();
        _animatedTransform.localRotation = _baseLocalRotation;
        _animatedTransform.localScale = _baseScale;
        SetSuccessFlashAlpha(0f);

        Vector3 bounceScale = _baseScale * _successScaleMultiplier;
        Vector3 endScale = _node.IsFocused
            ? _baseScale * _focusScaleMultiplier
            : _baseScale;
        Sequence sequence = Sequence.Create();
        sequence.Group(Tween.Scale(
                _animatedTransform,
                _baseScale,
                bounceScale,
                _successExpandDuration,
                _successExpandEase));
        if (_successFlashOverlay != null)
        {
            sequence.Group(Tween.Alpha(
                _successFlashOverlay,
                _successFlashAlpha,
                _successExpandDuration,
                _successExpandEase));
        }

        sequence.Chain(Tween.Scale(
                _animatedTransform,
                bounceScale,
                endScale,
                _successShrinkDuration,
                _successShrinkEase));
        if (_successFlashOverlay != null)
        {
            sequence.Group(Tween.Alpha(
                _successFlashOverlay,
                0f,
                _successShrinkDuration,
                _successShrinkEase));
        }

        _successScaleSequence = sequence;
    }

    /// <summary>Shakes between +/- the configured angle, then restores its original rotation.</summary>
    public void PlayPurchaseFailed()
    {
        if (!CanPlayInteractionAnimation())
        {
            RestoreInteractionBaseline();
            return;
        }

        _failureSequence.Stop();
        _successScaleSequence.Stop();
        SetSuccessFlashAlpha(0f);
        _animatedTransform.localRotation = _baseLocalRotation;

        Vector3 baseEuler = _baseLocalRotation.eulerAngles;
        Vector3 left = baseEuler + new Vector3(0f, 0f, _failureAngle);
        Vector3 right = baseEuler + new Vector3(0f, 0f, -_failureAngle);
        Sequence sequence = Sequence.Create();
        Vector3 current = baseEuler;
        int swingCount = _failureOscillationCount * 2;
        for (int i = 0; i < swingCount; i++)
        {
            Vector3 target = i % 2 == 0 ? left : right;
            sequence.Chain(Tween.LocalEulerAngles(
                _animatedTransform,
                current,
                target,
                _failureHalfSwingDuration,
                _failureEase));
            current = target;
        }

        _failureSequence = sequence.Chain(Tween.LocalEulerAngles(
            _animatedTransform,
            current,
            baseEuler,
            _failureReturnDuration,
            _failureEase));
    }

    private void Subscribe()
    {
        if (_node == null)
        {
            return;
        }

        _node.Focused += HandleFocused;
        _node.Unfocused += HandleUnfocused;
        _node.PurchaseResolved += HandlePurchaseResolved;
    }

    private void Unsubscribe()
    {
        if (_node == null)
        {
            return;
        }

        _node.Focused -= HandleFocused;
        _node.Unfocused -= HandleUnfocused;
        _node.PurchaseResolved -= HandlePurchaseResolved;
    }

    private void HandleFocused(UpgradeNodeUI node)
    {
        if (!CanPlayInteractionAnimation())
        {
            RestoreInteractionBaseline();
            return;
        }

        PlayFocusScale(true);
    }

    private void HandleUnfocused(UpgradeNodeUI node)
    {
        if (!CanPlayInteractionAnimation())
        {
            RestoreInteractionBaseline();
            return;
        }

        PlayFocusScale(false);
    }

    private void HandlePurchaseResolved(
        UpgradeNodeUI node,
        UpgradePurchaseResult result)
    {
        if (result.Succeeded)
        {
            PlayPurchaseSucceeded();
        }
        else
        {
            PlayPurchaseFailed();
        }
    }

    private void PlayFocusScale(bool focused)
    {
        if (_animatedTransform == null)
        {
            return;
        }

        _scaleTween.Stop();
        _successScaleSequence.Stop();
        SetSuccessFlashAlpha(0f);
        _scaleTween = Tween.Scale(
            _animatedTransform,
            focused ? _baseScale * _focusScaleMultiplier : _baseScale,
            _focusDuration,
            _focusEase);
    }

    private bool CanPlayInteractionAnimation()
    {
        return _animatedTransform != null &&
            _node != null &&
            _node.VisualState != UpgradeNodeVisualState.Completed;
    }

    private void RestoreInteractionBaseline()
    {
        _scaleTween.Stop();
        _successScaleSequence.Stop();
        _failureSequence.Stop();
        SetSuccessFlashAlpha(0f);

        if (_animatedTransform == null)
        {
            return;
        }

        _animatedTransform.localScale = _baseScale;
        _animatedTransform.localRotation = _baseLocalRotation;
    }

    private void SetSuccessFlashAlpha(float alpha)
    {
        if (_successFlashOverlay == null)
        {
            return;
        }

        Color color = _successFlashOverlay.color;
        color.a = alpha;
        _successFlashOverlay.color = color;
    }

    private void StopTweensAndRestore()
    {
        RestoreInteractionBaseline();
    }
}
