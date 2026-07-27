using UnityEngine;

public sealed class UpgradeInteractor : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private GameObject _upgradePanel;
    [SerializeField] private PlayerInputHandler _playerInput;
    [SerializeField] private UIInputHandler _uiInput;

    private bool _panelWasOpen;
    private bool _inputStateCaptured;
    private bool _playerInputWasEnabled;
    private bool _uiInputWasEnabled;

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
    }

    private void OnEnable()
    {
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
        if (_uiInput != null)
        {
            _uiInput.CancelPressed -= HandleCancelPressed;
        }

        RestoreInputState();
    }

    public void Interact()
    {
        if (_upgradePanel == null || _upgradePanel.activeSelf)
        {
            return;
        }

        _upgradePanel.SetActive(true);
        SyncWithPanelState();
    }

    private void HandleCancelPressed()
    {
        if (_upgradePanel == null || !_upgradePanel.activeSelf)
        {
            return;
        }

        _upgradePanel.SetActive(false);
        SyncWithPanelState();
    }

    private void SyncWithPanelState()
    {
        bool isOpen = _upgradePanel != null && _upgradePanel.activeSelf;

        if (isOpen != _panelWasOpen)
        {
            _panelWasOpen = isOpen;

            if (isOpen)
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
        if (isOpen)
        {
            if (_playerInput != null && _playerInput.InputEnabled)
            {
                _playerInput.SetInputEnabled(false);
            }

            if (_uiInput != null && !_uiInput.InputEnabled)
            {
                _uiInput.SetInputEnabled(true);
            }
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
}
