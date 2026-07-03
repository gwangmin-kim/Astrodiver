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
    private HandEquipment _currentEquipment = HandEquipment.NetGun;

    private void Awake()
    {
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
        if (_netGun == null) _netGun = GetComponentInChildren<NetGunController>();
        if (_plasmaGun == null) _plasmaGun = GetComponentInChildren<PlasmaGunController>();
    }

    private void Start()
    {
        _currentEquipment = HandEquipment.NetGun;
        _netGun.gameObject.SetActive(true);
        _plasmaGun.gameObject.SetActive(false);
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
                if (!_plasmaGun.IsSwitchable) return false;

                _netGun.gameObject.SetActive(true);
                _plasmaGun.gameObject.SetActive(false);
                break;

            case HandEquipment.PlasmaGun:
                if (!_netGun.IsSwitchable) return false;

                _netGun.gameObject.SetActive(false);
                _plasmaGun.gameObject.SetActive(true);
                break;
        }

        _currentEquipment = equipment;
        return true;
    }

    private void OnPressCapture()
    {
        if (!SwitchEquipment(HandEquipment.NetGun)) return;
        _netGun.OnPressCapture();
    }

    private void OnReleaseCapture()
    {
        if (!_netGun.gameObject.activeSelf) return;
        _netGun.OnReleaseCapture();
    }

    private void OnPressAttack()
    {
        if (!SwitchEquipment(HandEquipment.PlasmaGun)) return;
        _plasmaGun.isAttacking = true;
    }

    private void OnReleaseAttack()
    {
        if (!_plasmaGun.gameObject.activeSelf) return;
        _plasmaGun.isAttacking = false;
    }
}
