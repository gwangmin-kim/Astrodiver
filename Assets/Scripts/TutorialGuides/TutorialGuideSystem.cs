using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-9000)]
[DisallowMultipleComponent]
public sealed class TutorialGuideSystem : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject _overlayRoot;
    [SerializeField] private RectTransform _guideListRoot;
    [SerializeField] private TutorialGuideItemUI _itemPrefab;

    [Header("Scene Visibility")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private readonly Dictionary<TutorialGuideDefinition, TutorialGuideItemUI> _items = new();
    private readonly HashSet<TutorialGuideDefinition> _completing = new();
    private GameDataManager _gameData;
    private Coroutine _bindRoutine;
    private bool _isProcessing;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (_overlayRoot == null || _guideListRoot == null || _itemPrefab == null)
        {
            Debug.LogError(
                "Tutorial guide prefab references are not configured.",
                this);
            enabled = false;
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        UpdateProcessingForScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        StopProcessing();
    }

    private IEnumerator BindWhenAvailable()
    {
        while (_isProcessing && GameDataManager.Instance == null)
        {
            yield return null;
        }

        _bindRoutine = null;
        if (!_isProcessing || !isActiveAndEnabled)
        {
            yield break;
        }

        _gameData = GameDataManager.Instance;
        if (_gameData.IsMemoryOnlySession)
        {
            StopProcessing();
            yield break;
        }

        _gameData.ProgressEventCompleted += HandleProgressEventCompleted;
        _gameData.DataLoaded += HandleDataLoaded;
        RefreshVisibleGuides();
    }

    private void Unbind()
    {
        if (_gameData == null)
        {
            return;
        }

        _gameData.ProgressEventCompleted -= HandleProgressEventCompleted;
        _gameData.DataLoaded -= HandleDataLoaded;
        _gameData = null;
    }

    private void HandleDataLoaded(GameSaveData _)
    {
        if (_gameData != null && _gameData.IsMemoryOnlySession)
        {
            StopProcessing();
            return;
        }

        RefreshVisibleGuides();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode _)
    {
        UpdateProcessingForScene(scene);
    }

    private void UpdateProcessingForScene(Scene scene)
    {
        bool isMainMenu = string.Equals(
            scene.name,
            _mainMenuSceneName,
            StringComparison.Ordinal);
        if (isMainMenu || IsMemoryOnlySession())
        {
            StopProcessing();
            return;
        }

        StartProcessing();
    }

    private void StartProcessing()
    {
        if (IsMemoryOnlySession())
        {
            StopProcessing();
            return;
        }

        if (_isProcessing)
        {
            RefreshVisibleGuides();
            return;
        }

        _isProcessing = true;
        _overlayRoot.SetActive(true);
        if (_gameData != null)
        {
            RefreshVisibleGuides();
        }
        else
        {
            _bindRoutine = StartCoroutine(BindWhenAvailable());
        }
    }

    private void StopProcessing()
    {
        _isProcessing = false;
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        Unbind();
        foreach (TutorialGuideItemUI item in _items.Values)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }

        _items.Clear();
        _completing.Clear();
        if (_overlayRoot != null)
        {
            _overlayRoot.SetActive(false);
        }
    }

    private void HandleProgressEventCompleted(GameProgressEventId eventId)
    {
        if (!_isProcessing || _gameData == null || _gameData.IsMemoryOnlySession)
        {
            return;
        }

        foreach (TutorialGuideDefinition guide in _gameData.Definitions.TutorialGuides)
        {
            if (guide != null && guide.CompletionEvent == eventId &&
                _items.TryGetValue(guide, out TutorialGuideItemUI item))
            {
                StartCoroutine(CompleteGuide(guide, item));
            }
        }

        RefreshVisibleGuides();
    }

    private void RefreshVisibleGuides()
    {
        if (_gameData != null && _gameData.IsMemoryOnlySession)
        {
            StopProcessing();
            return;
        }

        if (!_isProcessing || _gameData == null || !_gameData.IsInitialized)
        {
            return;
        }

        List<TutorialGuideDefinition> visible = new();
        foreach (TutorialGuideDefinition guide in _gameData.Definitions.TutorialGuides)
        {
            if (guide != null && guide.IsVisibleFor(_gameData))
            {
                visible.Add(guide);
                EnsureItem(guide);
            }
        }

        foreach (TutorialGuideDefinition guide in new List<TutorialGuideDefinition>(_items.Keys))
        {
            if (!visible.Contains(guide) && !_completing.Contains(guide))
            {
                Destroy(_items[guide].gameObject);
                _items.Remove(guide);
            }
        }

        visible.Sort(CompareGuides);
        for (int index = 0; index < visible.Count; index++)
        {
            if (_items.TryGetValue(visible[index], out TutorialGuideItemUI item))
            {
                item.transform.SetSiblingIndex(index);
            }
        }
    }

    private void EnsureItem(TutorialGuideDefinition guide)
    {
        if (_items.ContainsKey(guide))
        {
            return;
        }

        TutorialGuideItemUI item = Instantiate(_itemPrefab, _guideListRoot);
        item.name = $"Guide_{guide.Key}";
        item.Bind(guide.Text);
        _items.Add(guide, item);
    }

    private IEnumerator CompleteGuide(
        TutorialGuideDefinition guide,
        TutorialGuideItemUI item)
    {
        if (!_completing.Add(guide))
        {
            yield break;
        }

        yield return item.PlayCompletionAnimation();
        _items.Remove(guide);
        _completing.Remove(guide);
        Destroy(item.gameObject);
        RefreshVisibleGuides();
    }

    private static int CompareGuides(
        TutorialGuideDefinition left,
        TutorialGuideDefinition right)
    {
        int order = left.SortOrder.CompareTo(right.SortOrder);
        return order != 0
            ? order
            : string.Compare(left.Key, right.Key, StringComparison.Ordinal);
    }

    private static bool IsMemoryOnlySession()
    {
        return GameDataManager.Instance != null &&
            GameDataManager.Instance.IsMemoryOnlySession;
    }
}
