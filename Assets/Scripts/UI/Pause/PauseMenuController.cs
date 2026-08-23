using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputHandler))]
public sealed class PauseMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _titleSceneName = "MainMenu";

    [Header("UI")]
    [SerializeField] private PauseMenuView _pauseMenuPrefab;

    private PlayerInputHandler _playerInput;
    private UIInputHandler _uiInput;
    private PauseMenuView _pauseMenuInstance;
    private Button _continueButton;
    private Button _exitButton;
    private GameObject _previousSelection;
    private bool _playerInputWasEnabled;
    private bool _uiInputWasEnabled;
    private bool _isPaused;
    private bool _isLeaving;
    private float _previousTimeScale = 1f;

    public bool IsPaused => _isPaused;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInputHandler>();

        if (_pauseMenuPrefab == null)
        {
            Debug.LogError("PauseMenuController: Pause menu prefab is not assigned.", this);
            enabled = false;
            return;
        }

        _pauseMenuInstance = Instantiate(_pauseMenuPrefab, transform);
        _pauseMenuInstance.name = _pauseMenuPrefab.name;
        _continueButton = _pauseMenuInstance.ContinueButton;
        _exitButton = _pauseMenuInstance.ExitButton;

        if (_continueButton == null || _exitButton == null)
        {
            Debug.LogError("PauseMenuController: Pause menu button references are not assigned.", _pauseMenuInstance);
            enabled = false;
            return;
        }

        _pauseMenuInstance.gameObject.SetActive(false);
    }

    private void Start()
    {
        _uiInput = FindAnyObjectByType<UIInputHandler>();
        _playerInput.CancelPressed += HandleCancelPressed;

        _continueButton.onClick.AddListener(Resume);
        _exitButton.onClick.AddListener(ExitToTitle);
    }

    private void OnDestroy()
    {
        if (_playerInput != null)
        {
            _playerInput.CancelPressed -= HandleCancelPressed;
        }

        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(Resume);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveListener(ExitToTitle);
        }

        if (_isPaused && !_isLeaving)
        {
            RestoreGameplayState();
        }
    }

    private void HandleCancelPressed()
    {
        if (_isLeaving)
        {
            return;
        }

        if (_isPaused)
        {
            Resume();
            return;
        }

        if (_playerInput != null && !_playerInput.InputEnabled)
        {
            return;
        }

        if (SessionManager.Instance != null && SessionManager.Instance.IsSessionFinished)
        {
            return;
        }

        Pause();
    }

    public void Pause()
    {
        if (_isPaused || _isLeaving)
        {
            return;
        }

        _isPaused = true;
        _previousTimeScale = Time.timeScale;
        _playerInputWasEnabled = _playerInput != null && _playerInput.InputEnabled;
        _uiInputWasEnabled = _uiInput != null && _uiInput.InputEnabled;
        _previousSelection = EventSystem.current?.currentSelectedGameObject;

        Time.timeScale = 0f;
        _playerInput?.SetInputEnabled(false);
        _uiInput?.SetInputEnabled(true);

        _pauseMenuInstance.gameObject.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(_continueButton.gameObject);
    }

    public void Resume()
    {
        if (!_isPaused || _isLeaving)
        {
            return;
        }

        RestoreGameplayState();
    }

    public void ExitToTitle()
    {
        if (_isLeaving)
        {
            return;
        }

        _isLeaving = true;
        _continueButton.interactable = false;
        _exitButton.interactable = false;
        Time.timeScale = 1f;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(_titleSceneName);
        }
        else
        {
            Debug.LogError(
                "PauseMenuController: SceneTransitionManager is not available.",
                this);
        }
    }

    private void RestoreGameplayState()
    {
        _pauseMenuInstance.gameObject.SetActive(false);
        Time.timeScale = _previousTimeScale;
        if (_uiInput != null)
        {
            _uiInput.SetInputEnabled(_uiInputWasEnabled);
        }
        if (_playerInput != null)
        {
            _playerInput.SetInputEnabled(_playerInputWasEnabled);
        }
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_previousSelection);
        }
        _previousSelection = null;
        _isPaused = false;
    }
}
