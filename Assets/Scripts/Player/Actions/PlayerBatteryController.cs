using System;
using UnityEngine;

public class PlayerBatteryController : MonoBehaviour
{
    [SerializeField] private BatteryData _batteryData;

    private float _currentBatteryAmount;
    private bool _isDepleted;

    public float CurrentBatteryAmount => _currentBatteryAmount;
    public float MaxBatteryAmount => Mathf.Max(0f, _batteryData.amount);

    public event Action<float, float> BatteryAmountChanged;

    private void Awake()
    {
        _currentBatteryAmount = MaxBatteryAmount;
    }

    private void Start()
    {
        PublishBatteryAmountChanged();
    }

    private void Update()
    {
        if (_isDepleted || SessionManager.Instance == null || SessionManager.Instance.IsSessionFinished)
        {
            return;
        }

        SetCurrentBatteryAmount(_currentBatteryAmount - Time.deltaTime);
        if (_currentBatteryAmount > 0f)
        {
            return;
        }

        _isDepleted = true;
        SessionManager.Instance.FinishSessionByTimeout();
    }

    private void SetCurrentBatteryAmount(float amount)
    {
        float nextAmount = Mathf.Clamp(amount, 0f, MaxBatteryAmount);
        if (_currentBatteryAmount == nextAmount)
        {
            return;
        }

        _currentBatteryAmount = nextAmount;
        PublishBatteryAmountChanged();
    }

    private void PublishBatteryAmountChanged()
    {
        BatteryAmountChanged?.Invoke(_currentBatteryAmount, MaxBatteryAmount);
    }
}

[System.Serializable]
public struct BatteryData
{
    [Min(0f)] public float amount;
}
