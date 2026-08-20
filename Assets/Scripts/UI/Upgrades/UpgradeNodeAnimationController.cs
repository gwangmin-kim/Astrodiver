using PrimeTween;
using UnityEngine;

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

    [Header("Appearance")]
    [SerializeField, Range(0.01f, 1f)] private float _appearStartScale = 0.5f;
    [SerializeField, Min(0.01f)] private float _appearDuration = 0.28f;
    [SerializeField] private Ease _appearEase = Ease.OutBack;

    [Header("Focus")]
    [SerializeField, Min(1f)] private float _focusScaleMultiplier = 1.2f;
    [SerializeField, Min(0.01f)] private float _focusDuration = 0.16f;
    [SerializeField] private Ease _focusEase = Ease.OutBack;

    [Header("Purchase Success")]
    [SerializeField, Min(0.01f)] private float _successRotationDuration = 0.34f;
    [SerializeField] private Ease _successRotationEase = Ease.OutBack;

    [Header("Purchase Failure")]
    [SerializeField, Range(1f, 45f)] private float _failureAngle = 5f;
    [SerializeField, Min(1)] private int _failureOscillationCount = 3;
    [SerializeField, Min(0.01f)] private float _failureHalfSwingDuration = 0.07f;
    [SerializeField, Min(0.01f)] private float _failureReturnDuration = 0.12f;
    [SerializeField] private Ease _failureEase = Ease.OutBack;

    private Tween _scaleTween;
    private Tween _rotationTween;
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
        _animatedTransform.localScale = _baseScale * _appearStartScale;
        _scaleTween = Tween.Scale(
            _animatedTransform,
            _baseScale,
            _appearDuration,
            _appearEase);
    }

    /// <summary>Plays one fast clockwise rotation after a successful purchase.</summary>
    public void PlayPurchaseSucceeded()
    {
        if (!CanPlayInteractionAnimation())
        {
            RestoreInteractionBaseline();
            return;
        }

        _rotationTween.Stop();
        _failureSequence.Stop();
        _animatedTransform.localRotation = _baseLocalRotation;
        Vector3 baseEuler = _baseLocalRotation.eulerAngles;
        _rotationTween = Tween.LocalEulerAngles(
            _animatedTransform,
            baseEuler,
            baseEuler + new Vector3(0f, 0f, -360f),
            _successRotationDuration,
            _successRotationEase);
    }

    /// <summary>Shakes between +/- the configured angle, then restores its original rotation.</summary>
    public void PlayPurchaseFailed()
    {
        if (!CanPlayInteractionAnimation())
        {
            RestoreInteractionBaseline();
            return;
        }

        _rotationTween.Stop();
        _failureSequence.Stop();
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
        _rotationTween.Stop();
        _failureSequence.Stop();

        if (_animatedTransform == null)
        {
            return;
        }

        _animatedTransform.localScale = _baseScale;
        _animatedTransform.localRotation = _baseLocalRotation;
    }

    private void StopTweensAndRestore()
    {
        RestoreInteractionBaseline();
    }
}
