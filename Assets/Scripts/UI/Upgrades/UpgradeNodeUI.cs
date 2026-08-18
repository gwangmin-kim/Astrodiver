using System;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum UpgradeNodeVisualState
{
    Locked, // 부모가 해금되지 않아 잠긴 상태
    Purchasable, // 다음 레벨을 구매할 수 있는 상태
    Unavailable, // 해금됐지만 다음 레벨을 구매할 수 없는 상태
    Completed // 최대 레벨까지 업그레이드 완료된 상태
}

[Serializable]
public struct UpgradeNodeStateStyle
{
    [SerializeField] private Color _outlineColor;

    public UpgradeNodeStateStyle(Color outlineColor)
    {
        _outlineColor = outlineColor;
    }

    public readonly Color OutlineColor => _outlineColor;
}

[DisallowMultipleComponent]
public sealed class UpgradeNodeUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("Upgrade")]
    [SerializeField] private UpgradeNodeDefinition _definition;
    [SerializeField] private UpgradeNodeVisualState _visualState;

    [Header("UI References")]
    [SerializeField] private Button _button;
    [SerializeField] private Image _outlineImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _levelText;

    [Header("State Styles")]
    [SerializeField] private UpgradeNodeStateStyle _lockedStyle;
    [SerializeField] private UpgradeNodeStateStyle _purchasableStyle;
    [SerializeField] private UpgradeNodeStateStyle _unavailableStyle;
    [SerializeField] private UpgradeNodeStateStyle _completedStyle;

    private bool _pointerInside;
    private bool _selected;
    private bool _wasFocused;

    public UpgradeNodeDefinition Definition => _definition;
    public UpgradeNodeVisualState VisualState => _visualState;
    public bool IsPointerInside => _pointerInside;
    public bool IsSelected => _selected;
    public bool IsFocused => _pointerInside || _selected;
    public Color OutlineColor => _outlineImage != null
        ? _outlineImage.color
        : Color.white;

    public event Action<UpgradeNodeUI> Clicked;
    public event Action<UpgradeNodeUI> PointerEntered;
    public event Action<UpgradeNodeUI> PointerExited;
    public event Action<UpgradeNodeUI> Focused;
    public event Action<UpgradeNodeUI> Unfocused;

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

        _pointerInside = false;
        _selected = false;
        UpdateFocusState();
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
        gameObject.SetActive(state != UpgradeNodeVisualState.Locked);
        ApplyVisualState(state);
    }

    private void ApplyVisualState(UpgradeNodeVisualState state)
    {
        UpgradeNodeStateStyle style = state switch
        {
            UpgradeNodeVisualState.Locked => _lockedStyle,
            UpgradeNodeVisualState.Purchasable => _purchasableStyle,
            UpgradeNodeVisualState.Unavailable => _unavailableStyle,
            UpgradeNodeVisualState.Completed => _completedStyle,
            _ => _lockedStyle
        };

        if (_outlineImage != null)
        {
            _outlineImage.color = style.OutlineColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerInside = true;
        PointerEntered?.Invoke(this);
        UpdateFocusState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerInside = false;
        PointerExited?.Invoke(this);
        UpdateFocusState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _selected = true;
        UpdateFocusState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _selected = false;
        UpdateFocusState();
    }

    private void HandleClick()
    {
        Clicked?.Invoke(this);
    }

    private void UpdateFocusState()
    {
        bool isFocused = IsFocused;
        if (isFocused == _wasFocused)
        {
            return;
        }

        _wasFocused = isFocused;
        if (isFocused)
        {
            Focused?.Invoke(this);
        }
        else
        {
            Unfocused?.Invoke(this);
        }
    }
}
