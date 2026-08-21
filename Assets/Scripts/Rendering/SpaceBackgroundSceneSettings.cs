using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpaceBackgroundSceneSettings : MonoBehaviour
{
    [SerializeField] private SpaceBackgroundProfile _profile;

    public SpaceBackgroundProfile Profile => _profile;
}
