using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SpaceBackgroundProfile",
    menuName = "Astrodiver/Rendering/Space Background Profile")]
public sealed class SpaceBackgroundProfile : ScriptableObject
{
    [Header("Gradient")]
    [SerializeField] private bool _useGradient = true;
    [SerializeField] private Color _solidColor = new(0.018f, 0.026f, 0.07f, 1f);
    [SerializeField] private Color _bottomColor = new(0.018f, 0.026f, 0.07f, 1f);
    [SerializeField] private Color _topColor = new(0.06f, 0.08f, 0.18f, 1f);
    [SerializeField, Range(-0.5f, 0.5f)] private float _curveStrength = 0.2f;
    [SerializeField, Range(0.5f, 12f)] private float _noiseScale = 3f;
    [SerializeField, Range(0f, 0.5f)] private float _noiseStrength = 0.18f;

    [Header("Dithering")]
    [SerializeField] private bool _useDithering = true;
    [SerializeField, Range(0f, 1f)] private float _ditherStrength = 0.3f;
    [SerializeField, Range(1f, 4f)] private float _ditherPixelSize = 1f;

    [Header("Nebula")]
    [SerializeField] private bool _useNebula = true;
    [SerializeField] private Color _nebulaColor = new(0.12f, 0.16f, 0.38f, 1f);
    [SerializeField, Range(0.5f, 6f)] private float _nebulaScale = 1.6f;
    [SerializeField, Range(0f, 0.5f)] private float _nebulaStrength = 0.08f;
    [SerializeField, Range(0.2f, 0.8f)] private float _nebulaCoverage = 0.52f;
    [SerializeField, Range(0f, 0.25f)] private float _nebulaParallax = 0.015f;

    [Header("Star Layers")]
    [SerializeField] private bool _useStars = true;
    [SerializeField] private Color _starColor = new(0.75f, 0.85f, 1f, 1f);
    [SerializeField] private bool _useStarParallax = true;
    [SerializeField, Range(0f, 2f)] private float _starParallaxStrength = 1f;

    [Header("Far Stars")]
    [SerializeField, Range(0f, 0.5f)] private float _farStarDensity = 0.055f;
    [SerializeField, Range(4f, 64f)] private float _farStarCellSize = 14f;
    [SerializeField, Range(1f, 4f)] private float _farStarSize = 1f;
    [SerializeField, Range(0f, 4f)] private float _farStarBrightness = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _farStarParallax = 0.03f;

    [Header("Middle Stars")]
    [SerializeField, Range(0f, 0.5f)] private float _middleStarDensity = 0.06f;
    [SerializeField, Range(4f, 64f)] private float _middleStarCellSize = 18f;
    [SerializeField, Range(1f, 4f)] private float _middleStarSize = 1.5f;
    [SerializeField, Range(0f, 4f)] private float _middleStarBrightness = 0.65f;
    [SerializeField, Range(0f, 1f)] private float _middleStarParallax = 0.1f;

    [Header("Near Stars")]
    [SerializeField, Range(0f, 0.5f)] private float _nearStarDensity = 0.025f;
    [SerializeField, Range(4f, 64f)] private float _nearStarCellSize = 24f;
    [SerializeField, Range(1f, 4f)] private float _nearStarSize = 2f;
    [SerializeField, Range(0f, 4f)] private float _nearStarBrightness = 1f;
    [SerializeField, Range(0f, 1f)] private float _nearStarParallax = 0.25f;

    [Header("Scene Seed")]
    [SerializeField] private bool _randomizeSeedsOnSceneEntry = true;
    [SerializeField] private int _fixedSeed = 12345;

    private static readonly int _useGradientId = Shader.PropertyToID("_UseGradient");
    private static readonly int _colorId = Shader.PropertyToID("_Color");
    private static readonly int _bottomColorId = Shader.PropertyToID("_BottomColor");
    private static readonly int _topColorId = Shader.PropertyToID("_TopColor");
    private static readonly int _curveStrengthId = Shader.PropertyToID("_CurveStrength");
    private static readonly int _noiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int _noiseStrengthId = Shader.PropertyToID("_NoiseStrength");
    private static readonly int _noiseSeedId = Shader.PropertyToID("_NoiseSeed");
    private static readonly int _useDitheringId = Shader.PropertyToID("_UseDithering");
    private static readonly int _ditherStrengthId = Shader.PropertyToID("_DitherStrength");
    private static readonly int _ditherPixelSizeId = Shader.PropertyToID("_DitherPixelSize");
    private static readonly int _useNebulaId = Shader.PropertyToID("_UseNebula");
    private static readonly int _nebulaColorId = Shader.PropertyToID("_NebulaColor");
    private static readonly int _nebulaScaleId = Shader.PropertyToID("_NebulaScale");
    private static readonly int _nebulaStrengthId = Shader.PropertyToID("_NebulaStrength");
    private static readonly int _nebulaCoverageId = Shader.PropertyToID("_NebulaCoverage");
    private static readonly int _nebulaSeedId = Shader.PropertyToID("_NebulaSeed");
    private static readonly int _nebulaParallaxId = Shader.PropertyToID("_NebulaParallax");
    private static readonly int _useStarsId = Shader.PropertyToID("_UseStars");
    private static readonly int _starColorId = Shader.PropertyToID("_StarColor");
    private static readonly int _useStarParallaxId = Shader.PropertyToID("_UseStarParallax");
    private static readonly int _starParallaxStrengthId = Shader.PropertyToID("_StarParallaxStrength");
    private static readonly int _farStarDensityId = Shader.PropertyToID("_FarStarDensity");
    private static readonly int _farStarCellSizeId = Shader.PropertyToID("_FarStarCellSize");
    private static readonly int _farStarSizeId = Shader.PropertyToID("_FarStarSize");
    private static readonly int _farStarBrightnessId = Shader.PropertyToID("_FarStarBrightness");
    private static readonly int _farStarSeedId = Shader.PropertyToID("_FarStarSeed");
    private static readonly int _farStarParallaxId = Shader.PropertyToID("_FarStarParallax");
    private static readonly int _middleStarDensityId = Shader.PropertyToID("_StarDensity");
    private static readonly int _middleStarCellSizeId = Shader.PropertyToID("_StarCellSize");
    private static readonly int _middleStarSizeId = Shader.PropertyToID("_StarSize");
    private static readonly int _middleStarBrightnessId = Shader.PropertyToID("_StarBrightness");
    private static readonly int _middleStarSeedId = Shader.PropertyToID("_StarSeed");
    private static readonly int _middleStarParallaxId = Shader.PropertyToID("_MidStarParallax");
    private static readonly int _nearStarDensityId = Shader.PropertyToID("_NearStarDensity");
    private static readonly int _nearStarCellSizeId = Shader.PropertyToID("_NearStarCellSize");
    private static readonly int _nearStarSizeId = Shader.PropertyToID("_NearStarSize");
    private static readonly int _nearStarBrightnessId = Shader.PropertyToID("_NearStarBrightness");
    private static readonly int _nearStarSeedId = Shader.PropertyToID("_NearStarSeed");
    private static readonly int _nearStarParallaxId = Shader.PropertyToID("_NearStarParallax");

    public SpaceBackgroundSeeds CreateSeeds()
    {
        int seed = _randomizeSeedsOnSceneEntry
            ? Guid.NewGuid().GetHashCode()
            : _fixedSeed;
        System.Random random = new(seed);

        return new SpaceBackgroundSeeds(
            NextSeed(random),
            NextSeed(random),
            NextSeed(random),
            NextSeed(random),
            NextSeed(random));
    }

    public void ApplyTo(
        MaterialPropertyBlock properties,
        SpaceBackgroundSeeds seeds)
    {
        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        properties.SetFloat(_useGradientId, ToFloat(_useGradient));
        properties.SetColor(_colorId, _solidColor);
        properties.SetColor(_bottomColorId, _bottomColor);
        properties.SetColor(_topColorId, _topColor);
        properties.SetFloat(_curveStrengthId, _curveStrength);
        properties.SetFloat(_noiseScaleId, _noiseScale);
        properties.SetFloat(_noiseStrengthId, _noiseStrength);
        properties.SetFloat(_noiseSeedId, seeds.GradientNoise);

        properties.SetFloat(_useDitheringId, ToFloat(_useDithering));
        properties.SetFloat(_ditherStrengthId, _ditherStrength);
        properties.SetFloat(_ditherPixelSizeId, _ditherPixelSize);

        properties.SetFloat(_useNebulaId, ToFloat(_useNebula));
        properties.SetColor(_nebulaColorId, _nebulaColor);
        properties.SetFloat(_nebulaScaleId, _nebulaScale);
        properties.SetFloat(_nebulaStrengthId, _nebulaStrength);
        properties.SetFloat(_nebulaCoverageId, _nebulaCoverage);
        properties.SetFloat(_nebulaSeedId, seeds.Nebula);
        properties.SetFloat(_nebulaParallaxId, _nebulaParallax);

        properties.SetFloat(_useStarsId, ToFloat(_useStars));
        properties.SetColor(_starColorId, _starColor);
        properties.SetFloat(_useStarParallaxId, ToFloat(_useStarParallax));
        properties.SetFloat(_starParallaxStrengthId, _starParallaxStrength);

        properties.SetFloat(_farStarDensityId, _farStarDensity);
        properties.SetFloat(_farStarCellSizeId, _farStarCellSize);
        properties.SetFloat(_farStarSizeId, _farStarSize);
        properties.SetFloat(_farStarBrightnessId, _farStarBrightness);
        properties.SetFloat(_farStarSeedId, seeds.FarStars);
        properties.SetFloat(_farStarParallaxId, _farStarParallax);

        properties.SetFloat(_middleStarDensityId, _middleStarDensity);
        properties.SetFloat(_middleStarCellSizeId, _middleStarCellSize);
        properties.SetFloat(_middleStarSizeId, _middleStarSize);
        properties.SetFloat(_middleStarBrightnessId, _middleStarBrightness);
        properties.SetFloat(_middleStarSeedId, seeds.MiddleStars);
        properties.SetFloat(_middleStarParallaxId, _middleStarParallax);

        properties.SetFloat(_nearStarDensityId, _nearStarDensity);
        properties.SetFloat(_nearStarCellSizeId, _nearStarCellSize);
        properties.SetFloat(_nearStarSizeId, _nearStarSize);
        properties.SetFloat(_nearStarBrightnessId, _nearStarBrightness);
        properties.SetFloat(_nearStarSeedId, seeds.NearStars);
        properties.SetFloat(_nearStarParallaxId, _nearStarParallax);
    }

    private static float NextSeed(System.Random random)
    {
        return (float)(random.NextDouble() * 10000.0);
    }

    private static float ToFloat(bool value)
    {
        return value ? 1f : 0f;
    }
}

public readonly struct SpaceBackgroundSeeds
{
    public SpaceBackgroundSeeds(
        float gradientNoise,
        float nebula,
        float farStars,
        float middleStars,
        float nearStars)
    {
        GradientNoise = gradientNoise;
        Nebula = nebula;
        FarStars = farStars;
        MiddleStars = middleStars;
        NearStars = nearStars;
    }

    public float GradientNoise { get; }
    public float Nebula { get; }
    public float FarStars { get; }
    public float MiddleStars { get; }
    public float NearStars { get; }
}
