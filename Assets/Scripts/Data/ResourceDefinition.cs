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

    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public int RowIndex => _particleRowIndex;
}
