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
        bool usesIntegerFormat,
        string label)
    {
        Kind = kind;
        CurrentValue = currentValue;
        NextValue = nextValue;
        UsesIntegerFormat = usesIntegerFormat;
        Label = label;
    }

    public UpgradeEffectPreviewKind Kind { get; }
    public float CurrentValue { get; }
    public float NextValue { get; }
    public bool UsesIntegerFormat { get; }
    public string Label { get; }

    public static UpgradeEffectPreview Numeric(
        float currentValue,
        float nextValue,
        bool usesIntegerFormat,
        string label = null)
    {
        return new UpgradeEffectPreview(
            UpgradeEffectPreviewKind.Numeric,
            currentValue,
            nextValue,
            usesIntegerFormat,
            label);
    }

    public static UpgradeEffectPreview Unlock()
    {
        return new UpgradeEffectPreview(
            UpgradeEffectPreviewKind.Unlock,
            0f,
            0f,
            false,
            null);
    }
}
