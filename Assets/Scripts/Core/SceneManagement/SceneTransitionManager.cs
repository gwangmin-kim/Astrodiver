using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private bool _isLoading;
    private bool _hasPendingHubSpawnPoint;
    private HubSpawnPoint _pendingHubSpawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    public void LoadScene(string sceneName)
    {
        if (_isLoading || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        _isLoading = true;
        SceneManager.LoadSceneAsync(sceneName);
    }

    /// <summary>
    /// Loads the Hub and records the spawn point for that single transition.
    /// </summary>
    public bool LoadHub(string sceneName, HubSpawnPoint spawnPoint)
    {
        if (_isLoading || string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        _isLoading = true;
        _pendingHubSpawnPoint = spawnPoint;
        _hasPendingHubSpawnPoint = true;
        SceneManager.LoadSceneAsync(sceneName);
        return true;
    }

    /// <summary>
    /// Returns the spawn point requested for the current Hub load. Direct Hub
    /// launches and all unspecified transitions intentionally use Return.
    /// </summary>
    public HubSpawnPoint ConsumeHubSpawnPoint()
    {
        if (!_hasPendingHubSpawnPoint)
        {
            return HubSpawnPoint.Return;
        }

        _hasPendingHubSpawnPoint = false;
        return _pendingHubSpawnPoint;
    }

    public void LoadScene(int buildIndex)
    {
        if (_isLoading || buildIndex < 0)
        {
            return;
        }

        _isLoading = true;
        SceneManager.LoadSceneAsync(buildIndex);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isLoading = false;
    }
}

public enum HubSpawnPoint
{
    Start,
    Return
}
