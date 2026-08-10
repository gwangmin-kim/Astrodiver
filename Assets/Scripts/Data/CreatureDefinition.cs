using UnityEngine;

[CreateAssetMenu(fileName = "CreatureDefinition", menuName = "Astrodiver/Inventory/Creature Definition")]
public sealed class CreatureDefinition : GameDefinition
{
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField][Min(1)] private int _maxStackCount = 20;

    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public int MaxStackCount => Mathf.Max(1, _maxStackCount);
}
