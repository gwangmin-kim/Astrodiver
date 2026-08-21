using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UpgradeResourceListUI : MonoBehaviour
{
    [SerializeField] private GameObject _listRoot;
    [SerializeField] private Transform _entryContainer;
    [SerializeField] private ResourceFragmentEntryUI _entryPrefab;

    private PlayerInventoryController _playerInventory;

    private void OnEnable()
    {
        BindInventory();
        RefreshEntries();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void BindInventory()
    {
        PlayerInventoryController inventory = PlayerInventoryController.Instance;
        if (_playerInventory != inventory)
        {
            Unsubscribe();
            _playerInventory = inventory;
        }

        if (_playerInventory != null)
        {
            _playerInventory.Changed -= RefreshEntries;
            _playerInventory.Changed += RefreshEntries;
        }
    }

    private void Unsubscribe()
    {
        if (_playerInventory != null)
        {
            _playerInventory.Changed -= RefreshEntries;
            _playerInventory = null;
        }
    }

    private void RefreshEntries()
    {
        ClearEntries();

        if (_playerInventory == null || _entryContainer == null || _entryPrefab == null)
        {
            SetListVisible(false);
            return;
        }

        IReadOnlyList<ResourceDefinition> definitions =
            GameDataManager.Instance?.Definitions?.OrderedResources;
        if (definitions == null)
        {
            SetListVisible(false);
            return;
        }

        bool hasEntries = false;
        foreach (ResourceDefinition definition in definitions)
        {
            int amount = _playerInventory.IsResourceChestUnlocked
                ? _playerInventory.GetChestResourceAmount(definition)
                : _playerInventory.GetResourceAmount(definition);
            if (amount <= 0)
            {
                continue;
            }

            ResourceFragmentEntryUI entry = Instantiate(
                _entryPrefab,
                _entryContainer,
                false);
            entry.name = $"Upgrade Resource {definition.Id}";
            entry.SetResource(definition, amount);
            hasEntries = true;
        }

        SetListVisible(hasEntries);
    }

    private void ClearEntries()
    {
        if (_entryContainer == null)
        {
            return;
        }

        for (int i = _entryContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(_entryContainer.GetChild(i).gameObject);
        }
    }

    private void SetListVisible(bool visible)
    {
        if (_listRoot == null)
        {
            return;
        }

        CanvasGroup canvasGroup = _listRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogWarning(
                "Upgrade resource list requires a CanvasGroup on its list root.",
                this);
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }
}
