using Astrodiver.Persistence;
using Astrodiver.ResourceFragments;
using UnityEngine;

namespace Astrodiver.Stages.Map
{
    /// <summary>Connects a map's destruction events to the existing resource collection flow.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(StageMap))]
    public sealed class MiningFragmentDrops : MonoBehaviour
    {
        private const float DropRadius = 0.15f;
        private StageMap _map;

        private void OnEnable()
        {
            _map = GetComponent<StageMap>();
            _map.MiningCellDestroyed += HandleCellDestroyed;
        }

        private void OnDisable()
        {
            if (_map != null) _map.MiningCellDestroyed -= HandleCellDestroyed;
            _map = null;
        }

        private void HandleCellDestroyed(StageMapMiningCellDestroyed destroyed)
        {
            if (!Application.isPlaying) return;
            FragmentParticleManager fragments = FragmentParticleManager.Instance;
            if (fragments == null)
            {
                Debug.LogError("Mining drops require a FragmentParticleManager in the session.", this);
                return;
            }

            float multiplier = GameDataManager.Instance?.RuntimeData?.MiningTileDropMultipliers
                .GetMultiplier(destroyed.Definition) ?? 1f;
            int count = CalculateDropCount(destroyed.Definition.BaseDropAmount, multiplier);
            fragments.DropFragment(destroyed.WorldPosition, destroyed.Definition.DropResource,
                DropRadius, count);

            // 부여된 광석: 베이스 드롭과 별개로 풍부도에 비례한 개수를 추가로 뱉는다.
            // (업그레이드 배율은 스테이지 구성 후 전체 재검토 예정이라 아직 적용하지 않는다.)
            OreDefinition ore = destroyed.Ore;
            if (ore != null && ore.Resource != null)
            {
                int oreCount = ore.RollDropCount(destroyed.OreRichness, Random.value);
                fragments.DropFragment(destroyed.WorldPosition, ore.Resource, DropRadius, oreCount);
            }
        }

        public static int CalculateDropCount(int baseCount, float multiplier)
        {
            int normalizedBaseCount = Mathf.Max(1, baseCount);
            float normalizedMultiplier =
                float.IsNaN(multiplier) || float.IsInfinity(multiplier)
                    ? 1f
                    : Mathf.Max(1f, multiplier);
            double scaled = normalizedBaseCount * (double)normalizedMultiplier;
            if (scaled >= int.MaxValue)
            {
                return int.MaxValue;
            }

            return Mathf.Max(1, Mathf.RoundToInt((float)scaled));
        }

    }
}
