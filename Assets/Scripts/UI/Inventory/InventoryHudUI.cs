using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class InventoryHudUI : MonoBehaviour
{
    [SerializeField] private PlayerInventoryController _playerInventory;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private CanvasScaler _canvasScaler;
    [SerializeField] private GraphicRaycaster _graphicRaycaster;
    [SerializeField] private CreatureInventoryBarUI _creatureInventoryBar;
    [SerializeField] private ResourceFragmentListUI _resourceFragmentList;
    [SerializeField] private Vector2 _referenceResolution = new(1920f, 1080f);
    [SerializeField] private int _sortingOrder = 10;

    private void OnEnable()
    {
        EnsureLayout();
        InitializeInventoryViews();
    }

    private void Start()
    {
        InitializeInventoryViews();
    }

    private void EnsureLayout()
    {
        EnsureRectTransform(gameObject);

        _canvas = EnsureComponent<Canvas>(gameObject);
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = _sortingOrder;

        _canvasScaler = EnsureComponent<CanvasScaler>(gameObject);
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.referenceResolution = _referenceResolution;
        _canvasScaler.matchWidthOrHeight = 0.5f;

        _graphicRaycaster = EnsureComponent<GraphicRaycaster>(gameObject);
        _graphicRaycaster.enabled = true;

        _creatureInventoryBar = EnsureChildComponent(_creatureInventoryBar, "Creature Inventory Bar");
        _resourceFragmentList = EnsureChildComponent(_resourceFragmentList, "Resource Fragment List");
    }

    private void InitializeInventoryViews()
    {
        PlayerInventoryController inventory = ResolvePlayerInventory();
        if (_creatureInventoryBar != null)
        {
            _creatureInventoryBar.Initialize(inventory);
        }
    }

    private PlayerInventoryController ResolvePlayerInventory()
    {
        if (_playerInventory != null) return _playerInventory;

        if (PlayerContext.Instance != null)
        {
            _playerInventory = PlayerContext.Instance.Inventory;
        }

        if (_playerInventory == null)
        {
            _playerInventory = FindAnyObjectByType<PlayerInventoryController>();
        }

        return _playerInventory;
    }

    private T EnsureChildComponent<T>(T current, string childName) where T : Component
    {
        if (current != null) return current;

        Transform child = transform.Find(childName);
        GameObject childObject = child != null ? child.gameObject : new GameObject(childName, typeof(RectTransform));
        childObject.transform.SetParent(transform, false);
        return EnsureComponent<T>(childObject);
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        return EnsureComponent<RectTransform>(target);
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
