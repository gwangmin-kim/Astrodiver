using UnityEngine;

[CreateAssetMenu(
    fileName = "FloatageDefinition",
    menuName = "Astrodiver/World/Floatage Definition")]
public sealed class FloatageDefinition : GameDefinition
{
    [Header("Drop Settings")]
    [SerializeField] private ResourceDefinition _dropResource;

    public ResourceDefinition DropResource => _dropResource;

    public bool TryValidate(out string error)
    {
        if (_dropResource == null)
        {
            error = $"Floatage definition '{name}' requires a drop resource.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
