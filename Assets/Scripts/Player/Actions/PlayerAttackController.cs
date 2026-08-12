using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAttackController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private NetGunController _netGun;
    [SerializeField] private PlasmaGunController _plasmaGun;

    private enum HandEquipment
    {
        NetGun,
        PlasmaGun
    }
    private HandEquipment _currentEquipment = HandEquipment.PlasmaGun;

    private void Awake()
    {
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
        if (_netGun == null) _netGun = GetComponentInChildren<NetGunController>();
        if (_plasmaGun == null) _plasmaGun = GetComponentInChildren<PlasmaGunController>();
    }

    private void Start()
    {
        _currentEquipment = HandEquipment.PlasmaGun;
        SetEquipmentEquipped(_netGun, false);
        SetEquipmentEquipped(_plasmaGun, true);
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

    private bool SwitchEquipment(HandEquipment equipment)
    {
        if (_currentEquipment == equipment) return true;

        switch (equipment)
        {
            case HandEquipment.NetGun:
                if (!_netGun.IsUnlocked || !_plasmaGun.IsSwitchable) return false;

                SetEquipmentEquipped(_netGun, true);
                SetEquipmentEquipped(_plasmaGun, false);
                break;

            case HandEquipment.PlasmaGun:
                if (!_netGun.IsSwitchable) return false;

                SetEquipmentEquipped(_netGun, false);
                SetEquipmentEquipped(_plasmaGun, true);
                break;
        }

        _currentEquipment = equipment;
        return true;
    }

    private void OnPressCapture()
    {
        if (!_netGun.IsUnlocked) return;
        if (!SwitchEquipment(HandEquipment.NetGun)) return;
        _netGun.OnPressCapture();
    }

    private void OnReleaseCapture()
    {
        if (_currentEquipment != HandEquipment.NetGun) return;
        _netGun.OnReleaseCapture();
    }

    private void OnPressAttack()
    {
        if (!_plasmaGun.HasAmmo) return;
        if (!SwitchEquipment(HandEquipment.PlasmaGun)) return;
        _plasmaGun.isAttacking = true;
    }

    private void OnReleaseAttack()
    {
        if (_currentEquipment != HandEquipment.PlasmaGun) return;
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
