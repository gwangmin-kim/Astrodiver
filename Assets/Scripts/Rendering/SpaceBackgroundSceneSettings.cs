using UnityEngine;

namespace Astrodiver.Rendering
{
    [DisallowMultipleComponent]
    public sealed class SpaceBackgroundSceneSettings : MonoBehaviour
    {
        [SerializeField] private SpaceBackgroundProfile _profile;

        public SpaceBackgroundProfile Profile => _profile;
    }
}
