using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class WorktableFacilityController : InteractableObject
{
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Collider2D _interactionCollider;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private WorktableInventoryUI _inventoryPopup;

    private readonly HashSet<Collider2D> _overlappingPlayerColliders = new();
    private GameDataManager _gameDataManager;

    public override float RepeatInterval
    {
        get
        {
            WorktableService service = WorktableService.Instance;
            return service != null ? service.TransferInterval : base.RepeatInterval;
        }
    }

    public override bool CanRepeatInteract
    {
        get
        {
            WorktableService service = WorktableService.Instance;
            return IsRepeatable && service != null &&
                service.CanTransferOneFromPlayer;
        }
    }

    private void Awake()
    {
        if (_interactionCollider == null)
        {
            _interactionCollider = GetComponent<Collider2D>();
        }

        if (_playerLayer.value == 0)
        {
            _playerLayer = LayerMask.GetMask("Player");
        }

        if (_inventoryPopup == null)
        {
            _inventoryPopup = GetComponent<WorktableInventoryUI>();
        }
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshState();
    }

    private void Start()
    {
        Subscribe();
        RefreshState();
    }

    private void OnDisable()
    {
        Unsubscribe();
        _overlappingPlayerColliders.Clear();
        _inventoryPopup?.SetPlayerOverlapping(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        _overlappingPlayerColliders.Add(other);
        _inventoryPopup?.SetPlayerOverlapping(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        _overlappingPlayerColliders.Remove(other);
        _inventoryPopup?.SetPlayerOverlapping(
            _overlappingPlayerColliders.Count > 0);
    }

    public override void Interact()
    {
        if (WorktableService.Instance != null)
        {
            WorktableService.Instance.TryTransferOneFromPlayer();
        }
    }

    private void Subscribe()
    {
        GameDataManager manager = GameDataManager.Instance;
        if (_gameDataManager == manager)
        {
            return;
        }

        Unsubscribe();
        _gameDataManager = manager;
        if (_gameDataManager != null)
        {
            _gameDataManager.DataChanged += HandleDataChanged;
            _gameDataManager.RuntimeDataChanged += HandleRuntimeDataChanged;
        }
    }

    private void Unsubscribe()
    {
        if (_gameDataManager != null)
        {
            _gameDataManager.DataChanged -= HandleDataChanged;
            _gameDataManager.RuntimeDataChanged -= HandleRuntimeDataChanged;
            _gameDataManager = null;
        }
    }

    private void HandleDataChanged(GameSaveData data)
    {
        RefreshState();
    }

    private void HandleRuntimeDataChanged(GameRuntimeData data)
    {
        RefreshState();
    }

    private void RefreshState()
    {
        GameDataManager manager = GameDataManager.Instance;
        bool unlocked = manager != null && manager.IsInitialized &&
            manager.RuntimeData.Facilities.WorktableUnlocked;
        if (_visualRoot != null)
        {
            _visualRoot.SetActive(unlocked);
        }

        if (_interactionCollider != null)
        {
            _interactionCollider.enabled = unlocked;
        }

        if (!unlocked)
        {
            _overlappingPlayerColliders.Clear();
            _inventoryPopup?.SetPlayerOverlapping(false);
        }
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        return other != null &&
            (_playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    private void Reset()
    {
        _isRepeatable = true;
        _interactionCollider = GetComponent<Collider2D>();
        _inventoryPopup = GetComponent<WorktableInventoryUI>();
        _playerLayer = LayerMask.GetMask("Player");
        if (_interactionCollider != null)
        {
            _interactionCollider.isTrigger = true;
        }

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer >= 0)
        {
            gameObject.layer = interactableLayer;
        }
    }
}
