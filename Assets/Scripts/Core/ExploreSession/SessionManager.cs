using UnityEngine;
using UnityEngine.UI;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Scene Settings")]
    [SerializeField] private string _hubSceneName = "Hub";

    [Header("Timeout Penalty")]
    [SerializeField][Range(0f, 1f)] private float _timeoutInventoryLossRatio = 1f;

    [Header("Session End UI (Optional)")]
    [SerializeField] private GameObject _sessionEndPanel;
    [SerializeField] private Button _returnButton;
    [SerializeField] private Button _retryButton;

    private bool _sessionStarted;

    public bool IsSessionFinished { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        _sessionEndPanel.SetActive(false);

        _returnButton.onClick.AddListener(ReturnToHub);
        _retryButton.onClick.AddListener(RetryExploration);
    }

    private void Start()
    {
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

        float lossRatio = isTimeout ? _timeoutInventoryLossRatio : 0f;
        if (!inventory.CompleteExploreSession(lossRatio))
        {
            Debug.LogError("SessionManager: Failed to save the completed exploration session.", this);
            IsSessionFinished = false;
            return;
        }

        Time.timeScale = 0f;
        _sessionEndPanel.SetActive(true);
    }

    private void ReturnToHub()
    {
        LoadScene(_hubSceneName);
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
