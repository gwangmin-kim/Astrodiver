using UnityEngine;

public sealed class UpgradeInteractor : InteractableObject
{
    [Header("References")]
    [SerializeField] private UpgradeTreeUI _upgradeTreeUI;
    [SerializeField] private PlayerInputHandler _playerInput;
    [SerializeField] private UIInputHandler _uiInput;
    [SerializeField] private GameObject _inventoryHud;
    [SerializeField] private TutorialDocumentView _stageUnlockReportDocument;
    [SerializeField] private StageUnlockReportPageEntry[] _stageUnlockReportPages;

    private bool _panelWasOpen;
    private bool _inputStateCaptured;
    private bool _playerInputWasEnabled;
    private bool _uiInputWasEnabled;
    private bool _inventoryHudStateCaptured;
    private bool _inventoryHudWasActive;
    private StageUnlockReportFlow _stageUnlockReportFlow;

    private void Awake()
    {
        if (_playerInput == null)
        {
            _playerInput = FindAnyObjectByType<PlayerInputHandler>();
        }

        if (_uiInput == null)
        {
            _uiInput = FindAnyObjectByType<UIInputHandler>();
        }

        if (_inventoryHud == null)
        {
            InventoryHudUI inventoryHud = FindAnyObjectByType<InventoryHudUI>();
            _inventoryHud = inventoryHud != null ? inventoryHud.gameObject : null;
        }

        _stageUnlockReportFlow = new StageUnlockReportFlow(
            _upgradeTreeUI,
            _stageUnlockReportDocument,
            _stageUnlockReportPages);
    }

    private void OnEnable()
    {
        _stageUnlockReportFlow?.Enable();
        if (_uiInput != null)
        {
            _uiInput.CancelPressed += HandleCancelPressed;
        }

        SyncWithPanelState();
    }

    private void Update()
    {
        SyncWithPanelState();
    }

    private void OnDisable()
    {
        _stageUnlockReportFlow?.Dispose();
        if (_uiInput != null)
        {
            _uiInput.CancelPressed -= HandleCancelPressed;
        }

        RestoreInputState();
        RestoreInventoryHudState();
    }

    public override void Interact()
    {
        if (_upgradeTreeUI == null)
        {
            return;
        }

        _upgradeTreeUI.Open();
        GameDataManager.Instance?.CompleteEventAndSave(GameProgressEventId.OpenUpgrader);
        SyncWithPanelState();
    }

    private void HandleCancelPressed()
    {
        if (_stageUnlockReportFlow != null && _stageUnlockReportFlow.IsActive)
        {
            _stageUnlockReportDocument?.Close();
            return;
        }

        if (_upgradeTreeUI == null)
        {
            return;
        }

        _upgradeTreeUI.Close();
        SyncWithPanelState();
    }

    private void SyncWithPanelState()
    {
        bool isOpen = _upgradeTreeUI != null && _upgradeTreeUI.gameObject.activeSelf;
        bool reportIsOpen = _stageUnlockReportFlow != null &&
                            _stageUnlockReportFlow.IsActive;
        bool holdsInteractionState = isOpen || reportIsOpen;

        if (holdsInteractionState != _panelWasOpen)
        {
            _panelWasOpen = holdsInteractionState;

            if (holdsInteractionState)
            {
                CaptureAndApplyInputState();
            }
            else
            {
                RestoreInputState();
            }
        }

        // UIInputHandler.Start can run after this component's OnEnable.
        // Keep the required maps in the correct state without pausing the scene.
        if (holdsInteractionState)
        {
            if (_playerInput != null && _playerInput.InputEnabled)
            {
                _playerInput.SetInputEnabled(false);
            }

            if (_uiInput != null && !_uiInput.InputEnabled)
            {
                _uiInput.SetInputEnabled(true);
            }

            HideInventoryHud();
        }
        else
        {
            RestoreInventoryHudState();
        }
    }

    private void CaptureAndApplyInputState()
    {
        if (!_inputStateCaptured)
        {
            _playerInputWasEnabled = _playerInput != null && _playerInput.InputEnabled;
            _uiInputWasEnabled = _uiInput != null && _uiInput.InputEnabled;
            _inputStateCaptured = true;
        }

        _playerInput?.SetInputEnabled(false);
        _uiInput?.SetInputEnabled(true);
    }

    private void RestoreInputState()
    {
        if (!_inputStateCaptured)
        {
            return;
        }

        _uiInput?.SetInputEnabled(_uiInputWasEnabled);
        _playerInput?.SetInputEnabled(_playerInputWasEnabled);
        _inputStateCaptured = false;
    }

    private void HideInventoryHud()
    {
        if (_inventoryHud == null)
        {
            return;
        }

        if (!_inventoryHudStateCaptured)
        {
            _inventoryHudWasActive = _inventoryHud.activeSelf;
            _inventoryHudStateCaptured = true;
        }

        if (_inventoryHud.activeSelf)
        {
            _inventoryHud.SetActive(false);
        }
    }

    private void RestoreInventoryHudState()
    {
        if (!_inventoryHudStateCaptured)
        {
            return;
        }

        if (_inventoryHud != null && _inventoryHud.activeSelf != _inventoryHudWasActive)
        {
            _inventoryHud.SetActive(_inventoryHudWasActive);
        }

        _inventoryHudStateCaptured = false;
    }
}
