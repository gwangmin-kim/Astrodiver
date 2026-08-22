using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class LowBatteryScreenEffectUI : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] private float _threshold = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float _maxOpacity = 0.35f;
    [SerializeField] private Color _overlayColor = new Color(1f, 0.05f, 0.05f, 1f);
    [SerializeField] private PlayerBatteryController _playerBattery;
    [SerializeField] private Image _overlayImage;

    private PlayerBatteryController _subscribedBattery;

    private void Awake()
    {
        if (_overlayImage == null)
        {
            _overlayImage = GetComponent<Image>();
        }

        _overlayImage.raycastTarget = false;
    }

    private void OnEnable()
    {
        Bind(ResolvePlayerBattery());
    }

    private void Start()
    {
        Bind(ResolvePlayerBattery());
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Bind(PlayerBatteryController playerBattery)
    {
        _playerBattery = playerBattery;

        if (_subscribedBattery != playerBattery)
        {
            Unsubscribe();

            if (isActiveAndEnabled && playerBattery != null)
            {
                playerBattery.BatteryAmountChanged += HandleBatteryAmountChanged;
                _subscribedBattery = playerBattery;
            }
        }

        if (playerBattery != null)
        {
            HandleBatteryAmountChanged(playerBattery.CurrentBatteryAmount, playerBattery.MaxBatteryAmount);
        }
        else
        {
            SetOpacity(0f);
        }
    }

    private void Unsubscribe()
    {
        if (_subscribedBattery == null)
        {
            return;
        }

        _subscribedBattery.BatteryAmountChanged -= HandleBatteryAmountChanged;
        _subscribedBattery = null;
    }

    private PlayerBatteryController ResolvePlayerBattery()
    {
        if (_playerBattery == null && PlayerContext.Instance != null)
        {
            _playerBattery = PlayerContext.Instance.Battery;
        }

        return _playerBattery;
    }

    private void HandleBatteryAmountChanged(float currentAmount, float maxAmount)
    {
        float batteryRatio = maxAmount > 0f ? Mathf.Clamp01(currentAmount / maxAmount) : 0f;
        float effectRatio = _threshold > 0f
            ? Mathf.Clamp01((_threshold - batteryRatio) / _threshold)
            : 0f;

        SetOpacity(effectRatio * _maxOpacity);
    }

    private void SetOpacity(float opacity)
    {
        if (_overlayImage == null)
        {
            return;
        }

        Color color = _overlayColor;
        color.a = Mathf.Clamp01(opacity);
        _overlayImage.color = color;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_overlayImage == null)
        {
            _overlayImage = GetComponent<Image>();
        }

        if (_overlayImage != null)
        {
            _overlayImage.raycastTarget = false;
            SetOpacity(0f);
        }
    }
#endif
}
