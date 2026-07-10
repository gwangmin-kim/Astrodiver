using UnityEngine;

[CreateAssetMenu(fileName = "ResourceDefinition", menuName = "Astrodiver/Inventory/Resource Definition")]
public sealed class ResourceDefinition : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
}
