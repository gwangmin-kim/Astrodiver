using System.Collections.Generic;

public sealed class UpgradeTooltipViewModel
{
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public IReadOnlyList<string> EffectLines { get; set; }
    public IReadOnlyList<UpgradeResourceCost> Costs { get; set; }
    public bool IsMaxLevel { get; set; }
}
