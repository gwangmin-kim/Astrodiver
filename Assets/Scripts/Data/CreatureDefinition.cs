using UnityEngine;

[CreateAssetMenu(fileName = "CreatureDefinition", menuName = "Astrodiver/Inventory/Creature Definition")]
public sealed class CreatureDefinition : GameDefinition
{
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;

    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
}
