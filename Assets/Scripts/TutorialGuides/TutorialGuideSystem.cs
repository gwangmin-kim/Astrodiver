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
    [SerializeField] private RectTransform _centerPresentationRoot;
    [SerializeField] private TutorialGuideTextUI _itemPrefab;

    [Header("Scene Visibility")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private readonly Dictionary<TutorialGuideDefinition, TutorialGuideTextUI> _items = new();
    private readonly HashSet<TutorialGuideDefinition> _completing = new();
    private readonly Queue<TutorialGuideDefinition> _presentationQueue = new();
    private readonly HashSet<TutorialGuideDefinition> _queuedGuides = new();
    private GameDataManager _gameData;
    private Coroutine _bindRoutine;
    private Coroutine _presentationRoutine;
    private bool _isProcessing;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (_overlayRoot == null || _guideListRoot == null ||
            _centerPresentationRoot == null || _itemPrefab == null)
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
        _gameData.ProgressEventCompleted += HandleProgressEventCompleted;
        _gameData.DataLoaded += HandleDataLoaded;
        RefreshVisibleGuides(false);
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
        RefreshVisibleGuides(false);
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
        if (isMainMenu)
        {
            StopProcessing();
            return;
        }

        StartProcessing();
    }

    private void StartProcessing()
    {
        if (_isProcessing)
        {
            RefreshVisibleGuides(false);
            return;
        }

        _isProcessing = true;
        _overlayRoot.SetActive(true);
        if (_gameData != null)
        {
            RefreshVisibleGuides(false);
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

        StopPresentationQueue();

        Unbind();
        foreach (TutorialGuideTextUI item in _items.Values)
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
        if (!_isProcessing)
        {
            return;
        }

        foreach (TutorialGuideDefinition guide in _gameData.Definitions.TutorialGuides)
        {
            if (guide != null && guide.CompletionEvent == eventId &&
                _items.TryGetValue(guide, out TutorialGuideTextUI item))
            {
                StartCoroutine(CompleteGuide(guide, item));
            }
        }

        RefreshVisibleGuides(true);
    }

    private void RefreshVisibleGuides(bool animateNewGuides)
    {
        if (!_isProcessing || _gameData == null || !_gameData.IsInitialized)
        {
            return;
        }

        if (!animateNewGuides)
        {
            StopPresentationQueue();
        }

        List<TutorialGuideDefinition> visible = new();
        foreach (TutorialGuideDefinition guide in _gameData.Definitions.TutorialGuides)
        {
            if (guide != null && guide.IsVisibleFor(_gameData))
            {
                visible.Add(guide);
                if (animateNewGuides)
                {
                    QueuePresentation(guide);
                }
                else
                {
                    EnsureItemImmediately(guide);
                }
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
            if (_items.TryGetValue(visible[index], out TutorialGuideTextUI item))
            {
                if (animateNewGuides)
                {
                    item.transform.SetSiblingIndex(index);
                }
                else
                {
                    item.PlaceInListImmediately(_guideListRoot, index);
                }
            }
        }

        if (animateNewGuides)
        {
            StartPresentationQueue();
        }
    }

    private void EnsureItemImmediately(TutorialGuideDefinition guide)
    {
        if (_items.ContainsKey(guide))
        {
            return;
        }

        TutorialGuideTextUI item = Instantiate(_itemPrefab, _guideListRoot);
        item.name = $"Guide_{guide.Key}";
        item.Bind(guide.Text);
        _items.Add(guide, item);
    }

    private void QueuePresentation(TutorialGuideDefinition guide)
    {
        if (_items.ContainsKey(guide) || !_queuedGuides.Add(guide))
        {
            return;
        }

        _presentationQueue.Enqueue(guide);
    }

    private void StartPresentationQueue()
    {
        if (_presentationRoutine == null && _presentationQueue.Count > 0)
        {
            _presentationRoutine = StartCoroutine(ProcessPresentationQueue());
        }
    }

    private void StopPresentationQueue()
    {
        if (_presentationRoutine != null)
        {
            StopCoroutine(_presentationRoutine);
            _presentationRoutine = null;
        }

        _presentationQueue.Clear();
        _queuedGuides.Clear();
    }

    private IEnumerator ProcessPresentationQueue()
    {
        while (_isProcessing && _presentationQueue.Count > 0)
        {
            TutorialGuideDefinition guide = _presentationQueue.Dequeue();
            _queuedGuides.Remove(guide);
            if (guide == null || !guide.IsVisibleFor(_gameData) ||
                _items.ContainsKey(guide))
            {
                continue;
            }

            TutorialGuideTextUI item = Instantiate(_itemPrefab, _guideListRoot);
            int siblingIndex = GetSiblingIndex(guide);
            item.name = $"Guide_{guide.Key}";
            item.Bind(guide.Text);
            _items.Add(guide, item);
            yield return item.PlayPresentation(
                _guideListRoot, _centerPresentationRoot, siblingIndex);
        }

        _presentationRoutine = null;
        StartPresentationQueue();
    }

    private int GetSiblingIndex(TutorialGuideDefinition guide)
    {
        int index = 0;
        foreach (TutorialGuideDefinition existing in _items.Keys)
        {
            if (CompareGuides(existing, guide) < 0)
            {
                index++;
            }
        }

        return index;
    }

    private IEnumerator CompleteGuide(
        TutorialGuideDefinition guide,
        TutorialGuideTextUI item)
    {
        if (!_completing.Add(guide))
        {
            yield break;
        }

        _items.Remove(guide);
        item.transform.SetParent(_centerPresentationRoot, true);
        yield return item.PlayCompletionAnimation();
        _completing.Remove(guide);
        Destroy(item.gameObject);
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
}
