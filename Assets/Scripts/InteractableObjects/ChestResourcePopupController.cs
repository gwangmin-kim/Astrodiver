using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class ChestResourcePopupController : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private GameObject _popupRoot;
    [SerializeField] private Transform _entryContainer;
    [SerializeField] private ResourceFragmentEntryUI _entryPrefab;

    private PlayerInventoryController _playerInventory;

    private void Awake()
    {
        if (_playerLayer.value == 0)
        {
            _playerLayer = LayerMask.GetMask("Player");
        }

        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        SetPopupVisible(false);
    }

    private void OnDisable()
    {
        HidePopup();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        ShowPopup();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        HidePopup();
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        return other != null &&
               (_playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    private void ShowPopup()
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

        RefreshEntries();
    }

    private void HidePopup()
    {
        Unsubscribe();
        SetPopupVisible(false);
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
            SetPopupVisible(false);
            return;
        }

        IReadOnlyList<ResourceInventoryEntry> resources =
            _playerInventory.ChestResourceAmounts;
        if (resources == null)
        {
            SetPopupVisible(false);
            return;
        }

        bool hasEntries = false;
        foreach (ResourceInventoryEntry resource in resources)
        {
            if (resource == null || resource.Amount <= 0 ||
                !_playerInventory.TryResolveResourceDefinition(
                    resource,
                    out ResourceDefinition definition))
            {
                continue;
            }

            ResourceFragmentEntryUI entry = Instantiate(
                _entryPrefab,
                _entryContainer,
                false);
            entry.name = $"Chest Resource {definition.Id}";
            entry.SetResource(definition, resource.Amount);
            hasEntries = true;
        }

        SetPopupVisible(hasEntries);
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

    private void SetPopupVisible(bool visible)
    {
        if (_popupRoot != null && _popupRoot.activeSelf != visible)
        {
            _popupRoot.SetActive(visible);
        }
    }
}
