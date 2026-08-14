public enum UpgradeEffectPreviewKind
{
    Numeric,
    Unlock
}

public readonly struct UpgradeEffectPreview
{
    private UpgradeEffectPreview(
        UpgradeEffectPreviewKind kind,
        float currentValue,
        float nextValue,
        bool usesIntegerFormat)
    {
        Kind = kind;
        CurrentValue = currentValue;
        NextValue = nextValue;
        UsesIntegerFormat = usesIntegerFormat;
    }

    public UpgradeEffectPreviewKind Kind { get; }
    public float CurrentValue { get; }
    public float NextValue { get; }
    public bool UsesIntegerFormat { get; }

    public static UpgradeEffectPreview Numeric(
        float currentValue,
        float nextValue,
        bool usesIntegerFormat)
    {
        return new UpgradeEffectPreview(
            UpgradeEffectPreviewKind.Numeric,
            currentValue,
            nextValue,
            usesIntegerFormat);
    }

    public static UpgradeEffectPreview Unlock()
    {
        return new UpgradeEffectPreview(
            UpgradeEffectPreviewKind.Unlock,
            0f,
            0f,
            false);
    }
}
