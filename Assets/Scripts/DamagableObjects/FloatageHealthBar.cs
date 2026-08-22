using UnityEngine;

[DisallowMultipleComponent]
public sealed class FloatageHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _followBar;
    [SerializeField] private Transform _currentHpBar;

    [Header("Placement")]
    [SerializeField] private float _verticalOffset = 1.2f;

    [Header("Follow Bar")]
    [SerializeField, Min(0f)] private float _followDelay = 0.15f;
    [SerializeField, Min(0.01f)] private float _followDecreaseSpeed = 2.5f;

    private FloatageController _floatage;
    private Vector3 _followBaseScale;
    private Vector3 _currentHpBaseScale;
    private float _currentValue = 1f;
    private float _followValue = 1f;
    private float _followDelayRemaining;

    private void Awake()
    {
        _floatage = GetComponentInParent<FloatageController>();
        _followBaseScale = _followBar != null ? _followBar.localScale : Vector3.one;
        _currentHpBaseScale = _currentHpBar != null ? _currentHpBar.localScale : Vector3.one;
    }

    private void OnEnable()
    {
        if (_floatage == null)
        {
            return;
        }

        _floatage.HealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (_floatage == null)
        {
            return;
        }

        _floatage.HealthChanged -= HandleHealthChanged;
    }

    private void Start()
    {
        if (_floatage != null)
        {
            SetValuesImmediately(_floatage.HealthNormalized);
        }
    }

    private void LateUpdate()
    {
        if (_floatage == null)
        {
            return;
        }

        transform.SetPositionAndRotation(
            _floatage.transform.position + Vector3.up * _verticalOffset,
            Quaternion.identity);

        if (_followValue <= _currentValue)
        {
            return;
        }

        if (_followDelayRemaining > 0f)
        {
            _followDelayRemaining -= Time.deltaTime;
            return;
        }

        _followValue = Mathf.MoveTowards(
            _followValue,
            _currentValue,
            _followDecreaseSpeed * Time.deltaTime);
        SetBarScale(_followBar, _followBaseScale, _followValue);
    }

    private void HandleHealthChanged(int currentHp, int maxHp)
    {
        float value = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;

        if (value >= _currentValue)
        {
            SetValuesImmediately(value);
            return;
        }

        _currentValue = value;
        SetBarScale(_currentHpBar, _currentHpBaseScale, _currentValue);
        _followDelayRemaining = _followDelay;
    }

    private void SetValuesImmediately(float value)
    {
        _currentValue = Mathf.Clamp01(value);
        _followValue = _currentValue;
        _followDelayRemaining = 0f;
        SetBarScale(_currentHpBar, _currentHpBaseScale, _currentValue);
        SetBarScale(_followBar, _followBaseScale, _followValue);
    }

    private static void SetBarScale(Transform bar, Vector3 baseScale, float value)
    {
        if (bar == null)
        {
            return;
        }

        bar.localScale = new Vector3(baseScale.x * Mathf.Clamp01(value), baseScale.y, baseScale.z);
    }
}
