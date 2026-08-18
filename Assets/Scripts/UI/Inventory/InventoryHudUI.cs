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

}
