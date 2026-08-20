using UnityEngine;

/// <summary>
/// Controls the outline properties of its preconfigured SpriteRenderer instances
/// without cloning their shared material.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractableOutline : MonoBehaviour
{
    private static readonly int _outlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int _outlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int _outlineEnabledId = Shader.PropertyToID("_OutlineEnabled");
    private static readonly int _spriteUvMinMaxId = Shader.PropertyToID("_SpriteUVMinMax");
    private static readonly int _spriteLocalSizeId = Shader.PropertyToID("_SpriteLocalSize");

    [SerializeField] private SpriteRenderer[] _spriteRenderers;

    private MaterialPropertyBlock _propertyBlock;
    private bool _isHighlighted;

    private void Reset()
    {
        CollectSpriteRenderers();
    }

    private void Awake()
    {
        if (_spriteRenderers == null || _spriteRenderers.Length == 0)
        {
            CollectSpriteRenderers();
        }

        _propertyBlock = new MaterialPropertyBlock();
        ApplyProperties(Color.clear, 0f);
    }

    private void OnDisable()
    {
        _isHighlighted = false;
        ApplyProperties(Color.clear, 0f);
    }

    public void SetHighlighted(bool highlighted, Color outlineColor, float outlineWidth)
    {
        _isHighlighted = highlighted;
        ApplyProperties(outlineColor, outlineWidth);
    }

    private void CollectSpriteRenderers()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void ApplyProperties(Color outlineColor, float outlineWidth)
    {
        if (_spriteRenderers == null)
        {
            return;
        }

        _propertyBlock ??= new MaterialPropertyBlock();

        foreach (SpriteRenderer spriteRenderer in _spriteRenderers)
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(_outlineColorId, outlineColor);
            _propertyBlock.SetFloat(_outlineWidthId, outlineWidth);
            _propertyBlock.SetFloat(_outlineEnabledId, _isHighlighted ? 1f : 0f);
            ApplySpriteBounds(spriteRenderer);
            spriteRenderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private void ApplySpriteBounds(SpriteRenderer spriteRenderer)
    {
        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null)
        {
            _propertyBlock.SetVector(_spriteUvMinMaxId, new Vector4(0f, 0f, 1f, 1f));
            _propertyBlock.SetVector(_spriteLocalSizeId, Vector4.one);
            return;
        }

        Vector2[] uv = sprite.uv;
        Vector2 uvMin = uv[0];
        Vector2 uvMax = uv[0];
        for (int i = 1; i < uv.Length; i++)
        {
            uvMin = Vector2.Min(uvMin, uv[i]);
            uvMax = Vector2.Max(uvMax, uv[i]);
        }

        Vector3 localSize = spriteRenderer.localBounds.size;
        _propertyBlock.SetVector(_spriteUvMinMaxId, new Vector4(uvMin.x, uvMin.y, uvMax.x, uvMax.y));
        _propertyBlock.SetVector(_spriteLocalSizeId, new Vector4(
            Mathf.Max(localSize.x, 0.0001f),
            Mathf.Max(localSize.y, 0.0001f),
            0f,
            0f));
    }
}
