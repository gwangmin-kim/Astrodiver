using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class ChestResourcePopupController : MonoBehaviour
{
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private PopupFadeSlideAnimation _popupAnimation;
    [SerializeField] private Transform _entryContainer;
    [SerializeField] private ResourceFragmentEntryUI _entryPrefab;
    [SerializeField, Min(0f)] private float _resourceChangeDisplayDuration = 2f;

    private readonly HashSet<Collider2D> _overlappingPlayerColliders = new();
    private PlayerInventoryController _playerInventory;
    private float _temporaryDisplayRemaining;
    private bool _hasEntries;

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

    private void OnEnable()
    {
        BindInventory();
    }

    private void Update()
    {
        if (_playerInventory != PlayerInventoryController.Instance)
        {
            BindInventory();
        }

        if (_temporaryDisplayRemaining <= 0f)
        {
            return;
        }

        _temporaryDisplayRemaining = Mathf.Max(
            0f,
            _temporaryDisplayRemaining - Time.unscaledDeltaTime);
        if (_temporaryDisplayRemaining <= 0f)
        {
            RefreshVisibility();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
        _overlappingPlayerColliders.Clear();
        _temporaryDisplayRemaining = 0f;
        _hasEntries = false;
        _popupAnimation?.HideImmediate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        _overlappingPlayerColliders.Add(other);
        BindInventory();
        RefreshEntries();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        _overlappingPlayerColliders.Remove(other);
        RefreshVisibility();
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        return other != null &&
               (_playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    private void BindInventory()
    {
        PlayerInventoryController inventory = PlayerInventoryController.Instance;
        if (_playerInventory == inventory)
        {
            return;
        }

        Unsubscribe();
        _playerInventory = inventory;
        if (_playerInventory != null)
        {
            _playerInventory.Changed += HandleInventoryChanged;
            _playerInventory.ChestResourcesChanged += HandleChestResourcesChanged;

            if (_playerInventory.ConsumePendingChestResourcePopupRequest())
            {
                StartTemporaryDisplay();
            }
        }

        RefreshEntries();
    }

    private void HandleInventoryChanged()
    {
        RefreshEntries();
    }

    private void HandleChestResourcesChanged()
    {
        _playerInventory?.ConsumePendingChestResourcePopupRequest();
        StartTemporaryDisplay();
        RefreshEntries();
    }

    private void Unsubscribe()
    {
        if (_playerInventory != null)
        {
            _playerInventory.Changed -= HandleInventoryChanged;
            _playerInventory.ChestResourcesChanged -= HandleChestResourcesChanged;
            _playerInventory = null;
        }
    }

    private void RefreshEntries()
    {
        ClearEntries();

        if (_playerInventory == null || _entryContainer == null || _entryPrefab == null)
        {
            _hasEntries = false;
            RefreshVisibility();
            return;
        }

        IReadOnlyList<ResourceDefinition> definitions =
            GameDataManager.Instance?.Definitions?.OrderedResources;
        if (definitions == null)
        {
            _hasEntries = false;
            RefreshVisibility();
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

        _hasEntries = entryCount > 0;
        RefreshVisibility();
    }

    private void StartTemporaryDisplay()
    {
        _temporaryDisplayRemaining = _resourceChangeDisplayDuration;
    }

    private void RefreshVisibility()
    {
        bool shouldShow = _hasEntries &&
            (_overlappingPlayerColliders.Count > 0 ||
             _temporaryDisplayRemaining > 0f);
        if (shouldShow)
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
