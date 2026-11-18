using UnityEngine;

namespace Astrodiver.Stages.Map
{
    /// <summary>
    /// 오버레이 렌더링 전 단계의 확인용. 플레이 중 Scene 뷰에 광석 셀을 OreDefinition.DebugColor로 그리며,
    /// 풍부도가 높을수록 사각형이 크고 진하게 표시된다.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(StageMap))]
    public sealed class StageOreDebugGizmos : MonoBehaviour
    {
        [SerializeField] private bool _onlyWhenSelected = true;
        [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.25f;

        private void OnDrawGizmos() { if (!_onlyWhenSelected) Draw(); }
        private void OnDrawGizmosSelected() { if (_onlyWhenSelected) Draw(); }

        private void Draw()
        {
            StageMap map = GetComponent<StageMap>();
            if (map == null || map.Tilemap == null) return;

            BoundsInt bounds = map.OreBounds;
            Color previous = Gizmos.color;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int cell = new(x, y, 0);
                    if (!map.TryGetOre(cell, out OreDefinition ore, out float richness)) continue;

                    Color color = ore.DebugColor;
                    color.a = Mathf.Lerp(_minAlpha, 1f, richness);
                    Gizmos.color = color;
                    float size = Mathf.Lerp(0.3f, 0.9f, richness);
                    Gizmos.DrawCube(map.Tilemap.GetCellCenterWorld(cell), new Vector3(size, size, 0.01f));
                }
            }
            Gizmos.color = previous;
        }
    }
}
