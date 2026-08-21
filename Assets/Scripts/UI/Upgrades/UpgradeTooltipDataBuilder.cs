using System.Collections.Generic;
using System.Globalization;

public sealed class UpgradeTooltipDataBuilder
{
    private const string UnlockText = "잠금 해제";
    private const string EmptyValueText = "-";

    private readonly List<string> _effectLines = new();
    private readonly List<UpgradeResourceCost> _costs = new();

    public UpgradeTooltipViewModel Build(
        UpgradeNodeDefinition definition,
        int currentLevel,
        GameRuntimeData runtimeData)
    {
        _effectLines.Clear();
        _costs.Clear();

        if (definition == null)
        {
            _effectLines.Add(EmptyValueText);
            return CreateModel("-", string.Empty, true);
        }

        int clampedLevel = UnityEngine.Mathf.Clamp(
            currentLevel,
            0,
            definition.MaxLevel);
        bool isMaxLevel = clampedLevel >= definition.MaxLevel;

        IReadOnlyList<UpgradeEffect> effects = definition.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            UpgradeEffect effect = effects[i];
            if (effect == null ||
                !effect.TryCreatePreview(runtimeData, out UpgradeEffectPreview preview))
            {
                continue;
            }

            _effectLines.Add(FormatPreview(preview, isMaxLevel));
        }

        if (_effectLines.Count == 0)
        {
            _effectLines.Add(EmptyValueText);
        }

        if (!isMaxLevel)
        {
            definition.GetCostForNextLevel(clampedLevel, _costs);
            _costs.Sort(ResourceDisplayOrder.Compare);
        }

        string displayName = string.IsNullOrWhiteSpace(definition.DisplayName)
            ? definition.Id
            : definition.DisplayName;
        if (definition.MaxLevel > 1)
        {
            displayName = $"{displayName} - 레벨 {clampedLevel} / {definition.MaxLevel}";
        }

        return CreateModel(displayName, definition.Description, isMaxLevel);
    }

    private UpgradeTooltipViewModel CreateModel(
        string displayName,
        string description,
        bool isMaxLevel)
    {
        return new UpgradeTooltipViewModel
        {
            DisplayName = displayName,
            Description = description ?? string.Empty,
            EffectLines = _effectLines.ToArray(),
            Costs = _costs.ToArray(),
            IsMaxLevel = isMaxLevel
        };
    }

    private static string FormatPreview(
        UpgradeEffectPreview preview,
        bool isMaxLevel)
    {
        if (preview.Kind == UpgradeEffectPreviewKind.Unlock)
        {
            return UnlockText;
        }

        string current = FormatNumber(
            preview.CurrentValue,
            preview.UsesIntegerFormat);
        if (isMaxLevel)
        {
            return current;
        }

        string next = FormatNumber(preview.NextValue, preview.UsesIntegerFormat);
        return $"{current} -> {next}";
    }

    private static string FormatNumber(float value, bool useIntegerFormat)
    {
        return useIntegerFormat
            ? UnityEngine.Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
