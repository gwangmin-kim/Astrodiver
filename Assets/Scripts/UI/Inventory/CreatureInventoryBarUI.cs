using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CreatureInventoryBarUI : MonoBehaviour
{
    [SerializeField] private PlayerInventoryController _playerInventory;
    [SerializeField] private CreatureInventorySlotUI _slotPrefab;

    private readonly List<CreatureInventorySlotUI> _slots = new();

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            Subscribe();
        }

        InitializeFromPlayerInventory();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            Unsubscribe();
        }
    }

    public void Initialize(PlayerInventoryController playerInventory)
    {
        Unsubscribe();
        _playerInventory = playerInventory;
        Subscribe();
        InitializeFromPlayerInventory();
    }

    private void OnInventoryChanged()
    {
        InitializeFromPlayerInventory();
    }

    private void Subscribe()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || _playerInventory == null)
        {
            return;
        }

        _playerInventory.Changed -= OnInventoryChanged;
        _playerInventory.Changed += OnInventoryChanged;
    }

    private void Unsubscribe()
    {
        if (_playerInventory != null)
        {
            _playerInventory.Changed -= OnInventoryChanged;
        }
    }

    private void InitializeFromPlayerInventory()
    {
        if (_playerInventory == null || !_playerInventory.IsInitialized)
        {
            return;
        }

        int slotCount = _playerInventory.CreatureSlotCapacity;
        EnsureSlots(slotCount);

        for (int i = 0; i < _playerInventory.CreatureSlots.Count && i < _slots.Count; i++)
        {
            SetUiSlot(i, _playerInventory.CreatureSlots[i]);
        }
    }

    private void SetUiSlot(int slotIndex, CreatureInventorySlot slot)
    {
        CreatureDefinition definition = null;
        _playerInventory?.TryResolveCreatureDefinition(slot, out definition);
        _slots[slotIndex].SetSlot(slot, definition);
    }

    private void EnsureSlots(int slotCount)
    {
        _slots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            string slotName = $"Creature Slot {i + 1:00}";
            GameObject slotObject = GetOrCreateSlotObject(slotName);

            if (slotObject == null)
            {
                continue;
            }

            CreatureInventorySlotUI slot = slotObject.GetComponent<CreatureInventorySlotUI>();
            if (slot == null)
            {
                Debug.LogError("Creature inventory slot prefab is missing CreatureInventorySlotUI.", slotObject);
                continue;
            }

            slot.SetEmpty();
            _slots.Add(slot);
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith("Creature Slot ")) continue;
            if (TryParseSlotNumber(child.name, out int slotNumber) && slotNumber <= slotCount) continue;

            DestroyUiObject(child.gameObject);
        }
    }

    private GameObject GetOrCreateSlotObject(string slotName)
    {
        Transform slotTransform = transform.Find(slotName);
        return slotTransform != null ? slotTransform.gameObject : CreateSlotObject(slotName);
    }

    private GameObject CreateSlotObject(string slotName)
    {
        if (_slotPrefab == null)
        {
            Debug.LogError("CreatureInventoryBarUI requires a slot prefab.", this);
            return null;
        }

        GameObject slotObject = Instantiate(_slotPrefab.gameObject, transform, false);

        slotObject.name = slotName;
        return slotObject;
    }

    private static bool TryParseSlotNumber(string slotName, out int slotNumber)
    {
        slotNumber = 0;
        string suffix = slotName.Replace("Creature Slot ", string.Empty);
        return int.TryParse(suffix, out slotNumber);
    }

    private static void DestroyUiObject(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

}
