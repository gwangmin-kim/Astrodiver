using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SessionAmmoSupplementHUD : MonoBehaviour
{
    [System.Serializable]
    private sealed class NetAmmoStack
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _iconRoot;
        [SerializeField] private GameObject _iconTemplate;
        [SerializeField, Min(0f)] private float _iconSpacing = 4f;

        private readonly List<GameObject> _icons = new();

        public void SetVisible(bool isVisible)
        {
            if (_root != null) _root.SetActive(isVisible);
        }

        public void SetAmmo(int currentAmmo, int totalAmmo)
        {
            int visibleCount = Mathf.Clamp(currentAmmo, 0, Mathf.Max(0, totalAmmo));
            while (_icons.Count < visibleCount)
            {
                GameObject icon = Instantiate(_iconTemplate, _iconRoot);
                icon.gameObject.SetActive(true);
                _icons.Add(icon);
            }

            for (int index = 0; index < _icons.Count; index++)
            {
                GameObject icon = _icons[index];
                if (icon == null) continue;

                icon.SetActive(index < visibleCount);
            }
        }
    }

    [System.Serializable]
    private sealed class PlasmaAmmoBar
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Slider _slider;

        public void SetVisible(bool isVisible)
        {
            if (_root != null) _root.SetActive(isVisible);
        }

        public void SetAmmo(int currentAmmo, int totalAmmo)
        {
            float normalized = totalAmmo > 0
                ? Mathf.Clamp01((float)currentAmmo / totalAmmo)
                : 0f;
            if (_slider != null) _slider.SetValueWithoutNotify(normalized);
        }
    }

    [SerializeField] private NetAmmoStack _netAmmoStack;
    [SerializeField] private PlasmaAmmoBar _plasmaAmmoBar;

    private PlayerAttackController _attackController;
    private NetGunController _netGun;
    private PlasmaGunController _plasmaGun;

    private void OnEnable() => Bind(ResolveAttackController());

    private void Start() => Bind(ResolveAttackController());

    private void OnDisable() => Unbind();

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
            Refresh();
            return;
        }

        Unbind();
        _attackController = attackController;
        if (_attackController == null)
        {
            _netAmmoStack?.SetVisible(false);
            _plasmaAmmoBar?.SetVisible(false);
            return;
        }

        _netGun = _attackController.NetGun;
        _plasmaGun = _attackController.PlasmaGun;
        _attackController.EquipmentSelected += HandleEquipmentSelected;
        if (_netGun != null) _netGun.AmmoChanged += HandleNetAmmoChanged;
        if (_plasmaGun != null) _plasmaGun.AmmoChanged += HandlePlasmaAmmoChanged;
        Refresh();
    }

    private void Unbind()
    {
        if (_attackController != null) _attackController.EquipmentSelected -= HandleEquipmentSelected;
        if (_netGun != null) _netGun.AmmoChanged -= HandleNetAmmoChanged;
        if (_plasmaGun != null) _plasmaGun.AmmoChanged -= HandlePlasmaAmmoChanged;
        _attackController = null;
        _netGun = null;
        _plasmaGun = null;
    }

    private void Refresh()
    {
        if (_netGun != null) HandleNetAmmoChanged(_netGun.RemainingAmmo, _netGun.TotalAmmo);
        if (_plasmaGun != null) HandlePlasmaAmmoChanged(_plasmaGun.RemainingAmmo, _plasmaGun.TotalAmmo);
        if (_attackController != null) HandleEquipmentSelected(_attackController.CurrentEquipment);
    }

    private void HandleEquipmentSelected(PlayerEquipmentType selectedEquipment)
    {
        _netAmmoStack?.SetVisible(
            selectedEquipment == PlayerEquipmentType.NetGun &&
            _netGun != null &&
            _netGun.IsUnlocked);
        _plasmaAmmoBar?.SetVisible(selectedEquipment == PlayerEquipmentType.PlasmaGun);
    }

    private void HandleNetAmmoChanged(int currentAmmo, int totalAmmo)
    {
        _netAmmoStack?.SetAmmo(currentAmmo, totalAmmo);
    }

    private void HandlePlasmaAmmoChanged(int currentAmmo, int totalAmmo)
    {
        _plasmaAmmoBar?.SetAmmo(currentAmmo, totalAmmo);
    }
}
