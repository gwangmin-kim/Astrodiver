using UnityEngine;

[CreateAssetMenu(fileName = "CreatureDefinition", menuName = "Astrodiver/Inventory/Creature Definition")]
public sealed class CreatureDefinition : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField][Min(1)] private int _maxStackCount = 20;

    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public int MaxStackCount => Mathf.Max(1, _maxStackCount);
}
