using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private bool _isLoading;

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
