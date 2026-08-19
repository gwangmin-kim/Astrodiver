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

    [Header("Particle Settings")]
    [SerializeField, Min(0f)] private float _particlesPerUnitLength = 12f;
    [SerializeField] private Vector2 _particleRotationSpeedRange = new(-180f, 180f);

    private Vector3 _defaultLocalScale;
    private Material _laserMaterial;
    private ParticleSystem _laserParticles;
    private PlasmaGunVisualPalette _palette;

    private void Awake()
    {
        if (_laserRenderer != null)
        {
            _defaultLocalScale = _laserRenderer.transform.localScale;
            _laserMaterial = _laserRenderer.material;
            _laserParticles = _laserRenderer.GetComponent<ParticleSystem>();
            ConfigureParticleRotation(_laserParticles, _particleRotationSpeedRange);
            ApplyPalette(_palette);
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

    public void ApplyPalette(PlasmaGunVisualPalette palette)
    {
        _palette = palette;
        if (_laserMaterial == null || _palette == null) return;
        _laserMaterial.SetColor(_innerColorId, _palette.InnerColor);
        _laserMaterial.SetColor(_middleColorId, _palette.MiddleColor);
        _laserMaterial.SetColor(_outlineColorId, _palette.OutlineColor);
        _laserMaterial.SetFloat(_glowIntensityId, _palette.GlowIntensity);
        if (_laserParticles != null) _palette.ApplyTo(_laserParticles);
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

    private static void ConfigureParticleRotation(ParticleSystem particles, Vector2 speedRange)
    {
        if (particles == null) return;
        ParticleSystem.MainModule main = particles.main;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(
            speedRange.x * Mathf.Deg2Rad,
            speedRange.y * Mathf.Deg2Rad);
    }

}
