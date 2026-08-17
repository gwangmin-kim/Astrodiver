using UnityEngine;

[CreateAssetMenu(fileName = "ResourceDefinition", menuName = "Astrodiver/Inventory/Resource Definition")]
public sealed class ResourceDefinition : GameDefinition
{
    [Header("Basic Informations")]
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;

    [Header("Drop Informations")]
    [Tooltip("스프라이트 시트에서 선택할 열의 인덱스 (시각적 종류를 결정)")]
    [SerializeField][Min(0)] private int _particleRowIndex;

    [Tooltip("드롭된 자원 파티클의 생존 시간(초)")]
    [SerializeField, Min(1f)] private float _fragmentLifetime = 30f;

    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public int RowIndex => _particleRowIndex;
    public float FragmentLifetime => Mathf.Max(1f, _fragmentLifetime);

    public bool TryValidate(out string error)
    {
        if (_fragmentLifetime < 1f)
        {
            error = $"Resource definition '{name}' requires a fragment lifetime of at least 1 second.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
