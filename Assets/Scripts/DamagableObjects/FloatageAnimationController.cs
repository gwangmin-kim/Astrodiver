using PrimeTween;
using UnityEngine;
[DisallowMultipleComponent]

/// <summary>
/// Owns visual-only floatage feedback and reports respawn completion. Gameplay
/// state and collider timing remain owned by <see cref="FloatageController"/>.
/// </summary>
[RequireComponent(typeof(FloatageController))]

public sealed class FloatageAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloatageController _floatage;
    [SerializeField] private Transform _visual;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Respawn")]
    [SerializeField, Range(0.01f, 1f)] private float _respawnStartScale = 0.5f;
    [SerializeField, Min(0.01f)] private float _respawnDuration = 1.5f;
    [SerializeField] private Ease _respawnEase = Ease.OutQuad;

    [Header("Activation")]
    [SerializeField, Min(1f)] private float _activationBounceScaleMultiplier = 1.2f;
    [SerializeField, Min(1f)] private float _activationGlowBrightnessMultiplier = 5f;
    [SerializeField, Min(0.01f)] private float _activationGlowOutDuration = 0.5f;

    [Header("Hit")]
    [SerializeField, Min(1f)] private float _hitBounceScaleMultiplier = 1.1f;
    [SerializeField, Min(1f)] private float _hitGlowBrightnessMultiplier = 3f;
    [SerializeField, Min(0.01f)] private float _hitGlowOutDuration = 0.16f;

    [Header("Shared Bounce And Glow Timing")]
    [SerializeField, Min(0.01f)] private float _bounceOutDuration = 0.08f;
    [SerializeField, Min(0.01f)] private float _bounceReturnDuration = 0.12f;
    [SerializeField] private Ease _bounceEase = Ease.OutQuad;
    [SerializeField, Min(0.01f)] private float _glowInDuration = 0.05f;
    [SerializeField] private Ease _glowEase = Ease.OutQuad;

    private Tween _respawnScaleTween;
    private Tween _respawnAlphaTween;
    private Sequence _bounceSequence;
    private Sequence _glowSequence;
    private Vector3 _baseVisualScale;
    private Color _baseColor;

    /// <summary>Published when the respawn appearance animation reaches its end.</summary>
    public event System.Action RespawnPresentationCompleted;

    private void Initialize()
    {
        if (_floatage == null)
        {
            _floatage = GetComponent<FloatageController>();
        }

        if (_visual == null)
        {
            _visual = transform.Find("Visual");
        }

        if (_spriteRenderer == null && _visual != null)
        {
            _spriteRenderer = _visual.GetComponent<SpriteRenderer>();
        }

        if (_visual != null)
        {
            _baseVisualScale = _visual.localScale;
        }

        if (_spriteRenderer != null)
        {
            _baseColor = _spriteRenderer.color;
        }
    }

    private void OnValidate()
    {
        Initialize();
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (_floatage == null)
        {
            return;
        }

        _floatage.Spawned += HandleSpawned;
        _floatage.Activated += HandleActivated;
        _floatage.Damaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (_floatage != null)
        {
            _floatage.Spawned -= HandleSpawned;
            _floatage.Activated -= HandleActivated;
            _floatage.Damaged -= HandleDamaged;
        }

        StopTweensAndRestore();
    }

    /// <summary>Plays the visual-only scale and fade-in used for a respawn.</summary>
    public void PlayRespawn()
    {
        StopTweensAndRestore();
        if (_visual == null || _spriteRenderer == null)
        {
            return;
        }

        _visual.localScale = _baseVisualScale * _respawnStartScale;
        _spriteRenderer.color = WithAlpha(_baseColor, 0f);
        _respawnScaleTween = Tween.Scale(
            _visual,
            _baseVisualScale,
            _respawnDuration,
            _respawnEase);
        _respawnAlphaTween = Tween.Alpha(
                _spriteRenderer,
                _baseColor.a,
                _respawnDuration,
                _respawnEase)
            .OnComplete(CompleteRespawnPresentation);
    }

    /// <summary>Plays the activation bounce and HDR color flash.</summary>
    public void PlayActivation()
    {
        PlayBounceAndGlow(
            _activationBounceScaleMultiplier,
            _activationGlowBrightnessMultiplier,
            _activationGlowOutDuration);
    }

    /// <summary>Plays the hit bounce and HDR color flash.</summary>
    public void PlayHit()
    {
        PlayBounceAndGlow(
            _hitBounceScaleMultiplier,
            _hitGlowBrightnessMultiplier,
            _hitGlowOutDuration);
    }

    private void HandleSpawned()
    {
        PlayRespawn();
    }

    private void HandleActivated()
    {
        PlayActivation();
    }

    private void CompleteRespawnPresentation()
    {
        RespawnPresentationCompleted?.Invoke();
    }

    private void HandleDamaged()
    {
        PlayHit();
    }

    private void PlayBounceAndGlow(
        float bounceScaleMultiplier,
        float glowBrightnessMultiplier,
        float glowOutDuration)
    {
        _respawnScaleTween.Stop();
        _respawnAlphaTween.Stop();
        _bounceSequence.Stop();
        _glowSequence.Stop();

        if (_visual == null || _spriteRenderer == null)
        {
            return;
        }

        _visual.localScale = _baseVisualScale;
        _spriteRenderer.color = _baseColor;
        _bounceSequence = Sequence.Create()
            .Chain(Tween.Scale(
                _visual,
                _baseVisualScale * bounceScaleMultiplier,
                _bounceOutDuration,
                _bounceEase))
            .Chain(Tween.Scale(
                _visual,
                _baseVisualScale,
                _bounceReturnDuration,
                _bounceEase));

        Color glowColor = new(
            _baseColor.r * glowBrightnessMultiplier,
            _baseColor.g * glowBrightnessMultiplier,
            _baseColor.b * glowBrightnessMultiplier,
            _baseColor.a);
        _glowSequence = Sequence.Create()
            .Chain(Tween.Color(
                _spriteRenderer,
                _baseColor,
                glowColor,
                _glowInDuration,
                _glowEase))
            .Chain(Tween.Color(
                _spriteRenderer,
                glowColor,
                _baseColor,
                glowOutDuration,
                _glowEase));
    }

    private void StopTweensAndRestore()
    {
        _respawnScaleTween.Stop();
        _respawnAlphaTween.Stop();
        _bounceSequence.Stop();
        _glowSequence.Stop();

        if (_visual != null)
        {
            _visual.localScale = _baseVisualScale;
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = _baseColor;
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
