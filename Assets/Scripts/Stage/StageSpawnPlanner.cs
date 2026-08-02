using System;
using System.Collections.Generic;

public static class StageSpawnPlanner
{
    public static int SampleGuaranteedCount(
        int maximumCount,
        float respawnProbability,
        Random random)
    {
        int safeMaximum = Math.Max(0, maximumCount);
        double probability = Math.Max(
            0d,
            Math.Min(1d, respawnProbability));

        if (safeMaximum == 0 || probability <= 0d)
        {
            return 0;
        }

        if (probability >= 1d)
        {
            return safeMaximum;
        }

        if (random == null)
        {
            throw new ArgumentNullException(nameof(random));
        }

        int count = 0;
        for (int i = 0; i < safeMaximum; i++)
        {
            if (random.NextDouble() <= probability)
            {
                count++;
            }
        }

        return count;
    }

    public static int SelectWeightedIndex(
        IReadOnlyList<float> weights,
        Random random)
    {
        if (weights == null)
        {
            throw new ArgumentNullException(nameof(weights));
        }

        if (random == null)
        {
            throw new ArgumentNullException(nameof(random));
        }

        double totalWeight = 0d;
        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += Math.Max(0d, weights[i]);
        }

        if (totalWeight <= double.Epsilon)
        {
            return -1;
        }

        double selection = random.NextDouble() * totalWeight;
        double cumulative = 0d;
        int lastPositiveIndex = -1;
        for (int i = 0; i < weights.Count; i++)
        {
            double weight = Math.Max(0d, weights[i]);
            if (weight <= 0d)
            {
                continue;
            }

            lastPositiveIndex = i;
            cumulative += weight;
            if (selection < cumulative)
            {
                return i;
            }
        }

        return lastPositiveIndex;
    }

    public static int[] AllocateByWeight(
        int count,
        IReadOnlyList<float> weights,
        Random random)
    {
        if (weights == null)
        {
            throw new ArgumentNullException(nameof(weights));
        }

        int safeCount = Math.Max(0, count);
        int[] allocations = new int[weights.Count];
        if (safeCount == 0 || weights.Count == 0)
        {
            return allocations;
        }

        double totalWeight = 0d;
        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += Math.Max(0d, weights[i]);
        }

        if (totalWeight <= double.Epsilon)
        {
            return allocations;
        }

        random ??= new Random(0);
        List<AreaRemainder> remainders = new(weights.Count);
        int assigned = 0;

        for (int i = 0; i < weights.Count; i++)
        {
            double raw = safeCount * Math.Max(0d, weights[i]) / totalWeight;
            int baseCount = (int)Math.Floor(raw);
            allocations[i] = baseCount;
            assigned += baseCount;
            remainders.Add(new AreaRemainder(i, raw - baseCount, random.Next()));
        }

        remainders.Sort(CompareRemainders);
        int remaining = safeCount - assigned;
        for (int i = 0; i < remaining; i++)
        {
            allocations[remainders[i].Index]++;
        }

        return allocations;
    }

    private static int CompareRemainders(AreaRemainder left, AreaRemainder right)
    {
        int remainderComparison = right.Remainder.CompareTo(left.Remainder);
        return remainderComparison != 0
            ? remainderComparison
            : left.TieBreaker.CompareTo(right.TieBreaker);
    }

    private readonly struct AreaRemainder
    {
        public AreaRemainder(int index, double remainder, int tieBreaker)
        {
            Index = index;
            Remainder = remainder;
            TieBreaker = tieBreaker;
        }

        public int Index { get; }
        public double Remainder { get; }
        public int TieBreaker { get; }
    }
}
