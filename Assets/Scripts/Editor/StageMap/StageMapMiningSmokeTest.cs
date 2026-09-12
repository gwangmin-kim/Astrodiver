using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class StageMapMiningSmokeTest
{
    [MenuItem("Astrodiver/Tests/Run Stage Map Mining Smoke Test")]
    public static void Run()
    {
        GameObject root = null;
        MiningTileDefinition definition = null;
        Tile visualTile = null;
        Tile protectedTile = null;
        try
        {
            root = new GameObject("StageMapMiningSmokeTest");
            Grid grid = root.AddComponent<Grid>();
            StageMap map = root.AddComponent<StageMap>();
            Tilemap platformLogic = CreateTilemap(root.transform, "PlatformLogic");
            platformLogic.gameObject.AddComponent<TilemapCollider2D>();
            Tilemap platformVisual = CreateTilemap(root.transform, "PlatformVisual");
            Tilemap decorationBackLogic = CreateTilemap(root.transform, "DecorationBackLogic");
            Tilemap decorationBackVisual = CreateTilemap(root.transform, "DecorationBackVisual");
            Tilemap decorationFrontLogic = CreateTilemap(root.transform, "DecorationFrontLogic");
            Tilemap decorationFrontVisual = CreateTilemap(root.transform, "DecorationFrontVisual");
            map.Configure(
                grid,
                platformLogic,
                decorationBackLogic,
                decorationFrontLogic,
                platformVisual,
                decorationBackVisual,
                decorationFrontVisual);

            definition = ScriptableObject.CreateInstance<MiningTileDefinition>();
            SetInt(definition, "_maximumHitPoints", 2);
            SetBool(definition, "_isDestructible", true);
            visualTile = ScriptableObject.CreateInstance<Tile>();
            protectedTile = ScriptableObject.CreateInstance<Tile>();
            protectedTile.colliderType = Tile.ColliderType.Grid;

            Vector3Int first = new(0, 0, 0);
            Vector3Int second = new(1, 0, 0);
            Vector3Int protectedCell = new(2, 0, 0);
            platformLogic.SetTile(first, definition);
            platformLogic.SetTile(second, definition);
            platformLogic.SetTile(protectedCell, protectedTile);
            platformVisual.SetTile(first, visualTile);
            platformVisual.SetTile(second, visualTile);
            platformVisual.SetTile(protectedCell, visualTile);

            Require(map.TryApplyMiningDamage(first, 1, out int firstRemaining) && firstRemaining == 1,
                "The first mining cell did not retain independent HP.");
            Require(map.TryGetMiningCell(second, out MiningTileDefinition secondDefinition, out int secondRemaining) &&
                    secondDefinition == definition && secondRemaining == 2,
                "Cells sharing a definition must keep independent HP.");
            Require(!map.TryApplyMiningDamage(protectedCell, 1, out _ ) && platformLogic.HasTile(protectedCell),
                "Common Platform tiles must remain protected.");

            int destructionCount = 0;
            StageMapMiningCellDestroyed destruction = default;
            map.MiningCellDestroyed += destroyed =>
            {
                destructionCount++;
                destruction = destroyed;
            };
            Require(map.TryApplyMiningDamage(first, 1, out int destroyedRemaining) && destroyedRemaining == 0,
                "The final mining damage was not accepted.");
            Require(!platformLogic.HasTile(first) && !platformVisual.HasTile(first),
                "Destruction must remove both Logic and Visual cells.");
            Require(destructionCount == 1 && destruction.Cell == first && destruction.Definition == definition,
                "Mining destruction must emit exactly one event with cell and definition.");
            Require(!map.TryApplyMiningDamage(first, 1, out _ ) && destructionCount == 1,
                "A destroyed cell must not emit a duplicate event.");

            GameObject reenteredRoot = new("StageMapMiningSmokeTestReentry");
            try
            {
                Grid reenteredGrid = reenteredRoot.AddComponent<Grid>();
                StageMap reenteredMap = reenteredRoot.AddComponent<StageMap>();
                Tilemap reenteredLogic = CreateTilemap(reenteredRoot.transform, "PlatformLogic");
                Tilemap reenteredVisual = CreateTilemap(reenteredRoot.transform, "PlatformVisual");
                reenteredMap.Configure(
                    reenteredGrid,
                    reenteredLogic,
                    CreateTilemap(reenteredRoot.transform, "DecorationBackLogic"),
                    CreateTilemap(reenteredRoot.transform, "DecorationFrontLogic"),
                    reenteredVisual,
                    CreateTilemap(reenteredRoot.transform, "DecorationBackVisual"),
                    CreateTilemap(reenteredRoot.transform, "DecorationFrontVisual"));
                reenteredLogic.SetTile(first, definition);
                reenteredVisual.SetTile(first, visualTile);
                Require(reenteredMap.TryGetMiningCell(first, out _, out int reenteredHitPoints) &&
                        reenteredHitPoints == 2,
                    "A new StageMap instance must start from the authored cell HP.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(reenteredRoot);
            }

            Debug.Log("Stage Map mining smoke test passed.");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
            if (visualTile != null) UnityEngine.Object.DestroyImmediate(visualTile);
            if (protectedTile != null) UnityEngine.Object.DestroyImmediate(protectedTile);
        }
    }

    private static Tilemap CreateTilemap(Transform parent, string name)
    {
        GameObject child = new(name);
        child.transform.SetParent(parent, false);
        return child.AddComponent<Tilemap>();
    }

    private static void SetInt(UnityEngine.Object target, string propertyName, int value)
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        SerializedObject serialized = new(target);
        serialized.FindProperty(propertyName).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
