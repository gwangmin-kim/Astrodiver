using System;
using System.Collections.Generic;
using UnityEngine;

public enum StageSpawnCategory
{
    Creature = 0,
    ResourceFloatage = 1
}

[Serializable]
public struct StageSpawnRect
{
    [SerializeField] private Vector2 _min;
    [SerializeField] private Vector2 _max;

    public StageSpawnRect(Vector2 min, Vector2 max)
    {
        _min = min;
        _max = max;
    }

    public Vector2 Min => Vector2.Min(_min, _max); // 영역 좌하단 지점
    public Vector2 Max => Vector2.Max(_min, _max); // 영역 우상단 지점
    public Vector2 Center => (Min + Max) * 0.5f; // 영역 중심 지점
    public Vector2 Size => Max - Min; // 영역의 크기
    public float Area => Size.x * Size.y; // 영역의 넓이
    public bool IsValid => Size.x > Mathf.Epsilon && Size.y > Mathf.Epsilon; // 유효한(넓이가 있는) 영역인지 여부

    /// <summary>
    /// 영역 내의 무작위 지점 반환
    /// </summary>
    public Vector2 GetRandomLocalPoint(System.Random random)
    {
        if (random == null)
        {
            throw new ArgumentNullException(nameof(random));
        }

        Vector2 min = Min;
        Vector2 max = Max;
        return new Vector2(
            Mathf.Lerp(min.x, max.x, (float)random.NextDouble()),
            Mathf.Lerp(min.y, max.y, (float)random.NextDouble()));
    }
}

[Serializable]
public sealed class StageSpawnAreaCollection
{
    [SerializeField] private List<StageSpawnRect> _creatureAreas = new();
    [SerializeField] private List<StageSpawnRect> _resourceAreas = new();

    public IReadOnlyList<StageSpawnRect> CreatureAreas =>
        _creatureAreas != null
            ? _creatureAreas
            : Array.Empty<StageSpawnRect>();
    public IReadOnlyList<StageSpawnRect> ResourceAreas =>
        _resourceAreas != null
            ? _resourceAreas
            : Array.Empty<StageSpawnRect>();

    public IReadOnlyList<StageSpawnRect> GetAreas(StageSpawnCategory category)
    {
        return category switch
        {
            StageSpawnCategory.Creature => CreatureAreas,
            StageSpawnCategory.ResourceFloatage => ResourceAreas,
            _ => Array.Empty<StageSpawnRect>()
        };
    }

    public bool TryValidate(out string error)
    {
        List<string> errors = new();
        ValidateAreas(CreatureAreas, StageSpawnCategory.Creature, errors);
        ValidateAreas(
            ResourceAreas,
            StageSpawnCategory.ResourceFloatage,
            errors);

        error = string.Join(Environment.NewLine, errors);
        return errors.Count == 0;
    }

    private static void ValidateAreas(
        IReadOnlyList<StageSpawnRect> areas,
        StageSpawnCategory category,
        ICollection<string> errors)
    {
        for (int i = 0; i < areas.Count; i++)
        {
            if (!areas[i].IsValid)
            {
                errors.Add($"{category} spawn area [{i}] has no area.");
            }
        }
    }
}
