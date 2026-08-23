using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class IrisSceneTransition : MonoBehaviour
{
    private const string ShaderName = "Astrodiver/UI/IrisTransition";

    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float _closeDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float _openDuration = 0.45f;

    [Header("Appearance")]
    [SerializeField] private Color _outsideColor = Color.black;
    [SerializeField, Range(0f, 0.1f)] private float _edgeSoftness = 0.008f;

    private GameObject _canvasRoot;
    private RawImage _maskImage;
    private Material _material;

    private void Awake()
    {
        CreateOverlay();
        HideImmediately();
    }

    private void OnDestroy()
    {
        if (_material != null)
        {
            Destroy(_material);
        }
    }

    public IEnumerator Close()
    {
        CreateOverlay();
        if (_canvasRoot == null)
        {
            yield break;
        }

        _canvasRoot.SetActive(true);
        yield return AnimateRadius(MaxRadius, 0f, _closeDuration);
    }

    public IEnumerator Open()
    {
        CreateOverlay();
        if (_canvasRoot == null)
        {
            yield break;
        }

        _canvasRoot.SetActive(true);
        yield return AnimateRadius(0f, MaxRadius, _openDuration);
        HideImmediately();
    }

    public bool IsAvailable
    {
        get
        {
            CreateOverlay();
            return _canvasRoot != null;
        }
    }

    public void HideImmediately()
    {
        if (_canvasRoot == null)
        {
            return;
        }

        SetRadius(MaxRadius);
        _canvasRoot.SetActive(false);
    }

    private void CreateOverlay()
    {
        if (_canvasRoot != null)
        {
            return;
        }

        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"Iris transition shader was not found: {ShaderName}", this);
            enabled = false;
            return;
        }

        _canvasRoot = new GameObject(
            "IrisTransitionCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        _canvasRoot.transform.SetParent(transform, false);

        Canvas canvas = _canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler canvasScaler = _canvasRoot.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        GameObject imageObject = new GameObject("Mask", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(_canvasRoot.transform, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        _material = new Material(shader)
        {
            name = "Iris Transition Material",
            hideFlags = HideFlags.DontSave
        };

        _maskImage = imageObject.GetComponent<RawImage>();
        _maskImage.texture = Texture2D.whiteTexture;
        _maskImage.material = _material;
        _maskImage.raycastTarget = true;

        SetRadius(MaxRadius);
    }

    private IEnumerator AnimateRadius(float startRadius, float endRadius, float duration)
    {
        float elapsed = 0f;
        SetRadius(startRadius);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            SetRadius(Mathf.Lerp(startRadius, endRadius, easedProgress));
            yield return null;
        }

        SetRadius(endRadius);
    }

    private float MaxRadius
    {
        get
        {
            float aspectRatio = Mathf.Max(1f, Screen.width / (float)Mathf.Max(1, Screen.height));
            return Mathf.Sqrt((aspectRatio * aspectRatio + 1f) * 0.25f) + _edgeSoftness;
        }
    }

    private void SetRadius(float radius)
    {
        if (_material == null)
        {
            return;
        }

        _material.SetFloat("_Radius", radius);
        _material.SetFloat("_EdgeSoftness", _edgeSoftness);
        _material.SetColor("_OutsideColor", _outsideColor);
    }
}
