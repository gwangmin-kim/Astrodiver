using UnityEngine;

/// <summary>
/// Keeps an outline visible on the player sprites using the project's
/// Sprite Lit 8-Direction Outline material.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerOutline : MonoBehaviour
{
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int OutlineEnabledId = Shader.PropertyToID("_OutlineEnabled");
    private static readonly int SpriteUvMinMaxId = Shader.PropertyToID("_SpriteUVMinMax");
    private static readonly int SpriteLocalSizeId = Shader.PropertyToID("_SpriteLocalSize");

    [Header("Outline Appearance")]
    [SerializeField] private Color _outlineColor = Color.white;
    [SerializeField, Min(0f)] private float _outlineWidth = 2f;

    [Header("Rendering")]
    [Tooltip("Must use the Astrodiver/2D/Sprite Lit 8-Direction Outline shader.")]
    [SerializeField] private Material _outlineMaterial;
    [SerializeField] private SpriteRenderer[] _spriteRenderers;

    private MaterialPropertyBlock _propertyBlock;

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
        ApplyOutline();
    }

    private void OnEnable()
    {
        ApplyOutline();
    }

    /// <summary>
    /// Reapplies the current inspector settings. Call this after changing the
    /// appearance at runtime.
    /// </summary>
    public void ApplyOutline()
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

            if (_outlineMaterial != null)
            {
                spriteRenderer.sharedMaterial = _outlineMaterial;
            }

            spriteRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(OutlineColorId, _outlineColor);
            _propertyBlock.SetFloat(OutlineWidthId, _outlineWidth);
            _propertyBlock.SetFloat(OutlineEnabledId, 1f);
            ApplySpriteBounds(spriteRenderer);
            spriteRenderer.SetPropertyBlock(_propertyBlock);
        }
    }

    private void CollectSpriteRenderers()
    {
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void ApplySpriteBounds(SpriteRenderer spriteRenderer)
    {
        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null)
        {
            _propertyBlock.SetVector(SpriteUvMinMaxId, new Vector4(0f, 0f, 1f, 1f));
            _propertyBlock.SetVector(SpriteLocalSizeId, Vector4.one);
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
        _propertyBlock.SetVector(SpriteUvMinMaxId, new Vector4(uvMin.x, uvMin.y, uvMax.x, uvMax.y));
        _propertyBlock.SetVector(SpriteLocalSizeId, new Vector4(
            Mathf.Max(localSize.x, 0.0001f),
            Mathf.Max(localSize.y, 0.0001f),
            0f,
            0f));
    }
}
