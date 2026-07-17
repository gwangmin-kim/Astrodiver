using UnityEngine;
using UnityEngine.UI;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Scene Settings")]
    [SerializeField] private string _hubSceneName = "Hub";
    [SerializeField] private string _explorationSceneName = "Game";

    [Header("Timeout Penalty")]
    [SerializeField][Range(0f, 1f)] private float _timeoutInventoryLossRatio = 1f;

    [Header("Session End UI (Optional)")]
    [SerializeField] private GameObject _sessionEndPanel;
    [SerializeField] private Button _returnButton;
    [SerializeField] private Button _retryButton;

    public bool IsSessionFinished { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // _explorationSceneName = gameObject.scene.name;

        EnsureSessionEndUi();
        _sessionEndPanel.SetActive(false);

        _returnButton.onClick.AddListener(ReturnToHub);
        _retryButton.onClick.AddListener(RetryExploration);
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        if (_returnButton != null) _returnButton.onClick.RemoveListener(ReturnToHub);
        if (_retryButton != null) _retryButton.onClick.RemoveListener(RetryExploration);
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
        if (IsSessionFinished)
        {
            return;
        }

        IsSessionFinished = true;

        if (isTimeout)
        {
            PlayerInventoryController inventory = PlayerContext.Instance.Inventory;
            inventory.LoseSessionInventory(_timeoutInventoryLossRatio);
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
        LoadScene(_explorationSceneName);
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

    private void EnsureSessionEndUi()
    {
        if (_sessionEndPanel != null && _returnButton != null && _retryButton != null)
        {
            return;
        }

        Canvas canvas = CreateCanvas();
        _sessionEndPanel = CreateImageObject("Session End Panel", canvas.transform, new Color(0f, 0f, 0f, 0.75f));
        StretchToParent((RectTransform)_sessionEndPanel.transform);

        GameObject dialog = CreateImageObject("Dialog", _sessionEndPanel.transform, new Color(0.08f, 0.1f, 0.14f, 0.98f));
        RectTransform dialogRect = (RectTransform)dialog.transform;
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.sizeDelta = new Vector2(560f, 280f);

        Text title = CreateText("Title", dialog.transform, "탐사 세션 종료", 40, TextAnchor.MiddleCenter);
        RectTransform titleRect = (RectTransform)title.transform;
        titleRect.anchorMin = new Vector2(0.1f, 0.58f);
        titleRect.anchorMax = new Vector2(0.9f, 0.9f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        _returnButton = CreateButton("Return Button", dialog.transform, "우주선으로 복귀");
        SetButtonRect(_returnButton, new Vector2(0.08f, 0.12f), new Vector2(0.48f, 0.4f));

        _retryButton = CreateButton("Retry Button", dialog.transform, "재탐사");
        SetButtonRect(_retryButton, new Vector2(0.52f, 0.12f), new Vector2(0.92f, 0.4f));
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new("Session End Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static GameObject CreateImageObject(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        imageObject.GetComponent<Image>().color = color;
        return imageObject;
    }

    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        GameObject buttonObject = CreateImageObject(objectName, parent, new Color(0.16f, 0.2f, 0.27f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        Text text = CreateText("Label", buttonObject.transform, label, 26, TextAnchor.MiddleCenter);
        StretchToParent((RectTransform)text.transform);
        return button;
    }

    private static Text CreateText(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    private static void SetButtonRect(Button button, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
