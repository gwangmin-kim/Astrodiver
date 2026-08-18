using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CreatureInventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Color _emptyIconColor = new(1f, 1f, 1f, 0f);
    [SerializeField] private Color _filledIconColor = Color.white;
    private bool _hasReportedMissingReferences;

    public void SetSlot(CreatureInventorySlot slot, CreatureDefinition definition)
    {
        if (!HasRequiredReferences()) return;

        if (slot == null || slot.IsEmpty || definition == null)
        {
            SetEmpty();
            return;
        }

        _iconImage.sprite = definition.Icon;
        _iconImage.color = _filledIconColor;
        _iconImage.enabled = definition.Icon != null;
        _countText.text = slot.Count.ToString();
        _countText.enabled = true;
    }

    public void SetEmpty()
    {
        if (!HasRequiredReferences()) return;

        _iconImage.sprite = null;
        _iconImage.color = _emptyIconColor;
        _iconImage.enabled = false;
        _countText.text = string.Empty;
        _countText.enabled = false;
    }

    private bool HasRequiredReferences()
    {
        if (_backgroundImage != null && _iconImage != null && _countText != null)
        {
            return true;
        }

        if (!_hasReportedMissingReferences)
        {
            Debug.LogError(
                "CreatureInventorySlotUI requires Background, Icon, and Count references configured in its prefab.",
                this);
            _hasReportedMissingReferences = true;
        }

        return false;
    }
}
