using UnityEngine;

/// <summary>
/// Renders the plasma gun's beam independently from hit detection and damage.
/// The preconfigured child SpriteRenderer uses a sliced capsule sprite, so its
/// round end caps keep their size while only the centre section changes length.
/// </summary>
public sealed class PlasmaGunLaserVisual : MonoBehaviour
{
    private const float ParticleShapePadding = 0.5f;
    private static readonly int _innerColorId = Shader.PropertyToID("_InnerColor");
    private static readonly int _middleColorId = Shader.PropertyToID("_MiddleColor");
    private static readonly int _outlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int _glowIntensityId = Shader.PropertyToID("_GlowIntensity");

    [Header("Required References")]
    [SerializeField] private SpriteRenderer _laserRenderer;

    [Header("Sprite Settings")]
    [SerializeField, Min(0.01f)] private float _beamWidth = 0.32f;

    [Header("Color Settings")]
    [SerializeField] private Color _innerColor = Color.white;
    [SerializeField] private Color _middleColor = new(1f, 0.6933962f, 0.97642165f, 1f);
    [SerializeField] private Color _outlineColor = new(1f, 0f, 0.65657973f, 1f);
    [SerializeField, Min(0f)] private float _glowIntensity = 2f;

    [Header("Particle Settings")]
    [SerializeField, Min(0f)] private float _particlesPerUnitLength = 12f;

    private Vector3 _defaultLocalScale;
    private Material _laserMaterial;
    private ParticleSystem _laserParticles;

    private void Awake()
    {
        if (_laserRenderer != null)
        {
            _defaultLocalScale = _laserRenderer.transform.localScale;
            _laserMaterial = _laserRenderer.material;
            _laserParticles = _laserRenderer.GetComponent<ParticleSystem>();
            ApplyColors();
        }
    }

    /// <summary>
    /// Displays this beam segment between two world-space positions.
    /// </summary>
    public void Show(Vector2 start, Vector2 end)
    {
        if (_laserRenderer == null || _laserRenderer.sprite == null)
        {
            Debug.LogWarning("Plasma laser visual needs a preconfigured SpriteRenderer.", this);
            return;
        }

        // Beam prefabs are stored inactive. Activating before configuration runs
        // Awake for a newly-instantiated chain beam and caches its material/scale.
        _laserRenderer.gameObject.SetActive(true);
        ConfigureBeam(start, end);
    }

    public void Hide()
    {
        if (_laserRenderer != null)
        {
            _laserRenderer.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Hide();
    }

    public void SetColors(Color inner, Color middle, Color outline)
    {
        _innerColor = inner;
        _middleColor = middle;
        _outlineColor = outline;
        ApplyColors();
    }

    private void ConfigureBeam(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        float length = delta.magnitude;
        if (length <= Mathf.Epsilon)
        {
            Hide();
            return;
        }

        float nativeHeight = _laserRenderer.sprite.bounds.size.y;
        float scale = _beamWidth / nativeHeight;
        float totalWidth = Mathf.Max(length + _beamWidth, _beamWidth);

        // The sprite is horizontal. Positioning its midpoint at the segment midpoint
        // puts the centres of its two circular caps exactly at start and end.
        _laserRenderer.transform.SetPositionAndRotation(
            (start + end) * 0.5f,
            Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg));
        _laserRenderer.transform.localScale = _defaultLocalScale * scale;

        // Keep the sliced sprite at its native height. Width is converted to local
        // units before uniform scaling so the cap circles are never stretched.
        _laserRenderer.size = new Vector2(totalWidth / scale, nativeHeight);
        UpdateParticleShape(totalWidth, _beamWidth, scale);
        UpdateParticleEmission(length);
        _laserRenderer.gameObject.SetActive(true);
    }

    private void ApplyColors()
    {
        if (_laserMaterial != null)
        {
            _laserMaterial.SetColor(_innerColorId, _innerColor);
            _laserMaterial.SetColor(_middleColorId, _middleColor);
            _laserMaterial.SetColor(_outlineColorId, _outlineColor);
            _laserMaterial.SetFloat(_glowIntensityId, _glowIntensity);
        }

        if (_laserParticles != null)
        {
            ParticleSystem.MainModule main = _laserParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                ToHdr(_innerColor),
                ToHdr(_outlineColor));
        }
    }

    private void UpdateParticleShape(float width, float height, float transformScale)
    {
        if (_laserParticles == null) return;

        // Shape dimensions are local to the laser transform. Convert the desired
        // world-space beam bounds back to local units before that transform's
        // uniform height scale is applied.
        ParticleSystem.ShapeModule shape = _laserParticles.shape;
        shape.scale = new Vector3(
            (width + ParticleShapePadding) / transformScale,
            (height + ParticleShapePadding) / transformScale,
            1f);
    }

    private void UpdateParticleEmission(float beamLength)
    {
        if (_laserParticles == null) return;

        ParticleSystem.EmissionModule emission = _laserParticles.emission;
        emission.rateOverTime = beamLength * _particlesPerUnitLength;
    }

    private Color ToHdr(Color color)
    {
        return new Color(
            color.r * _glowIntensity,
            color.g * _glowIntensity,
            color.b * _glowIntensity,
            color.a);
    }
}
