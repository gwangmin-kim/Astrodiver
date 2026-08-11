using System;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum UpgradeNodeVisualState
{
    Locked, // 부모가 해금되지 않아 잠긴 상태
    Unlocked, // 부모가 해금되어 접근 가능하나, 아직 0단계인 상태
    Purchased, // 1레벨 이상 구매했으나, 아직 최대 레벨에 도달하지 않은 상태
    Completed // 최대 레벨까지 업그레이드 완료된 상태
}

[Serializable]
public struct UpgradeNodeStateStyle
{
    private const float MinScale = 0.5f;
    private const float MaxScale = 1.2f;

    [SerializeField] private Color _backgroundColor;
    [SerializeField] private Color _iconColor;
    [SerializeField, Range(MinScale, MaxScale)] private float _scale;

    public UpgradeNodeStateStyle(Color backgroundColor, Color iconColor, float scale)
    {
        _backgroundColor = backgroundColor;
        _iconColor = iconColor;
        _scale = scale;
    }

    public readonly Color BackgroundColor => _backgroundColor;
    public readonly Color IconColor => _iconColor;
    public readonly float Scale => Mathf.Clamp(_scale, MinScale, MaxScale);
}

[DisallowMultipleComponent]
public sealed class UpgradeNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Upgrade")]
    [SerializeField] private UpgradeNodeDefinition _definition;
    [SerializeField] private UpgradeNodeVisualState _visualState;

    [Header("UI References")]
    [SerializeField] private RectTransform _visualRoot;
    [SerializeField] private Button _button;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _levelText;

    [Header("State Styles")]
    [SerializeField] private UpgradeNodeStateStyle _lockedStyle;
    [SerializeField] private UpgradeNodeStateStyle _unlockedStyle;
    [SerializeField] private UpgradeNodeStateStyle _purchasedStyle;
    [SerializeField] private UpgradeNodeStateStyle _completedStyle;

    public UpgradeNodeDefinition Definition => _definition;
    public UpgradeNodeVisualState VisualState => _visualState;

    public event Action<UpgradeNodeUI> Clicked;
    public event Action<UpgradeNodeUI> PointerEntered;
    public event Action<UpgradeNodeUI> PointerExited;

    private void OnValidate()
    {
        if (_iconImage != null)
        {
            _iconImage.sprite = _definition != null ? _definition.Icon : null;
            _iconImage.enabled = _iconImage.sprite != null;
        }

        if (_definition != null)
        {
            name = _definition.Id;
        }

        ApplyVisualState(_visualState);
    }

    private void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(HandleClick);
        }
    }

    public void SetLevel(int currentLevel)
    {
        int maxLevel = _definition != null ? _definition.MaxLevel : 1;
        int clampedLevel = Mathf.Clamp(currentLevel, 0, maxLevel);
        _levelText.text = $"{clampedLevel}/{maxLevel}";
    }

    public void SetVisualState(UpgradeNodeVisualState state)
    {
        _visualState = state;
        ApplyVisualState(state);
    }

    private void ApplyVisualState(UpgradeNodeVisualState state)
    {
        UpgradeNodeStateStyle style = state switch
        {
            UpgradeNodeVisualState.Locked => _lockedStyle,
            UpgradeNodeVisualState.Unlocked => _unlockedStyle,
            UpgradeNodeVisualState.Purchased => _purchasedStyle,
            UpgradeNodeVisualState.Completed => _completedStyle,
            _ => _lockedStyle
        };

        if (_backgroundImage != null)
        {
            _backgroundImage.color = style.BackgroundColor;
        }

        if (_iconImage != null)
        {
            _iconImage.color = style.IconColor;
        }

        if (_visualRoot != null)
        {
            _visualRoot.localScale = Vector3.one * style.Scale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExited?.Invoke(this);
    }

    private void HandleClick()
    {
        Clicked?.Invoke(this);
    }
}
