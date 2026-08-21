using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class WorktableFacilityController : InteractableObject
{
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private Collider2D _interactionCollider;

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
    }

    private void Reset()
    {
        _isRepeatable = true;
        _interactionCollider = GetComponent<Collider2D>();
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
