using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BatteryBarUI : MonoBehaviour
{
    [SerializeField] private PlayerBatteryController _playerBattery;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Slider _slider;

    private PlayerBatteryController _subscribedBattery;

    private void Awake()
    {
        ConfigureSlider();
    }

    private void OnEnable()
    {
        Bind(ResolvePlayerBattery());
    }

    private void Start()
    {
        PlayerBatteryController battery = ResolvePlayerBattery();
        Bind(battery);

        if (battery == null)
        {
            Debug.LogWarning("BatteryBarUI: PlayerBatteryController is not found.", this);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Initialize(PlayerBatteryController playerBattery)
    {
        Bind(playerBattery);
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
            SetNormalizedValue(0f);
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
        if (_playerBattery == null)
        {
            _playerBattery = PlayerContext.Instance.Battery;
        }

        return _playerBattery;
    }

    private void HandleBatteryAmountChanged(float currentAmount, float maxAmount)
    {
        float normalizedAmount = maxAmount > 0f
            ? Mathf.Clamp01(currentAmount / maxAmount)
            : 0f;

        SetNormalizedValue(normalizedAmount);
    }

    private void ConfigureSlider()
    {
        if (_slider == null)
        {
            _slider = GetComponent<Slider>();
        }

        if (_slider == null)
        {
            return;
        }

        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.wholeNumbers = false;
        _slider.interactable = false;
    }

    private void SetNormalizedValue(float value)
    {
        ConfigureSlider();
        if (_slider == null)
        {
            Debug.LogWarning("BatteryBarUI: slider not found.", this);
        }
        else _slider.SetValueWithoutNotify(Mathf.Clamp01(value));

        if (_text == null)
        {
            Debug.LogWarning("BatteryBarUI: text not found.", this);
        }
        else _text.text = $"{value * 100f:00.0}%";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ConfigureSlider();
    }
#endif
}
