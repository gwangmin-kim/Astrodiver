using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CreatureInventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Color _backgroundColor = new(0.08f, 0.1f, 0.13f, 0.82f);
    [SerializeField] private Color _emptyIconColor = new(1f, 1f, 1f, 0f);
    [SerializeField] private Color _filledIconColor = Color.white;
    [SerializeField] private Color _countColor = Color.white;
    [SerializeField][Range(8, 64)] private int _countFontSize = 16;

    public void SetSlot(CreatureInventorySlot slot)
    {
        EnsureLayout();

        if (slot.IsEmpty)
        {
            SetEmpty();
            return;
        }

        _iconImage.sprite = slot.Definition.Icon;
        _iconImage.color = _filledIconColor;
        _iconImage.enabled = slot.Definition.Icon != null;
        _countText.text = slot.Count.ToString();
        _countText.enabled = true;
    }

    public void SetEmpty()
    {
        EnsureLayout();

        _iconImage.sprite = null;
        _iconImage.color = _emptyIconColor;
        _iconImage.enabled = false;
        _countText.text = string.Empty;
        _countText.enabled = false;
    }

    private void OnEnable()
    {
        EnsureLayout();
    }

    private void EnsureLayout()
    {
        RectTransform rectTransform = EnsureRectTransform(gameObject);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        _backgroundImage = EnsureComponent(_backgroundImage, gameObject);
        _backgroundImage.color = _backgroundColor;
        _backgroundImage.raycastTarget = false;

        _iconImage = EnsureChildImage(_iconImage, "Icon");
        RectTransform iconRect = _iconImage.rectTransform;
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8f, 8f);
        iconRect.offsetMax = new Vector2(-8f, -8f);
        _iconImage.preserveAspect = true;
        _iconImage.raycastTarget = false;

        _countText = EnsureChildText(_countText, "Count");
        RectTransform countRect = _countText.rectTransform;
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.anchoredPosition = new Vector2(-4f, 4f);
        countRect.sizeDelta = new Vector2(52f, 22f);
        _countText.alignment = TextAlignmentOptions.BottomRight;
        _countText.color = _countColor;
        _countText.fontSize = _countFontSize;
        _countText.raycastTarget = false;
    }

    private Image EnsureChildImage(Image current, string childName)
    {
        if (current != null) return current;

        Transform child = transform.Find(childName);
        GameObject childObject = child != null ? child.gameObject : CreateChild(childName);
        return EnsureComponent<Image>(null, childObject);
    }

    private TextMeshProUGUI EnsureChildText(TextMeshProUGUI current, string childName)
    {
        if (current != null) return current;

        Transform child = transform.Find(childName);
        GameObject childObject = child != null ? child.gameObject : CreateChild(childName);
        return EnsureComponent<TextMeshProUGUI>(null, childObject);
    }

    private static T EnsureComponent<T>(T current, GameObject target) where T : Component
    {
        if (current != null) return current;

        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private GameObject CreateChild(string childName)
    {
        GameObject childObject = new(childName, typeof(RectTransform));
        childObject.transform.SetParent(transform, false);
        return childObject;
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        return rectTransform != null ? rectTransform : target.AddComponent<RectTransform>();
    }

}
