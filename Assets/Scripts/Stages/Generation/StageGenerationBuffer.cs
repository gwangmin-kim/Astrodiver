using UnityEngine;

namespace Astrodiver.Stages.Generation
{
    internal sealed class StageGenerationBuffer
    {
        public StageGenerationBuffer(BoundsInt bounds)
        {
            Bounds = bounds;
            CellCount = bounds.size.x * bounds.size.y;
            Field = new float[CellCount];
            Solid = new bool[CellCount];
            OreIndex = new sbyte[CellCount];
            OreRichness = new float[CellCount];
            System.Array.Fill(OreIndex, (sbyte)-1);
        }

        public BoundsInt Bounds { get; }
        public int CellCount { get; }
        public float[] Field { get; }
        public bool[] Solid { get; }
        /// <summary>부여된 광석 규칙 인덱스. 없으면 -1.</summary>
        public sbyte[] OreIndex { get; }
        /// <summary>부여된 광석의 정규화된 풍부도(0~1).</summary>
        public float[] OreRichness { get; }

        public int Index(int x, int y) =>
            (y - Bounds.yMin) * Bounds.size.x + x - Bounds.xMin;

        public void Classify(float density)
        {
            for (int i = 0; i < CellCount; i++)
                Solid[i] = IsSolid(Field[i], density);
        }

        public static bool IsSolid(float field, float threshold) =>
            threshold >= 1f || (threshold > 0f && field < threshold);
    }
}
