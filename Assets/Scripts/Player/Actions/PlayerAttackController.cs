using UnityEngine;

public enum PlayerEquipmentType
{
    NetGun,
    PlasmaGun
}

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAttackController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private NetGunController _netGun;
    [SerializeField] private PlasmaGunController _plasmaGun;

    private PlayerEquipmentType _currentEquipment = PlayerEquipmentType.PlasmaGun;

    public PlayerEquipmentType CurrentEquipment => _currentEquipment;
    public NetGunController NetGun => _netGun;
    public PlasmaGunController PlasmaGun => _plasmaGun;
    public event System.Action<PlayerEquipmentType> EquipmentSelected;

    private void Awake()
    {
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
        if (_netGun == null) _netGun = GetComponentInChildren<NetGunController>();
        if (_plasmaGun == null) _plasmaGun = GetComponentInChildren<PlasmaGunController>();
    }

    private void Start()
    {
        _currentEquipment = PlayerEquipmentType.PlasmaGun;
        SetEquipmentEquipped(_netGun, false);
        SetEquipmentEquipped(_plasmaGun, true);
        EquipmentSelected?.Invoke(_currentEquipment);
    }

    private void OnEnable()
    {
        _inputHandler.pressCaptureEvent += OnPressCapture;
        _inputHandler.releaseCaptureEvent += OnReleaseCapture;

        _inputHandler.pressAttackEvent += OnPressAttack;
        _inputHandler.releaseAttackEvent += OnReleaseAttack;
    }

    private void OnDisable()
    {
        _inputHandler.pressCaptureEvent -= OnPressCapture;
        _inputHandler.releaseCaptureEvent -= OnReleaseCapture;

        _inputHandler.pressAttackEvent -= OnPressAttack;
        _inputHandler.releaseAttackEvent -= OnReleaseAttack;
    }

    private bool SwitchEquipment(PlayerEquipmentType equipment)
    {
        if (_currentEquipment == equipment) return true;

        switch (equipment)
        {
            case PlayerEquipmentType.NetGun:
                if (!_netGun.IsUnlocked || !_plasmaGun.IsSwitchable) return false;

                SetEquipmentEquipped(_netGun, true);
                SetEquipmentEquipped(_plasmaGun, false);
                break;

            case PlayerEquipmentType.PlasmaGun:
                if (!_netGun.IsSwitchable) return false;

                SetEquipmentEquipped(_netGun, false);
                SetEquipmentEquipped(_plasmaGun, true);
                break;
        }

        _currentEquipment = equipment;
        EquipmentSelected?.Invoke(_currentEquipment);
        return true;
    }

    private void OnPressCapture()
    {
        if (!_netGun.IsUnlocked) return;
        if (!SwitchEquipment(PlayerEquipmentType.NetGun)) return;
        _netGun.OnPressCapture();
    }

    private void OnReleaseCapture()
    {
        if (_currentEquipment != PlayerEquipmentType.NetGun) return;
        _netGun.OnReleaseCapture();
    }

    private void OnPressAttack()
    {
        if (!_plasmaGun.HasAmmo) return;
        if (!SwitchEquipment(PlayerEquipmentType.PlasmaGun)) return;
        _plasmaGun.isAttacking = true;
    }

    private void OnReleaseAttack()
    {
        if (_currentEquipment != PlayerEquipmentType.PlasmaGun) return;
        _plasmaGun.isAttacking = false;
    }

    private static void SetEquipmentEquipped(MonoBehaviour equipment, bool isEquipped)
    {
        // Controllers must remain active so world objects they own (such as launched nets)
        // can continue their lifecycle while another hand equipment is selected.
        equipment.gameObject.SetActive(true);

        // TODO: Sprite 루트 오브젝트를 할당해주고, 해당 오브젝트를 켜고 끄는 방식으로 변경 (런타임에 컴포넌트를 찾을 필요 없도록)
        Renderer[] renderers = equipment.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = isEquipped;
        }
    }
}
