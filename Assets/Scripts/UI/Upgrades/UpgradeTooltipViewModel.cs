using Astrodiver.Upgrades;
using System.Collections.Generic;

namespace Astrodiver.UI.Upgrades
{
    public sealed class UpgradeTooltipViewModel
    {
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public IReadOnlyList<string> EffectLines { get; set; }
        public IReadOnlyList<UpgradeResourceCost> Costs { get; set; }
        public bool IsMaxLevel { get; set; }
    }
}
