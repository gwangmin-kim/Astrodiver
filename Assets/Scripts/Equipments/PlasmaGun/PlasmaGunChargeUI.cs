using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlasmaGunChargeUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;

    private Vector3 _baseLocalScale;

    private void Awake()
    {
        _baseLocalScale = transform.localScale;
        ConfigureFillImage();
        SetCharging(false, 0f);
    }

    public void SetCharging(bool isCharging, float progress)
    {
        if (!isCharging)
        {
            if (_fillImage != null) _fillImage.fillAmount = 0f;
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (_fillImage != null) _fillImage.fillAmount = Mathf.Clamp01(progress);
    }

    private void LateUpdate()
    {
        // 위치는 총구의 자식 Transform으로 따라가되, 게이지 자체는 월드 회전 0을 유지한다.
        transform.rotation = Quaternion.identity;
        CancelInheritedMirror();
    }

    private void CancelInheritedMirror()
    {
        if (transform.parent == null) return;

        // 왼쪽 조준 시 Hand의 X 스케일이 -1이 된다. UI에도 반전이 상속되지 않도록
        // 로컬 X 스케일의 부호를 반대로 적용해 월드 공간에서의 미러링을 상쇄한다.
        bool parentIsMirrored = transform.parent.localToWorldMatrix.determinant < 0f;
        float xScale = Mathf.Abs(_baseLocalScale.x) * (parentIsMirrored ? -1f : 1f);
        transform.localScale = new Vector3(xScale, _baseLocalScale.y, _baseLocalScale.z);
    }

    private void ConfigureFillImage()
    {
        if (_fillImage == null) return;

        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Radial360;
        _fillImage.fillOrigin = (int)Image.Origin360.Top;
        _fillImage.fillClockwise = true;
        _fillImage.raycastTarget = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ConfigureFillImage();
    }
#endif
}
