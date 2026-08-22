using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string _hubSceneName = "Hub";

    [Header("Buttons")]
    [SerializeField] private Button _newGameButton;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _quitButton;

    [Header("Labels")]
    [SerializeField] private TMP_Text _continueLabel;
    [SerializeField] private TMP_Text _quitLabel;

    [Header("Colors")]
    [SerializeField] private Color _enabledLabelColor = Color.white;
    [SerializeField] private Color _disabledLabelColor = new(0.42f, 0.45f, 0.52f, 1f);
    [SerializeField] private Color _quitConfirmationColor = new(0.92f, 0.18f, 0.2f, 1f);

    private const string QuitText = "게임 종료";
    private const string QuitConfirmationText = "정말로 종료하시겠습니까?";
    private bool _quitConfirmationRequested;
    private bool _isStartingGame;

    private void Awake()
    {
        _newGameButton.onClick.AddListener(StartNewGame);
        _continueButton.onClick.AddListener(ContinueGame);
        _quitButton.onClick.AddListener(RequestQuit);
        RefreshContinueState();
    }

    private void OnDestroy()
    {
        _newGameButton.onClick.RemoveListener(StartNewGame);
        _continueButton.onClick.RemoveListener(ContinueGame);
        _quitButton.onClick.RemoveListener(RequestQuit);
    }

    private void StartNewGame()
    {
        if (!TryBeginGameStart(out GameDataManager manager))
        {
            return;
        }

        if (!manager.TryStartNewGame(out string error))
        {
            HandleGameStartFailure("start a new game", error);
            return;
        }

        if (!LoadHub())
        {
            HandleGameStartFailure("load the Hub scene", "SceneTransitionManager is not available.");
        }
    }

    private void ContinueGame()
    {
        if (!TryBeginGameStart(out GameDataManager manager))
        {
            return;
        }

        if (!manager.TryLoadSavedGame(out string error))
        {
            HandleGameStartFailure("continue the saved game", error);
            return;
        }

        if (!LoadHub())
        {
            HandleGameStartFailure("load the Hub scene", "SceneTransitionManager is not available.");
        }
    }

    private void RequestQuit()
    {
        if (!_quitConfirmationRequested)
        {
            _quitConfirmationRequested = true;
            _quitLabel.text = QuitConfirmationText;
            _quitLabel.color = _quitConfirmationColor;
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    internal void CancelQuitConfirmation()
    {
        if (!_quitConfirmationRequested)
        {
            return;
        }

        _quitConfirmationRequested = false;
        _quitLabel.text = QuitText;
        _quitLabel.color = _enabledLabelColor;
    }

    private void RefreshContinueState()
    {
        bool canContinue = GameDataManager.Instance != null &&
            GameDataManager.Instance.HasSaveData &&
            !_isStartingGame;
        _continueButton.interactable = canContinue;
        _continueLabel.color = canContinue
            ? _enabledLabelColor
            : _disabledLabelColor;
    }

    private bool TryBeginGameStart(out GameDataManager manager)
    {
        manager = GameDataManager.Instance;
        if (_isStartingGame || manager == null)
        {
            if (manager == null)
            {
                Debug.LogError("MainMenuController: GameDataManager is not available.", this);
            }

            return false;
        }

        _isStartingGame = true;
        CancelQuitConfirmation();
        _newGameButton.interactable = false;
        _continueButton.interactable = false;
        _quitButton.interactable = false;
        return true;
    }

    private void HandleGameStartFailure(string operation, string error)
    {
        Debug.LogError($"MainMenuController: Could not {operation}. {error}", this);
        _isStartingGame = false;
        _newGameButton.interactable = true;
        _quitButton.interactable = true;
        RefreshContinueState();
    }

    private bool LoadHub()
    {
        if (SceneTransitionManager.Instance == null)
        {
            return false;
        }

        return SceneTransitionManager.Instance.LoadHub(
            _hubSceneName,
            HubSpawnPoint.Start);
    }
}
