using UnityEngine;

/// <summary>Single colour source for every plasma-gun visual.</summary>
public sealed class PlasmaGunVisualPalette : MonoBehaviour
{
    [SerializeField] private Color _outlineColor = Color.cyan;
    [SerializeField, Range(0f, 1f)] private float _middleColorBlend = 0.5f;
    [SerializeField, Min(0f)] private float _glowIntensity = 2f;

    public Color InnerColor => Color.white;
    public Color OutlineColor => _outlineColor;
    public Color MiddleColor => Color.Lerp(InnerColor, OutlineColor, _middleColorBlend);
    public float GlowIntensity => _glowIntensity;

    public Color ToHdr(Color color) => new(
        color.r * GlowIntensity, color.g * GlowIntensity, color.b * GlowIntensity, color.a);

    public void ApplyTo(PlasmaGunLaserVisual visual)
    {
        if (visual != null) visual.ApplyPalette(this);
    }

    public void ApplyTo(ParticleSystem particles)
    {
        if (particles == null) return;
        ParticleSystem.MainModule main = particles.main;
        main.startColor = new ParticleSystem.MinMaxGradient(ToHdr(InnerColor), ToHdr(OutlineColor));
    }
}
