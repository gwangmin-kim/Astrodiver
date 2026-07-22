using UnityEngine;

[DisallowMultipleComponent]
public sealed class InventoryHudUI : MonoBehaviour
{
    [SerializeField] private PlayerInventoryController _playerInventory;
    [SerializeField] private CreatureInventoryBarUI _creatureInventoryBar;
    [SerializeField] private ResourceFragmentListUI _resourceFragmentList;

    private bool _hasStarted;

    private void OnEnable()
    {
        EnsureLayout();

        if (_hasStarted)
        {
            InitializeInventoryViews();
        }
    }

    private void Start()
    {
        _hasStarted = true;
        InitializeInventoryViews();
    }

    private void EnsureLayout()
    {
        EnsureRectTransform(gameObject);

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

        if (_resourceFragmentList != null)
        {
            _resourceFragmentList.Initialize(inventory);
        }
    }

    private PlayerInventoryController ResolvePlayerInventory()
    {
        if (_playerInventory != null && _playerInventory == PlayerInventoryController.Instance)
            return _playerInventory;

        _playerInventory = PlayerInventoryController.Instance;
        if (_playerInventory == null)
        {
            Debug.LogWarning("CreatureInventoryBarUI: PlayerInventoryController is not initialized.", this);
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
