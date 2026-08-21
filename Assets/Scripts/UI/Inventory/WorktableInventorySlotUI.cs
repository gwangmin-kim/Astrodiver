using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CreatureInventorySlotUI))]
public sealed class WorktableInventorySlotUI : MonoBehaviour
{
    [SerializeField] private CreatureInventorySlotUI _slotUI;
    [SerializeField] private GameObject _progressRoot;
    [SerializeField] private Image _progressFillImage;

    private void Awake()
    {
        Initialize();
    }

    public void SetSlot(CreatureInventorySlot slot, CreatureDefinition definition)
    {
        Initialize();
        if (_slotUI != null)
        {
            _slotUI.SetSlot(slot, definition);
        }
    }

    public void SetProcessing(bool isProcessing, float normalizedProgress)
    {
        Initialize();
        if (_progressRoot == null || _progressFillImage == null)
        {
            return;
        }

        _progressRoot.SetActive(isProcessing);
        if (isProcessing)
        {
            _progressFillImage.fillAmount = Mathf.Clamp01(normalizedProgress);
        }
    }

    private void Initialize()
    {
        if (_slotUI == null)
        {
            _slotUI = GetComponent<CreatureInventorySlotUI>();
        }

    }
}
