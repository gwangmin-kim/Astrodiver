using UnityEngine;

namespace Astrodiver.Stages
{
    [DisallowMultipleComponent]
    public sealed class StageSceneSettings : MonoBehaviour
    {
        [SerializeField] private StageDefinition _definition;

        public StageDefinition Definition => _definition;
    }
}
