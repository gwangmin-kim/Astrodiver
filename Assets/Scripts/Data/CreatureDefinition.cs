using UnityEngine;

[CreateAssetMenu(fileName = "CreatureDefinition", menuName = "Astrodiver/Inventory/Creature Definition")]
public sealed class CreatureDefinition : GameDefinition
{
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField, Min(0.01f)] private float _worktableProcessSeconds = 1f;

    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public float WorktableProcessSeconds =>
        Mathf.Max(0.01f, _worktableProcessSeconds);

    public bool TryValidate(out string error)
    {
        if (_worktableProcessSeconds < 0.01f ||
            float.IsNaN(_worktableProcessSeconds) ||
            float.IsInfinity(_worktableProcessSeconds))
        {
            error = $"Creature '{name}' requires a positive worktable process time.";
            return false;
        }

        error = null;
        return true;
    }
}
