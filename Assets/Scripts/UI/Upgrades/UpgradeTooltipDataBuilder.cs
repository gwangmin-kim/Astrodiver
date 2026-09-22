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
        bool hasMiningDropPreview = false;
        bool hasStageRespawnProbabilityBonusPreview = false;

        IReadOnlyList<UpgradeEffect> effects = definition.Effects;
        for (int i = 0; i < effects.Count; i++)
        {
            UpgradeEffect effect = effects[i];
            if (effect == null ||
                !effect.TryCreatePreview(runtimeData, out UpgradeEffectPreview preview))
            {
                continue;
            }

            if (effect is MiningTileDropBonusUpgradeEffect || effect is MiningTileDropMultiplierUpgradeEffect)
            {
                if (!hasMiningDropPreview)
                {
                    AddMiningPreviews(effects, runtimeData, isMaxLevel);
                    hasMiningDropPreview = true;
                }
                continue;
            }

            if (effect is StageRespawnProbabilityBonusUpgradeEffect)
            {
                if (hasStageRespawnProbabilityBonusPreview)
                {
                    continue;
                }

                hasStageRespawnProbabilityBonusPreview = true;
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

    private void AddMiningPreviews(
        IReadOnlyList<UpgradeEffect> effects, GameRuntimeData runtimeData, bool isMaxLevel)
    {
        // Simulate the whole node in its actual effect order, including repeated targets.
        var nextMultipliers = new MiningTileDropMultiplierRuntimeData();
        var tiles = new List<MiningTileDefinition>();
        foreach (var effect in effects)
        {
            MiningTileDefinition tile = effect switch
            {
                MiningTileDropBonusUpgradeEffect bonus => bonus.MiningTile,
                MiningTileDropMultiplierUpgradeEffect multiplier => multiplier.MiningTile,
                _ => null
            };
            if (tile == null || !effect.TryValidate(out _)) continue;
            if (!tiles.Contains(tile))
            {
                tiles.Add(tile);
                nextMultipliers.Multiply(tile, runtimeData.MiningTileDropMultipliers.GetMultiplier(tile));
            }
            if (effect is MiningTileDropBonusUpgradeEffect bonusEffect)
                nextMultipliers.AddBonus(tile, bonusEffect.Bonus);
            else if (effect is MiningTileDropMultiplierUpgradeEffect multiplierEffect)
                nextMultipliers.Multiply(tile, multiplierEffect.Multiplier);
        }
        var groups = new List<(float current, float next, string names)>();
        foreach (var tile in tiles)
        {
            float current = runtimeData.MiningTileDropMultipliers.GetMultiplier(tile);
            float next = nextMultipliers.GetMultiplier(tile);
            string label = tile.DropResource != null ? tile.DropResource.DisplayName : tile.name;
            int index = groups.FindIndex(group => group.current == current && group.next == next);
            if (index < 0) groups.Add((current, next, label));
            else
            {
                var group = groups[index];
                groups[index] = (current, next, group.names + ", " + label);
            }
        }
        foreach (var group in groups)
            _effectLines.Add(FormatPreview(UpgradeEffectPreview.Numeric(
                group.current, group.next, false, group.names + " 드롭 배율"), isMaxLevel));
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
        string valueText;
        if (isMaxLevel)
        {
            valueText = current;
        }
        else
        {
            string next = FormatNumber(preview.NextValue, preview.UsesIntegerFormat);
            valueText = $"{current} -> {next}";
        }

        return string.IsNullOrWhiteSpace(preview.Label)
            ? valueText
            : $"{preview.Label}: {valueText}";
    }

    private static string FormatNumber(float value, bool useIntegerFormat)
    {
        return useIntegerFormat
            ? UnityEngine.Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
