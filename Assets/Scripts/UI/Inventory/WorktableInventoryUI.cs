using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WorktableInventoryUI : MonoBehaviour
{
    [SerializeField] private CreatureInventorySlotUI _slotPrefab;
    [SerializeField] private ChestResourcePopupAnimation _popupAnimation;
    [SerializeField] private RectTransform _slotContainer;

    private readonly List<CreatureInventorySlotUI> _slots = new();
    private WorktableService _service;

    private void OnEnable()
    {
        if (_popupAnimation != null)
        {
            _popupAnimation.HideImmediate();
        }

        BindService();
        Refresh();
    }

    private void Update()
    {
        if (_service != WorktableService.Instance)
        {
            BindService();
            Refresh();
        }
    }

    private void OnDisable()
    {
        UnbindService();
        if (_popupAnimation != null)
        {
            _popupAnimation.HideImmediate();
        }
    }

    private void BindService()
    {
        if (_service == WorktableService.Instance)
        {
            return;
        }

        UnbindService();
        _service = WorktableService.Instance;
        if (_service != null)
        {
            _service.Changed += HandleServiceChanged;
        }
    }

    private void UnbindService()
    {
        if (_service != null)
        {
            _service.Changed -= HandleServiceChanged;
            _service = null;
        }
    }

    private void HandleServiceChanged()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_popupAnimation == null || _slotContainer == null ||
            _slotPrefab == null || _service == null ||
            !_service.IsInitialized || !_service.IsUnlocked)
        {
            HidePopup();
            return;
        }

        EnsureSlots(_service.SlotCapacity);
        for (int i = 0; i < _slots.Count; i++)
        {
            CreatureInventorySlot inventorySlot = i < _service.CreatureSlots.Count
                ? _service.CreatureSlots[i]
                : null;
            CreatureDefinition definition = null;
            _service.TryResolveCreatureDefinition(inventorySlot, out definition);
            _slots[i].SetSlot(inventorySlot, definition);
        }

        if (_service.ProcessingSlotIndex >= 0)
        {
            _popupAnimation.Show();
        }
        else
        {
            HidePopup();
        }
    }

    private void EnsureSlots(int slotCount)
    {
        if (_slots.Count == slotCount)
        {
            return;
        }

        ClearSlots();
        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            GameObject slotObject = Instantiate(
                _slotPrefab.gameObject,
                _slotContainer,
                false);
            slotObject.name = $"Worktable Slot {slotIndex + 1:00}";
            CreatureInventorySlotUI slot =
                slotObject.GetComponent<CreatureInventorySlotUI>();
            if (slot == null)
            {
                Debug.LogError(
                    "Worktable slot template requires CreatureInventorySlotUI.",
                    slotObject);
                Destroy(slotObject);
                continue;
            }

            _slots.Add(slot);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_slotContainer);
    }

    private void ClearSlots()
    {
        for (int i = _slotContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = _slotContainer.GetChild(i);
            if (child.name.StartsWith("Worktable Slot "))
            {
                Destroy(child.gameObject);
            }
        }

        _slots.Clear();
    }

    private void HidePopup()
    {
        if (_popupAnimation != null)
        {
            _popupAnimation.Hide();
        }
    }

}
