namespace Astrodiver.Stages.Map
{
    /// <summary>
    /// 생성된 셀 블록과 같은 인덱스 순서(행 우선, bounds 기준)로 정렬된 광석 부여 결과.
    /// Indices[i]는 Palette 인덱스(없으면 -1), Richness[i]는 0~1 풍부도.
    /// StageMap이 배열 소유권을 가져간다.
    /// </summary>
    public readonly struct StageOreLayerData
    {
        public StageOreLayerData(OreDefinition[] palette, sbyte[] indices, float[] richness)
        {
            Palette = palette;
            Indices = indices;
            Richness = richness;
        }

        public OreDefinition[] Palette { get; }
        public sbyte[] Indices { get; }
        public float[] Richness { get; }
        public bool IsEmpty => Palette == null || Palette.Length == 0 || Indices == null || Richness == null;
    }
}
