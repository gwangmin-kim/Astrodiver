using UnityEngine;

[DisallowMultipleComponent]
public sealed class ChestFacilityController : MonoBehaviour
{
    [SerializeField] private GameObject _visualRoot;

    private GameDataManager _gameDataManager;
    private UpgradeService _upgradeService;

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
        if (_visualRoot == null)
        {
            return;
        }

        bool isUnlocked = GameDataManager.Instance?.RuntimeData?.Facilities
            ?.ResourceChestUnlocked ?? false;
        _visualRoot.SetActive(isUnlocked);
    }
}
