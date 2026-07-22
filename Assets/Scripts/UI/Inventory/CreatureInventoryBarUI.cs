using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CreatureInventoryBarUI : MonoBehaviour
{
    [SerializeField] private PlayerInventoryController _playerInventory;
    [SerializeField] private CreatureInventorySlotUI _slotPrefab;
    [SerializeField][Min(24f)] private float _slotSize = 64f;
    [SerializeField][Min(0f)] private float _slotSpacing = 8f;
    [SerializeField] private Vector2 _anchoredPosition = new(0f, 24f);

    private readonly List<CreatureInventorySlotUI> _slots = new();

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            InventoryEvents.CreatureSlotChanged += OnCreatureSlotChanged;
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            InventoryEvents.CreatureSlotChanged -= OnCreatureSlotChanged;
        }
    }

    public void Initialize(PlayerInventoryController playerInventory)
    {
        _playerInventory = playerInventory;
        InitializeFromPlayerInventory();
    }

    private void OnCreatureSlotChanged(int slotIndex, CreatureInventorySlot slot)
    {
        InitializeFromPlayerInventory();
        if (slotIndex < 0 || slotIndex >= _slots.Count) return;

        _slots[slotIndex].SetSlot(slot);
    }

    private void InitializeFromPlayerInventory()
    {
        int slotCount = _playerInventory != null && _playerInventory.CreatureSlots != null
            ? _playerInventory.CreatureSlots.Count
            : 0;

        EnsureLayout(slotCount);

        if (_playerInventory == null || _playerInventory.CreatureSlots == null) return;

        for (int i = 0; i < _playerInventory.CreatureSlots.Count && i < _slots.Count; i++)
        {
            _slots[i].SetSlot(_playerInventory.CreatureSlots[i]);
        }
    }

    private void EnsureLayout(int slotCount)
    {
        slotCount = Mathf.Max(0, slotCount);

        RectTransform rectTransform = EnsureComponent<RectTransform>(gameObject);
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = _anchoredPosition;
        rectTransform.sizeDelta = new Vector2(
            slotCount * _slotSize + Mathf.Max(0, slotCount - 1) * _slotSpacing,
            _slotSize);

        GridLayoutGroup layoutGroup = EnsureComponent<GridLayoutGroup>(gameObject);
        layoutGroup.cellSize = new Vector2(_slotSize, _slotSize);
        layoutGroup.spacing = new Vector2(_slotSpacing, 0f);
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
        layoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        layoutGroup.constraintCount = 1;

        EnsureSlots(slotCount);
    }

    private void EnsureSlots(int slotCount)
    {
        _slots.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            string slotName = $"Creature Slot {i + 1:00}";
            GameObject slotObject = GetOrCreateSlotObject(slotName);

            CreatureInventorySlotUI slot = EnsureComponent<CreatureInventorySlotUI>(slotObject);
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
        GameObject slotObject = _slotPrefab != null
            ? Instantiate(_slotPrefab.gameObject)
            : new GameObject(slotName, typeof(RectTransform));

        slotObject.name = slotName;
        slotObject.transform.SetParent(transform, false);
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

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
