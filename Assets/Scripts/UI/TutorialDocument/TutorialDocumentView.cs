using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Displays a collection of complete page GameObjects as a modal, paged document.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialDocumentView : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private CanvasGroup _dimmer;
    [SerializeField] private Transform _documentMotionRoot;
    [SerializeField] private CanvasGroup _documentCanvasGroup;
    [SerializeField] private List<GameObject> _pages = new();

    [Header("Navigation")]
    [SerializeField] private Button _previousButton;
    [SerializeField] private Button _nextOrCloseButton;
    [SerializeField] private Image _nextOrCloseIcon;
    [SerializeField] private Sprite _rightSprite;
    [SerializeField] private Sprite _closeSprite;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float _showDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float _hideDuration = 0.18f;
    [SerializeField, Min(1f)] private float _slideOffset = 900f;
    [SerializeField] private Ease _showEase = Ease.OutCubic;
    [SerializeField] private Ease _hideEase = Ease.InCubic;

    private PlayerInputHandler _playerInput;
    private UIInputHandler _uiInput;
    private GameObject _inventoryHud;
    private Vector3 _shownPosition;
    private GameObject _previousSelection;
    private bool _previousPlayerInputEnabled;
    private bool _previousUiInputEnabled;
    private bool _inventoryHudStateCaptured;
    private bool _inventoryHudWasActive;
    private bool _initialized;
    private bool _closing;
    private int _pageIndex;
    private Tween _dimmerTween;
    private Tween _documentAlphaTween;
    private Coroutine _motionRoutine;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (_initialized && !IsOpen)
        {
            SetVisualStateImmediate(false);
        }
    }

    private void OnDisable()
    {
        StopTweens();
        if (IsOpen)
        {
            RestoreInput();
            RestoreInventoryHud();
            IsOpen = false;
        }
    }

    private void OnDestroy()
    {
        if (_previousButton != null)
        {
            _previousButton.onClick.RemoveListener(ShowPreviousPage);
        }

        if (_nextOrCloseButton != null)
        {
            _nextOrCloseButton.onClick.RemoveListener(ShowNextPageOrClose);
        }
    }

    private void Update()
    {
        if (!IsOpen || _closing || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            ShowPreviousPage();
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            ShowNextPageOrClose();
        }

        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Open()
    {
        Initialize();
        if (IsOpen || _pages.Count == 0)
        {
            return;
        }

        gameObject.SetActive(true);
        IsOpen = true;
        _closing = false;
        _pageIndex = 0;
        SaveAndDisableGameplayInput();
        HideInventoryHud();
        RefreshPage();

        StopTweens();
        _dimmer.alpha = 0f;
        _documentCanvasGroup.alpha = 0f;
        _documentMotionRoot.localPosition = HiddenPosition;
        _dimmer.blocksRaycasts = true;
        _dimmer.interactable = true;

        _dimmerTween = Tween.Alpha(_dimmer, 1f, _showDuration, _showEase);
        _documentAlphaTween = Tween.Alpha(
            _documentCanvasGroup, 1f, _showDuration, _showEase);
        _motionRoutine = StartCoroutine(AnimateDocumentPosition(
            HiddenPosition, _shownPosition, _showDuration, _showEase));
        EventSystem.current?.SetSelectedGameObject(_nextOrCloseButton?.gameObject);
    }

    public void Close()
    {
        if (!IsOpen || _closing)
        {
            return;
        }

        _closing = true;
        StopTweens();
        _dimmer.interactable = false;
        _documentAlphaTween = Tween.Alpha(
            _documentCanvasGroup, 0f, _hideDuration, _hideEase);
        _dimmerTween = Tween.Alpha(_dimmer, 0f, _hideDuration, _hideEase);
        _motionRoutine = StartCoroutine(AnimateDocumentPosition(
            _documentMotionRoot.localPosition,
            HiddenPosition,
            _hideDuration,
            _hideEase));
        StartCoroutine(CloseAfterAnimation());
    }

    public void ShowPreviousPage()
    {
        if (IsOpen && !_closing && _pageIndex > 0)
        {
            _pageIndex--;
            RefreshPage();
        }
    }

    public void ShowNextPageOrClose()
    {
        if (!IsOpen || _closing)
        {
            return;
        }

        if (_pageIndex >= _pages.Count - 1)
        {
            Close();
            return;
        }

        _pageIndex++;
        RefreshPage();
    }

    private IEnumerator CloseAfterAnimation()
    {
        yield return new WaitForSecondsRealtime(_hideDuration);
        RestoreInput();
        RestoreInventoryHud();
        IsOpen = false;
        _closing = false;
        gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _dimmer ??= GetComponent<CanvasGroup>();
        _documentMotionRoot ??= transform;
        _documentCanvasGroup ??= _documentMotionRoot.GetComponent<CanvasGroup>();
        _documentCanvasGroup ??= _documentMotionRoot.gameObject.AddComponent<CanvasGroup>();
        PopulatePagesFromContentRoot();
        _shownPosition = _documentMotionRoot.localPosition;

        if (_previousButton != null)
        {
            _previousButton.onClick.AddListener(ShowPreviousPage);
        }

        if (_nextOrCloseButton != null)
        {
            _nextOrCloseButton.onClick.AddListener(ShowNextPageOrClose);
        }

        _initialized = true;
    }

    private void PopulatePagesFromContentRoot()
    {
        bool hasAssignedPage = false;
        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pages[i] != null)
            {
                hasAssignedPage = true;
                break;
            }
        }

        if (hasAssignedPage)
        {
            return;
        }

        Transform contentRoot = transform.Find(
            "DocumentMotionRoot/PaperFrame/ContentRoot");
        if (contentRoot == null)
        {
            return;
        }

        _pages.Clear();
        for (int i = 0; i < contentRoot.childCount; i++)
        {
            _pages.Add(contentRoot.GetChild(i).gameObject);
        }
    }

    private void RefreshPage()
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pages[i] != null)
            {
                _pages[i].SetActive(i == _pageIndex);
            }
        }

        bool isFirstPage = _pageIndex == 0;
        bool isLastPage = _pageIndex >= _pages.Count - 1;
        if (_previousButton != null)
        {
            _previousButton.gameObject.SetActive(!isFirstPage);
        }

        if (_nextOrCloseIcon != null)
        {
            _nextOrCloseIcon.sprite = isLastPage ? _closeSprite : _rightSprite;
        }
    }

    private void SaveAndDisableGameplayInput()
    {
        _playerInput ??= FindAnyObjectByType<PlayerInputHandler>();
        _uiInput ??= FindAnyObjectByType<UIInputHandler>();
        _previousPlayerInputEnabled = _playerInput != null && _playerInput.InputEnabled;
        _previousUiInputEnabled = _uiInput != null && _uiInput.InputEnabled;
        _previousSelection = EventSystem.current?.currentSelectedGameObject;

        _playerInput?.SetInputEnabled(false);
        _uiInput?.SetInputEnabled(true);
    }

    private void RestoreInput()
    {
        _playerInput?.SetInputEnabled(_previousPlayerInputEnabled);
        _uiInput?.SetInputEnabled(_previousUiInputEnabled);
        EventSystem.current?.SetSelectedGameObject(_previousSelection);
        _previousSelection = null;
    }

    private void HideInventoryHud()
    {
        if (!_inventoryHudStateCaptured)
        {
            InventoryHudUI inventoryHud = FindAnyObjectByType<InventoryHudUI>();
            _inventoryHud = inventoryHud != null ? inventoryHud.gameObject : null;
            _inventoryHudWasActive = _inventoryHud != null && _inventoryHud.activeSelf;
            _inventoryHudStateCaptured = true;
        }

        if (_inventoryHud != null)
        {
            _inventoryHud.SetActive(false);
        }
    }

    private void RestoreInventoryHud()
    {
        if (!_inventoryHudStateCaptured)
        {
            return;
        }

        if (_inventoryHud != null)
        {
            _inventoryHud.SetActive(_inventoryHudWasActive);
        }

        _inventoryHudStateCaptured = false;
    }

    private void SetVisualStateImmediate(bool visible)
    {
        _dimmer.alpha = visible ? 1f : 0f;
        _dimmer.blocksRaycasts = visible;
        _dimmer.interactable = visible;
        _documentCanvasGroup.alpha = visible ? 1f : 0f;
        _documentMotionRoot.localPosition = visible
            ? _shownPosition
            : HiddenPosition;
    }

    private Vector3 HiddenPosition =>
        _shownPosition + Vector3.down * _slideOffset;

    private IEnumerator AnimateDocumentPosition(
        Vector3 from,
        Vector3 to,
        float duration,
        Ease ease)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            _documentMotionRoot.localPosition = Vector3.LerpUnclamped(
                from,
                to,
                Easing.Evaluate(progress, ease));
            yield return null;
        }

        _documentMotionRoot.localPosition = to;
        _motionRoutine = null;
    }

    private void StopTweens()
    {
        _dimmerTween.Stop();
        _documentAlphaTween.Stop();
        if (_motionRoutine != null)
        {
            StopCoroutine(_motionRoutine);
            _motionRoutine = null;
        }
    }
}
