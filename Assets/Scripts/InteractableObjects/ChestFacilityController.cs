using UnityEngine;

[DisallowMultipleComponent]
public sealed class ChestFacilityController : MonoBehaviour
{
    [SerializeField] private GameObject _visualRoot;

    private GameDataManager _gameDataManager;
    private UpgradeService _upgradeService;
    private Collider2D _interactionCollider;
    private ChestResourcePopupController _resourcePopup;

    private void Awake()
    {
        _interactionCollider = GetComponent<Collider2D>();
        _resourcePopup = GetComponent<ChestResourcePopupController>();
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshVisibility();
    }

    private void Start()
    {
        Subscribe();
        RefreshVisibility();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        GameDataManager manager = GameDataManager.Instance;
        if (_gameDataManager != manager)
        {
            Unsubscribe();
            _gameDataManager = manager;
            if (_gameDataManager != null)
            {
                _gameDataManager.DataChanged += HandleDataChanged;
            }
        }

        UpgradeService service = _gameDataManager?.Upgrades;
        if (_upgradeService == service)
        {
            return;
        }

        if (_upgradeService != null)
        {
            _upgradeService.UpgradePurchased -= HandleUpgradePurchased;
        }

        _upgradeService = service;
        if (_upgradeService != null)
        {
            _upgradeService.UpgradePurchased += HandleUpgradePurchased;
        }
    }

    private void Unsubscribe()
    {
        if (_gameDataManager != null)
        {
            _gameDataManager.DataChanged -= HandleDataChanged;
            _gameDataManager = null;
        }

        if (_upgradeService != null)
        {
            _upgradeService.UpgradePurchased -= HandleUpgradePurchased;
            _upgradeService = null;
        }
    }

    private void HandleDataChanged(GameSaveData data)
    {
        RefreshVisibility();
    }

    private void HandleUpgradePurchased(UpgradeNodeDefinition definition, int level)
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool isUnlocked = GameDataManager.Instance?.RuntimeData?.Facilities
            ?.ResourceChestUnlocked ?? false;
        if (_visualRoot != null)
        {
            _visualRoot.SetActive(isUnlocked);
        }

        if (_interactionCollider != null)
        {
            _interactionCollider.enabled = isUnlocked;
        }

        if (_resourcePopup != null)
        {
            _resourcePopup.enabled = isUnlocked;
        }
    }
}
