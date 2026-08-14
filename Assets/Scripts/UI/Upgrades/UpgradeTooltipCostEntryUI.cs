using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UpgradeTooltipCostEntryUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _amountText;

    public void SetResource(ResourceDefinition definition, int amount)
    {
        _iconImage.sprite = definition != null ? definition.Icon : null;
        _iconImage.enabled = _iconImage.sprite != null;
        _amountText.text = amount.ToString();
    }
}
