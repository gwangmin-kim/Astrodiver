namespace Astrodiver.SceneManagement
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public sealed class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        private bool _isLoading;
        private bool _hasPendingHubSpawnPoint;
        private HubSpawnPoint _pendingHubSpawnPoint;
        private IrisSceneTransition _irisTransition;
        private AsyncOperation _introHubLoad;
        private bool _isActivatingIntroHub;

        public bool IsLoading => _isLoading;
        public bool IsIntroHubReady => _introHubLoad != null && _introHubLoad.progress >= 0.9f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _irisTransition = GetComponent<IrisSceneTransition>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadScene(string sceneName)
        {
            if (!CanLoadScene(sceneName))
            {
                return;
            }

            BeginLoad(
                () => SceneManager.LoadSceneAsync(sceneName),
                $"scene '{sceneName}'");
        }

        public bool LoadIntro(string sceneName)
        {
            return CanLoadScene(sceneName) && BeginLoad(
                () => SceneManager.LoadSceneAsync(sceneName),
                $"intro scene '{sceneName}'");
        }

        /// <summary>
        /// The intro owns the loading screen. Keep the Hub inactive until its letter is accepted.
        /// This reserves the scene-loading queue, so other transitions must not start meanwhile.
        /// </summary>
        public bool PreloadHubForIntro(string sceneName)
        {
            if (_isLoading || !CanLoadScene(sceneName) ||
                _irisTransition == null || !_irisTransition.IsAvailable)
            {
                return false;
            }

            try
            {
                _introHubLoad = SceneManager.LoadSceneAsync(sceneName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Could not preload the intro Hub: {exception.Message}", this);
                return false;
            }

            if (_introHubLoad == null)
            {
                return false;
            }

            _introHubLoad.allowSceneActivation = false;
            _isActivatingIntroHub = false;
            _isLoading = true;
            return true;
        }

        public bool ActivateIntroHub()
        {
            if (!IsIntroHubReady || _isActivatingIntroHub)
            {
                return false;
            }

            _isActivatingIntroHub = true;
            _pendingHubSpawnPoint = HubSpawnPoint.Start;
            _hasPendingHubSpawnPoint = true;
            StartCoroutine(ActivateIntroHubRoutine());
            return true;
        }

        private IEnumerator ActivateIntroHubRoutine()
        {
            yield return _irisTransition.Close();
            _introHubLoad.allowSceneActivation = true;
            yield return _introHubLoad;
            _introHubLoad = null;
            yield return null;
            yield return _irisTransition.Open();
            _isActivatingIntroHub = false;
            _isLoading = false;
        }

        /// <summary>
        /// Loads the Hub and records the spawn point for that single transition.
        /// </summary>
        public bool LoadHub(string sceneName, HubSpawnPoint spawnPoint)
        {
            if (_isLoading || !CanLoadScene(sceneName))
            {
                return false;
            }

            bool started = BeginLoad(
                () => SceneManager.LoadSceneAsync(sceneName),
                $"Hub scene '{sceneName}'");
            if (!started)
            {
                return false;
            }

            _pendingHubSpawnPoint = spawnPoint;
            _hasPendingHubSpawnPoint = true;
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
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"Scene build index is out of range: {buildIndex}.", this);
                return;
            }

            BeginLoad(
                () => SceneManager.LoadSceneAsync(buildIndex),
                $"build index {buildIndex}");
        }

        private bool BeginLoad(Func<AsyncOperation> loadOperationFactory, string destinationDescription)
        {
            if (_isLoading)
            {
                return false;
            }

            if (_irisTransition == null || !_irisTransition.IsAvailable)
            {
                Debug.LogError(
                    "SceneTransitionManager requires an available IrisSceneTransition component.",
                    this);
                return false;
            }

            _isLoading = true;
            StartCoroutine(LoadSceneRoutine(loadOperationFactory, destinationDescription));
            return true;
        }

        private IEnumerator LoadSceneRoutine(
            Func<AsyncOperation> loadOperationFactory,
            string destinationDescription)
        {
            yield return _irisTransition.Close();

            AsyncOperation loadOperation = loadOperationFactory();
            if (loadOperation == null)
            {
                Debug.LogError($"Could not start loading {destinationDescription}.", this);
                yield return _irisTransition.Open();
                _isLoading = false;
                yield break;
            }

            loadOperation.allowSceneActivation = false;
            while (loadOperation.progress < 0.9f)
            {
                yield return null;
            }

            loadOperation.allowSceneActivation = true;
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            // Let scene Awake/Start work and the new camera render once while still covered.
            yield return null;
            yield return _irisTransition.Open();
            _isLoading = false;
        }

        private bool CanLoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                return true;
            }

            Debug.LogError($"Scene cannot be loaded because it is not in Build Settings: {sceneName}.", this);
            return false;
        }
    }

    public enum HubSpawnPoint
    {
        Start,
        Return
    }
}
