using System;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum UpgradeNodeVisualState
{
    Locked,
    Unlocked,
    Completed
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
    [SerializeField] private UpgradeNodeStateStyle _completedStyle;

    public UpgradeNodeDefinition Definition => _definition;
    public UpgradeNodeVisualState VisualState => _visualState;

    public event Action<UpgradeNodeUI> Clicked;
    public event Action<UpgradeNodeUI> PointerEntered;
    public event Action<UpgradeNodeUI> PointerExited;

    private void OnValidate()
    {
        _iconImage.sprite = _definition != null ? _definition.Icon : null;
        _iconImage.enabled = _iconImage.sprite != null;

        if (_definition != null)
        {
            name = _definition.DisplayName;
        }
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
        UpgradeNodeStateStyle style = state switch
        {
            UpgradeNodeVisualState.Locked => _lockedStyle,
            UpgradeNodeVisualState.Completed => _completedStyle,
            _ => _unlockedStyle
        };

        _backgroundImage.color = style.BackgroundColor;
        _iconImage.color = style.IconColor;
        _visualRoot.localScale = Vector3.one * style.Scale;
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
