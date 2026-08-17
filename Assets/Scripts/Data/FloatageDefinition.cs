using UnityEngine;

[CreateAssetMenu(
    fileName = "FloatageDefinition",
    menuName = "Astrodiver/World/Floatage Definition")]
public sealed class FloatageDefinition : GameDefinition
{
    [Header("Durability Settings")]
    [SerializeField, Min(1)] private int _hp = 100;

    [Header("Drop Settings")]
    [SerializeField] private ResourceDefinition _dropResource;

    public int Hp => Mathf.Max(1, _hp);
    public ResourceDefinition DropResource => _dropResource;

    public bool TryValidate(out string error)
    {
        if (_hp < 1)
        {
            error = $"Floatage definition '{name}' requires HP of at least 1.";
            return false;
        }

        if (_dropResource == null)
        {
            error = $"Floatage definition '{name}' requires a drop resource.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
