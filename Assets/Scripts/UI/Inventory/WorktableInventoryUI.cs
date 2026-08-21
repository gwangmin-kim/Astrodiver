using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WorktableInventoryUI : MonoBehaviour
{
    private const int SlotsPerRow = 4;

    [SerializeField] private CreatureInventorySlotUI _slotPrefab;
    [SerializeField] private ChestResourcePopupAnimation _popupAnimation;
    [SerializeField] private RectTransform _rowContainer;
    [SerializeField] private RectTransform _rowPrefab;

    private readonly List<WorktableInventorySlotUI> _slots = new();
    private readonly List<GameObject> _rows = new();
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
        if (_popupAnimation == null || _rowContainer == null ||
            _rowPrefab == null || _slotPrefab == null || _service == null ||
            !_service.IsInitialized || !_service.IsUnlocked)
        {
            HidePopup();
            return;
        }

        EnsureSlots(_service.SlotCapacity);
        int processingIndex = _service.ProcessingSlotIndex;
        for (int i = 0; i < _slots.Count; i++)
        {
            CreatureInventorySlot inventorySlot = i < _service.CreatureSlots.Count
                ? _service.CreatureSlots[i]
                : null;
            CreatureDefinition definition = null;
            _service.TryResolveCreatureDefinition(inventorySlot, out definition);
            _slots[i].SetSlot(inventorySlot, definition);
            _slots[i].SetProcessing(
                i == processingIndex,
                i == processingIndex ? _service.NormalizedProgress : 0f);
        }

        if (processingIndex >= 0)
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

        ClearLayout();
        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            if (slotIndex % SlotsPerRow == 0)
            {
                CreateRow();
            }

            GameObject slotObject = Instantiate(
                _slotPrefab.gameObject,
                _rows[_rows.Count - 1].transform,
                false);
            slotObject.name = $"Worktable Slot {slotIndex + 1:00}";
            WorktableInventorySlotUI slot =
                slotObject.GetComponent<WorktableInventorySlotUI>();
            if (slot == null)
            {
                Debug.LogError(
                    "Worktable slot template requires WorktableInventorySlotUI.",
                    slotObject);
                Destroy(slotObject);
                continue;
            }

            _slots.Add(slot);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rowContainer);
    }

    private void CreateRow()
    {
        RectTransform row = Instantiate(_rowPrefab, _rowContainer, false);
        row.name = $"Worktable Slot Row {_rows.Count + 1:00}";
        row.gameObject.SetActive(true);
        _rows.Add(row.gameObject);
    }

    private void ClearLayout()
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            if (_rows[i] != null)
            {
                Destroy(_rows[i]);
            }
        }

        _rows.Clear();
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
