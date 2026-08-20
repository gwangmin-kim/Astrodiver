using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SessionEquipmentAmmoHUD : MonoBehaviour
{
    [System.Serializable]
    private sealed class EquipmentSlot
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _ammoText;

        private Color _backgroundColor;
        private Color _iconColor;
        private bool _colorsCached;

        public void SetVisible(bool isVisible)
        {
            if (_root != null) _root.SetActive(isVisible);
        }

        public void SetAmmo(int currentAmmo, int totalAmmo)
        {
            if (_ammoText != null) _ammoText.text = $"{currentAmmo} / {totalAmmo}";
        }

        public void SetSelected(bool isSelected, Color inactiveTint)
        {
            CacheColors();
            Color tint = isSelected ? Color.white : inactiveTint;
            if (_background != null) _background.color = Multiply(_backgroundColor, tint);
            if (_icon != null) _icon.color = Multiply(_iconColor, tint);
        }

        private void CacheColors()
        {
            if (_colorsCached) return;
            _backgroundColor = _background != null ? _background.color : Color.white;
            _iconColor = _icon != null ? _icon.color : Color.white;
            _colorsCached = true;
        }

        private static Color Multiply(Color color, Color multiplier)
        {
            return new Color(
                color.r * multiplier.r,
                color.g * multiplier.g,
                color.b * multiplier.b,
                color.a * multiplier.a);
        }
    }

    [Header("Slot References")]
    [SerializeField] private EquipmentSlot _netGunSlot;
    [SerializeField] private EquipmentSlot _plasmaGunSlot;
    [SerializeField] private Color _inactiveTint = new(0.4f, 0.4f, 0.4f, 0.8f);

    private PlayerAttackController _attackController;
    private NetGunController _netGun;
    private PlasmaGunController _plasmaGun;

    private void OnEnable()
    {
        Bind(ResolveAttackController());
    }

    private void Start()
    {
        Bind(ResolveAttackController());
    }

    private void OnDisable()
    {
        Unbind();
    }

    private PlayerAttackController ResolveAttackController()
    {
        return PlayerContext.Instance != null
            ? PlayerContext.Instance.GetComponent<PlayerAttackController>()
            : null;
    }

    private void Bind(PlayerAttackController attackController)
    {
        if (_attackController == attackController)
        {
            RefreshAll();
            return;
        }

        Unbind();
        _attackController = attackController;
        if (_attackController == null) return;

        _netGun = _attackController.NetGun;
        _plasmaGun = _attackController.PlasmaGun;
        _attackController.EquipmentSelected += HandleEquipmentSelected;
        if (_netGun != null) _netGun.AmmoChanged += HandleNetGunAmmoChanged;
        if (_plasmaGun != null) _plasmaGun.AmmoChanged += HandlePlasmaGunAmmoChanged;
        RefreshAll();
    }

    private void Unbind()
    {
        if (_attackController != null) _attackController.EquipmentSelected -= HandleEquipmentSelected;
        if (_netGun != null) _netGun.AmmoChanged -= HandleNetGunAmmoChanged;
        if (_plasmaGun != null) _plasmaGun.AmmoChanged -= HandlePlasmaGunAmmoChanged;
        _attackController = null;
        _netGun = null;
        _plasmaGun = null;
    }

    private void RefreshAll()
    {
        bool isNetGunUnlocked = _netGun != null && _netGun.IsUnlocked;
        _netGunSlot?.SetVisible(isNetGunUnlocked);
        if (_netGun != null) _netGunSlot?.SetAmmo(_netGun.RemainingAmmo, _netGun.TotalAmmo);
        if (_plasmaGun != null) _plasmaGunSlot?.SetAmmo(_plasmaGun.RemainingAmmo, _plasmaGun.TotalAmmo);
        if (_attackController != null) HandleEquipmentSelected(_attackController.CurrentEquipment);
    }

    private void HandleNetGunAmmoChanged(int currentAmmo, int totalAmmo)
    {
        _netGunSlot?.SetAmmo(currentAmmo, totalAmmo);
    }

    private void HandlePlasmaGunAmmoChanged(int currentAmmo, int totalAmmo)
    {
        _plasmaGunSlot?.SetAmmo(currentAmmo, totalAmmo);
    }

    private void HandleEquipmentSelected(PlayerEquipmentType selectedEquipment)
    {
        _netGunSlot?.SetSelected(selectedEquipment == PlayerEquipmentType.NetGun, _inactiveTint);
        _plasmaGunSlot?.SetSelected(selectedEquipment == PlayerEquipmentType.PlasmaGun, _inactiveTint);
    }
}
