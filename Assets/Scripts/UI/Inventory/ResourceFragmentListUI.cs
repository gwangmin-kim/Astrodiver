using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ResourceFragmentListUI : MonoBehaviour
{
    [SerializeField] private ResourceFragmentEntryUI _entryPrefab;
    [SerializeField] private PlayerInventoryController _playerInventory;
    [SerializeField] private Vector2 _anchoredPosition = new(-32f, 24f);
    [SerializeField] private Vector2 _size = new(190f, 240f);
    [SerializeField][Min(0f)] private float _entrySpacing = 4f;

    private readonly Dictionary<ResourceDefinition, ResourceFragmentEntryUI> _entries = new();

    private void OnEnable()
    {
        EnsureLayout();

        if (Application.isPlaying)
        {
            Subscribe();
        }

        Refresh();
    }

    public void Initialize(PlayerInventoryController inventory)
    {
        Unsubscribe();
        _playerInventory = inventory;
        Subscribe();
        Refresh();
    }

    private void Refresh()
    {
        ResourceDefinition[] displayedDefinitions = new ResourceDefinition[_entries.Count];
        _entries.Keys.CopyTo(displayedDefinitions, 0);

        foreach (ResourceDefinition definition in displayedDefinitions)
        {
            if (_playerInventory == null || _playerInventory.GetResourceAmount(definition) <= 0)
            {
                RemoveEntry(definition);
            }
        }

        if (_playerInventory == null)
        {
            return;
        }

        foreach (ResourceInventoryEntry entry in _playerInventory.ResourceAmounts)
        {
            if (_playerInventory.TryResolveResourceDefinition(
                    entry,
                    out ResourceDefinition definition))
            {
                SetResourceAmount(definition, entry.Amount);
            }
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            Unsubscribe();
        }
    }

    private void OnInventoryChanged()
    {
        Refresh();
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

    private void SetResourceAmount(ResourceDefinition definition, int amount)
    {
        if (definition == null) return;

        if (amount <= 0)
        {
            RemoveEntry(definition);
            return;
        }

        ResourceFragmentEntryUI entry = GetOrCreateEntry(definition);
        if (entry == null) return;

        entry.SetResource(definition, amount);
    }

    private void EnsureLayout()
    {
        RectTransform rectTransform = EnsureComponent<RectTransform>(gameObject);
        rectTransform.anchorMin = new Vector2(1f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(1f, 0f);
        rectTransform.anchoredPosition = _anchoredPosition;
        rectTransform.sizeDelta = _size;

        VerticalLayoutGroup layoutGroup = EnsureComponent<VerticalLayoutGroup>(gameObject);
        layoutGroup.childAlignment = TextAnchor.LowerRight;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.spacing = _entrySpacing;
    }

    private ResourceFragmentEntryUI GetOrCreateEntry(ResourceDefinition definition)
    {
        if (_entries.TryGetValue(definition, out ResourceFragmentEntryUI entry) && entry != null)
        {
            return entry;
        }

        if (_entryPrefab == null)
        {
            return null;
        }

        entry = Instantiate(_entryPrefab, transform, false);
        entry.name = GetEntryName(definition);
        _entries[definition] = entry;
        return entry;
    }

    private void RemoveEntry(ResourceDefinition definition)
    {
        if (!_entries.TryGetValue(definition, out ResourceFragmentEntryUI entry)) return;

        _entries.Remove(definition);
        if (entry == null) return;

        DestroyUiObject(entry.gameObject);
    }

    private static string GetEntryName(ResourceDefinition definition)
    {
        string id = string.IsNullOrWhiteSpace(definition.Id) ? definition.name : definition.Id;
        return $"Resource Fragment {id}";
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
