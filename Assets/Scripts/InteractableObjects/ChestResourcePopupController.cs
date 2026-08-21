using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class ChestResourcePopupController : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private ChestResourcePopupAnimation _popupAnimation;
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

        _popupAnimation?.HideImmediate();
    }

    private void OnDisable()
    {
        Unsubscribe();
        _popupAnimation?.HideImmediate();
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
        _popupAnimation?.Hide();
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
            _popupAnimation?.Hide();
            return;
        }

        IReadOnlyList<ResourceDefinition> definitions =
            GameDataManager.Instance?.Definitions?.OrderedResources;
        if (definitions == null)
        {
            _popupAnimation?.Hide();
            return;
        }

        int entryCount = 0;
        foreach (ResourceDefinition definition in definitions)
        {
            int amount = _playerInventory.GetChestResourceAmount(definition);
            if (amount <= 0)
            {
                continue;
            }

            ResourceFragmentEntryUI entry = Instantiate(
                _entryPrefab,
                _entryContainer,
                false);
            entry.name = $"Chest Resource {definition.Id}";
            entry.SetResource(definition, amount);
            entryCount++;
        }

        if (entryCount > 0)
        {
            _popupAnimation?.Show();
        }
        else
        {
            _popupAnimation?.Hide();
        }
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

}
