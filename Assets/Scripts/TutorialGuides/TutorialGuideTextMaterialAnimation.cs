using PrimeTween;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialGuideTextMaterialAnimation : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    [Header("Presentation Flash")]
    [SerializeField] private Color _flashOutlineColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float _flashOutlineWidth = 0.28f;
    [SerializeField] private Color _flashUnderlayColor = new(1f, 1f, 1f, 0.85f);
    [SerializeField, Range(-1f, 1f)] private float _flashUnderlayDilate = 0.18f;
    [SerializeField, Range(0f, 1f)] private float _flashUnderlaySoftness = 0.45f;

    private static readonly int _outlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int _outlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int _outlineSoftnessId = Shader.PropertyToID("_OutlineSoftness");
    private static readonly int _underlayColorId = Shader.PropertyToID("_UnderlayColor");
    private static readonly int _underlayOffsetXId = Shader.PropertyToID("_UnderlayOffsetX");
    private static readonly int _underlayOffsetYId = Shader.PropertyToID("_UnderlayOffsetY");
    private static readonly int _underlayDilateId = Shader.PropertyToID("_UnderlayDilate");
    private static readonly int _underlaySoftnessId = Shader.PropertyToID("_UnderlaySoftness");

    private Material _sharedMaterial;
    private Material _presentationMaterial;
    private Color _baseOutlineColor;
    private float _baseOutlineWidth;
    private float _baseOutlineSoftness;
    private Color _baseUnderlayColor;
    private float _baseUnderlayOffsetX;
    private float _baseUnderlayOffsetY;
    private float _baseUnderlayDilate;
    private float _baseUnderlaySoftness;

    private void Awake()
    {
        if (_text == null)
        {
            _text = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnDisable()
    {
        ReleasePresentationMaterial();
    }

    private void OnDestroy()
    {
        ReleasePresentationMaterial();
    }

    public bool BeginPresentationFlash()
    {
        ReleasePresentationMaterial();
        if (_text == null || _text.fontSharedMaterial == null)
        {
            return false;
        }

        _sharedMaterial = _text.fontSharedMaterial;
        _presentationMaterial = new Material(_sharedMaterial)
        {
            hideFlags = HideFlags.DontSave,
        };

        if (!HasFlashProperties(_presentationMaterial))
        {
            ReleasePresentationMaterial();
            return false;
        }

        CacheBaseProperties();
        _text.fontSharedMaterial = _presentationMaterial;
        ApplyFlash(1f);
        return true;
    }

    public Tween CreatePresentationFadeTween(float duration)
    {
        return Tween.Custom(
            this, 1f, 0f, duration,
            (_, intensity) => ApplyFlash(intensity),
            ease: Ease.InOutCubic);
    }

    public void ReleasePresentationMaterial()
    {
        if (_presentationMaterial == null)
        {
            return;
        }

        ApplyFlash(0f);
        if (_text != null && _text.fontSharedMaterial == _presentationMaterial)
        {
            _text.fontSharedMaterial = _sharedMaterial;
        }

        Destroy(_presentationMaterial);
        _presentationMaterial = null;
        _sharedMaterial = null;
    }

    private void CacheBaseProperties()
    {
        _baseOutlineColor = _presentationMaterial.GetColor(_outlineColorId);
        _baseOutlineWidth = _presentationMaterial.GetFloat(_outlineWidthId);
        _baseOutlineSoftness = _presentationMaterial.GetFloat(_outlineSoftnessId);
        _baseUnderlayColor = _presentationMaterial.GetColor(_underlayColorId);
        _baseUnderlayOffsetX = _presentationMaterial.GetFloat(_underlayOffsetXId);
        _baseUnderlayOffsetY = _presentationMaterial.GetFloat(_underlayOffsetYId);
        _baseUnderlayDilate = _presentationMaterial.GetFloat(_underlayDilateId);
        _baseUnderlaySoftness = _presentationMaterial.GetFloat(_underlaySoftnessId);
    }

    private void ApplyFlash(float intensity)
    {
        if (_presentationMaterial == null)
        {
            return;
        }

        _presentationMaterial.SetColor(
            _outlineColorId, Color.Lerp(_baseOutlineColor, _flashOutlineColor, intensity));
        _presentationMaterial.SetFloat(
            _outlineWidthId, Mathf.Lerp(_baseOutlineWidth, _flashOutlineWidth, intensity));
        _presentationMaterial.SetFloat(
            _outlineSoftnessId, Mathf.Lerp(_baseOutlineSoftness, 0f, intensity));
        _presentationMaterial.SetColor(
            _underlayColorId,
            Color.Lerp(_baseUnderlayColor, _flashUnderlayColor, intensity));
        _presentationMaterial.SetFloat(
            _underlayOffsetXId, Mathf.Lerp(_baseUnderlayOffsetX, 0f, intensity));
        _presentationMaterial.SetFloat(
            _underlayOffsetYId, Mathf.Lerp(_baseUnderlayOffsetY, 0f, intensity));
        _presentationMaterial.SetFloat(
            _underlayDilateId,
            Mathf.Lerp(_baseUnderlayDilate, _flashUnderlayDilate, intensity));
        _presentationMaterial.SetFloat(
            _underlaySoftnessId,
            Mathf.Lerp(_baseUnderlaySoftness, _flashUnderlaySoftness, intensity));
    }

    private static bool HasFlashProperties(Material material)
    {
        return material.HasProperty(_outlineColorId)
            && material.HasProperty(_outlineWidthId)
            && material.HasProperty(_outlineSoftnessId)
            && material.HasProperty(_underlayColorId)
            && material.HasProperty(_underlayOffsetXId)
            && material.HasProperty(_underlayOffsetYId)
            && material.HasProperty(_underlayDilateId)
            && material.HasProperty(_underlaySoftnessId);
    }
}
