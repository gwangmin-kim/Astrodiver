using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Scene Settings")]
    [SerializeField] private string _hubSceneName = "Hub";

    [Header("Player Spawn")]
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private Transform _runtimeRoot;
    [SerializeField] private StageSceneSettings _stageSettings;

    [Header("Session End UI (Optional)")]
    [SerializeField] private GameObject _sessionEndPanel;
    [SerializeField] private Button _returnButton;
    [SerializeField] private Button _retryButton;

    private bool _sessionStarted;
    private GameObject _timeoutInventoryLossMessage;

    public bool IsSessionFinished { get; private set; }
    public PlayerContext SpawnedPlayer { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SpawnedPlayer = SpawnPlayer();
        if (SpawnedPlayer != null)
        {
            InitializeStageGravity();
            if (GameDataManager.Instance != null)
                GameDataManager.Instance.CompleteEventAndSave(
                    GameProgressEventId.ExploreFirstTime);
        }

        _timeoutInventoryLossMessage = _sessionEndPanel.transform
            .Find("Dialog/Timeout Inventory Loss Message")?.gameObject;

        _sessionEndPanel.SetActive(false);
        if (_timeoutInventoryLossMessage != null)
        {
            _timeoutInventoryLossMessage.SetActive(false);
        }

        _returnButton.onClick.AddListener(ReturnToHub);
        _retryButton.onClick.AddListener(RetryExploration);
    }

    private PlayerContext SpawnPlayer()
    {
        PlayerContext existingPlayer = PlayerContext.Instance;
        if (existingPlayer != null)
        {
            Debug.LogWarning(
                "SessionManager: A PlayerContext already exists; reusing that player.",
                existingPlayer);
            return existingPlayer;
        }

        if (_playerPrefab == null || _playerSpawnPoint == null || _runtimeRoot == null)
        {
            Debug.LogError(
                "SessionManager: Player prefab, SpaceShip spawn point, and Runtime root must be assigned.",
                this);
            return null;
        }

        if (_playerPrefab.GetComponent<PlayerContext>() == null)
        {
            Debug.LogError(
                "SessionManager: The player prefab does not contain PlayerContext.",
                _playerPrefab);
            return null;
        }

        GameObject playerObject = Instantiate(
            _playerPrefab,
            _playerSpawnPoint.position,
            _playerSpawnPoint.rotation,
            _runtimeRoot);
        playerObject.name = _playerPrefab.name;
        return playerObject.GetComponent<PlayerContext>();
    }

    private void InitializeStageGravity()
    {
        if (_stageSettings == null)
        {
            Debug.LogError(
                "SessionManager: StageSceneSettings reference is missing; player gravity was not initialized.",
                this);
            return;
        }

        StageDefinition definition = _stageSettings.Definition;
        if (definition == null)
        {
            Debug.LogError(
                "SessionManager: StageDefinition reference is missing; player gravity was not initialized.",
                _stageSettings);
            return;
        }

        if (!SpawnedPlayer.TryGetComponent<PlayerMovementController>(out var movement))
        {
            Debug.LogError(
                "SessionManager: SessionPlayer is missing PlayerMovementController; player gravity was not initialized.",
                SpawnedPlayer);
            return;
        }

        movement.InitializeForStage(definition.GravityScale);
    }

    private void Start()
    {
        if (SpawnedPlayer == null)
        {
            return;
        }

        PlayerInventoryController inventory = PlayerInventoryController.Instance;
        if (inventory == null)
        {
            Debug.LogError("SessionManager: Player inventory not found.", this);
            return;
        }

        _sessionStarted = inventory.BeginExploreSession();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        if (_returnButton != null) _returnButton.onClick.RemoveListener(ReturnToHub);
        if (_retryButton != null) _retryButton.onClick.RemoveListener(RetryExploration);

        if (_sessionStarted && !IsSessionFinished)
        {
            if (PlayerInventoryController.Instance != null)
            {
                PlayerInventoryController.Instance.CancelExploreSession();
            }
        }

        Time.timeScale = 1f;
        Instance = null;
    }

    public void FinishSessionByReturn()
    {
        FinishSession(false);
    }

    public void FinishSessionByTimeout()
    {
        FinishSession(true);
    }

    private void FinishSession(bool isTimeout)
    {
        if (IsSessionFinished || !_sessionStarted)
        {
            return;
        }

        IsSessionFinished = true;

        PlayerInventoryController inventory = PlayerInventoryController.Instance;

        if (inventory == null)
        {
            Debug.LogWarning("SessionManager: Player inventory not found.", this);
            IsSessionFinished = false;
            return;
        }

        float lossRatio = isTimeout
            ? GameDataManager.Instance.RuntimeData.Inventory.TimeoutInventoryLossRatio
            : 0f;
        if (!inventory.CompleteExploreSession(lossRatio))
        {
            Debug.LogError("SessionManager: Failed to save the completed exploration session.", this);
            IsSessionFinished = false;
            return;
        }

        if (!isTimeout)
        {
            GameDataManager.Instance?.CompleteEventAndSave(
                GameProgressEventId.ReturnSafely);
        }

        if (_timeoutInventoryLossMessage != null)
        {
            _timeoutInventoryLossMessage.SetActive(isTimeout);
        }

        Time.timeScale = 0f;
        _sessionEndPanel.SetActive(true);
    }

    private void ReturnToHub()
    {
        Time.timeScale = 1f;
        SceneTransitionManager.Instance.LoadHub(
            _hubSceneName,
            HubSpawnPoint.Return);
    }

    private void RetryExploration()
    {
        LoadScene(gameObject.scene.name);
    }

    private static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        Time.timeScale = 1f;
        SceneTransitionManager.Instance.LoadScene(sceneName);
    }
}
