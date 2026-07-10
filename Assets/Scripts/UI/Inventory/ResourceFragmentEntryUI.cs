using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ResourceFragmentEntryUI : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Text _amountText;
    [SerializeField][Min(12f)] private float _iconSize = 22f;
    [SerializeField][Min(8)] private int _amountFontSize = 16;
    [SerializeField] private Color _amountColor = Color.white;

    public void SetResource(ResourceDefinition definition, int amount)
    {
        EnsureLayout();

        _iconImage.sprite = definition != null ? definition.Icon : null;
        _iconImage.enabled = _iconImage.sprite != null;
        _amountText.text = amount.ToString();
    }

    private void OnEnable()
    {
        EnsureLayout();
    }

    private void EnsureLayout()
    {
        RectTransform rectTransform = EnsureComponent<RectTransform>(gameObject);
        rectTransform.sizeDelta = new Vector2(160f, 28f);

        HorizontalLayoutGroup layoutGroup = EnsureComponent<HorizontalLayoutGroup>(gameObject);
        layoutGroup.childAlignment = TextAnchor.MiddleRight;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.spacing = 6f;

        _iconImage = EnsureChildImage(_iconImage, "Icon");
        _iconImage.preserveAspect = true;
        _iconImage.raycastTarget = false;
        LayoutElement iconLayout = EnsureComponent<LayoutElement>(_iconImage.gameObject);
        iconLayout.preferredWidth = _iconSize;
        iconLayout.preferredHeight = _iconSize;

        _amountText = EnsureChildText(_amountText, "Amount");
        _amountText.alignment = TextAnchor.MiddleLeft;
        _amountText.color = _amountColor;
        _amountText.fontSize = _amountFontSize;
        _amountText.raycastTarget = false;
        if (_amountText.font == null)
        {
            _amountText.font = GetDefaultFont();
        }

        LayoutElement textLayout = EnsureComponent<LayoutElement>(_amountText.gameObject);
        textLayout.preferredWidth = 76f;
        textLayout.preferredHeight = 28f;
    }

    private Image EnsureChildImage(Image current, string childName)
    {
        if (current != null) return current;

        Transform child = transform.Find(childName);
        GameObject childObject = child != null ? child.gameObject : CreateChild(childName);
        return EnsureComponent<Image>(childObject);
    }

    private Text EnsureChildText(Text current, string childName)
    {
        if (current != null) return current;

        Transform child = transform.Find(childName);
        GameObject childObject = child != null ? child.gameObject : CreateChild(childName);
        return EnsureComponent<Text>(childObject);
    }

    private GameObject CreateChild(string childName)
    {
        GameObject childObject = new(childName, typeof(RectTransform));
        childObject.transform.SetParent(transform, false);
        return childObject;
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
