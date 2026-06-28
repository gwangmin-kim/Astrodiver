using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerAttackController : MonoBehaviour
{
    [Header("Required Components")]
    [SerializeField] private PlayerInputHandler _inputHandler;
    [SerializeField] private PlasmaGunController _plasmaGun;

    private void Awake()
    {
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
        if (_plasmaGun == null) _plasmaGun = GetComponentInChildren<PlasmaGunController>();
    }
    private void OnEnable()
    {
        _inputHandler.pressAttackEvent += OnPressAttack;
        _inputHandler.releaseAttackEvent += OnReleaseAttack;
    }

    private void OnDisable()
    {
        _inputHandler.pressAttackEvent -= OnPressAttack;
        _inputHandler.releaseAttackEvent -= OnReleaseAttack;
    }

    private void OnPressAttack()
    {
        _plasmaGun.isAttacking = true;
    }

    private void OnReleaseAttack()
    {
        _plasmaGun.isAttacking = false;
    }
}
