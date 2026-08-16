using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "FloatageDefinition",
    menuName = "Astrodiver/World/Floatage Definition")]
public sealed class FloatageDefinition : GameDefinition
{
    [Header("Durability Settings")]
    [SerializeField, Min(1)] private int _hp = 100;

    [Header("Drop Settings")]
    [SerializeField] private FragmentDropData _dropData;

    public int Hp => Mathf.Max(1, _hp);
    public FragmentDropData DropData => _dropData;

    public bool TryValidate(out string error)
    {
        if (_hp < 1)
        {
            error = $"Floatage definition '{name}' requires HP of at least 1.";
            return false;
        }

        if (_dropData.resource == null)
        {
            error = $"Floatage definition '{name}' requires a drop resource.";
            return false;
        }

        if (_dropData.count < 1)
        {
            error = $"Floatage definition '{name}' requires a drop count of at least 1.";
            return false;
        }

        if (_dropData.radius < 0f)
        {
            error = $"Floatage definition '{name}' cannot use a negative drop radius.";
            return false;
        }

        if (_dropData.lifetime < 1f)
        {
            error = $"Floatage definition '{name}' requires a drop lifetime of at least 1 second.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

[Serializable]
public struct FragmentDropData
{
    [Tooltip("Resource type created when this floatage is destroyed")]
    public ResourceDefinition resource;

    [Tooltip("Radius of the fragment spawn area")]
    [Min(0f)] public float radius;

    [Tooltip("Number of fragments to spawn")]
    public short count;

    [Tooltip("Lifetime of spawned fragments")]
    [Min(1f)] public float lifetime;
}
