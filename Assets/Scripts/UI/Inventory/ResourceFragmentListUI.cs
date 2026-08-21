using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ResourceFragmentListUI : MonoBehaviour
{
    [SerializeField] private ResourceFragmentEntryUI _entryPrefab;
    [SerializeField] private PlayerInventoryController _playerInventory;

    private readonly Dictionary<ResourceDefinition, ResourceFragmentEntryUI> _entries = new();

    private void OnEnable()
    {
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

        IReadOnlyList<ResourceDefinition> definitions =
            GameDataManager.Instance?.Definitions?.OrderedResources;
        if (definitions == null)
        {
            return;
        }

        int siblingIndex = 0;
        foreach (ResourceDefinition definition in definitions)
        {
            int amount = _playerInventory.GetResourceAmount(definition);
            if (amount > 0)
            {
                ResourceFragmentEntryUI entry =
                    SetResourceAmount(definition, amount);
                if (entry != null)
                {
                    entry.transform.SetSiblingIndex(siblingIndex++);
                }
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

    private ResourceFragmentEntryUI SetResourceAmount(
        ResourceDefinition definition,
        int amount)
    {
        if (definition == null) return null;

        if (amount <= 0)
        {
            RemoveEntry(definition);
            return null;
        }

        ResourceFragmentEntryUI entry = GetOrCreateEntry(definition);
        if (entry == null) return null;

        entry.SetResource(definition, amount);
        return entry;
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

}
