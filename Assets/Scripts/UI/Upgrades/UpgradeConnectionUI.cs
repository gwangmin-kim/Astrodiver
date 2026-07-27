using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UpgradeConnectionUI : MonoBehaviour
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private Image _lineImage;

    private RectTransform _parentNode;
    private RectTransform _childNode;
    private RectTransform _coordinateSpace;
    private float _width;

    public void SetNodes(
        RectTransform parentNode,
        RectTransform childNode,
        RectTransform coordinateSpace,
        float width,
        Color color)
    {
        _parentNode = parentNode;
        _childNode = childNode;
        _coordinateSpace = coordinateSpace;
        _width = Mathf.Max(1f, width);
        _lineImage.color = color;
        RefreshGeometry();
    }

    public void RefreshGeometry()
    {
        if (_parentNode == null || _childNode == null || _coordinateSpace == null)
        {
            return;
        }

        Vector2 start = _coordinateSpace.InverseTransformPoint(_parentNode.position);
        Vector2 end = _coordinateSpace.InverseTransformPoint(_childNode.position);
        Vector2 direction = end - start;

        _rectTransform.anchoredPosition = (start + end) * 0.5f;
        _rectTransform.sizeDelta = new Vector2(direction.magnitude, _width);
        _rectTransform.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }
}
