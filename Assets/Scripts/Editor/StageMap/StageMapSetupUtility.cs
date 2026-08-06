using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

internal static class StageMapSetupUtility
{
    private const string StageRootName = "StageRoot";
    private const string MapName = "Map";
    private const string GridName = "Grid";

    [MenuItem("Astrodiver/Stage Map/Setup Current Stage")]
    internal static void SetupCurrentStage()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Stage Map setup is only available in Edit Mode.");
            return;
        }

        StageMapDefaultTiles.EnsureAssets();

        GameObject stageRoot = GameObject.Find(StageRootName);
        if (stageRoot == null)
        {
            stageRoot = CreateObject(StageRootName, null);
        }

        Transform mapRoot = EnsureChild(stageRoot.transform, MapName);
        Transform gridRoot = EnsureChild(mapRoot, GridName);
        Grid grid = EnsureComponent<Grid>(gridRoot.gameObject);
        grid.cellSize = StageMap.cellSize;
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        Tilemap platformLogic = EnsureLogicTilemap(
            gridRoot,
            StageMapLayer.Platform);
        Tilemap decorationBackLogic = EnsureLogicTilemap(
            gridRoot,
            StageMapLayer.DecorationBack);
        Tilemap decorationFrontLogic = EnsureLogicTilemap(
            gridRoot,
            StageMapLayer.DecorationFront);

        Tilemap platformVisual = EnsureVisualTilemap(
            gridRoot,
            StageMapLayer.Platform,
            sortingOrder: 0);
        Tilemap decorationBackVisual = EnsureVisualTilemap(
            gridRoot,
            StageMapLayer.DecorationBack,
            sortingOrder: -10);
        Tilemap decorationFrontVisual = EnsureVisualTilemap(
            gridRoot,
            StageMapLayer.DecorationFront,
            sortingOrder: 10);

        MigrateLayer(
            platformLogic,
            platformVisual,
            StageMapLayer.Platform);
        MigrateLayer(
            decorationBackLogic,
            decorationBackVisual,
            StageMapLayer.DecorationBack);
        MigrateLayer(
            decorationFrontLogic,
            decorationFrontVisual,
            StageMapLayer.DecorationFront);

        ConfigurePlatformPhysics(platformLogic.gameObject);
        RemoveDecorationPhysics(decorationBackLogic.gameObject);
        RemoveDecorationPhysics(decorationFrontLogic.gameObject);
        RemoveComponent<TilemapRenderer>(platformLogic.gameObject);
        RemoveComponent<TilemapRenderer>(decorationBackLogic.gameObject);
        RemoveComponent<TilemapRenderer>(decorationFrontLogic.gameObject);
        RemoveAllPhysics(platformVisual.gameObject);
        RemoveAllPhysics(decorationBackVisual.gameObject);
        RemoveAllPhysics(decorationFrontVisual.gameObject);

        StageMap stageMap = EnsureComponent<StageMap>(mapRoot.gameObject);
        Undo.RecordObject(stageMap, "Configure Stage Map");
        stageMap.Configure(
            grid,
            platformLogic,
            decorationBackLogic,
            decorationFrontLogic,
            platformVisual,
            decorationBackVisual,
            decorationFrontVisual);
        stageMap.EnforceTransformLock();
        EditorUtility.SetDirty(stageMap);

        EditorSceneManager.MarkSceneDirty(stageRoot.scene);
        Selection.activeObject = stageMap;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log(
            "Stage Map Logic/Visual pairs are configured and migrated.",
            stageMap);
    }

    internal static StageMap FindCurrentStageMap()
    {
        return Object.FindAnyObjectByType<StageMap>();
    }

    private static Tilemap EnsureLogicTilemap(
        Transform gridRoot,
        StageMapLayer layer)
    {
        Transform child = EnsureChild(gridRoot, layer.ToString());
        return EnsureComponent<Tilemap>(child.gameObject);
    }

    private static Tilemap EnsureVisualTilemap(
        Transform gridRoot,
        StageMapLayer layer,
        int sortingOrder)
    {
        Transform child = EnsureChild(gridRoot, $"{layer}Visual");
        Tilemap tilemap = EnsureComponent<Tilemap>(child.gameObject);
        TilemapRenderer renderer =
            EnsureComponent<TilemapRenderer>(child.gameObject);
        renderer.sortingOrder = sortingOrder;
        renderer.mode = TilemapRenderer.Mode.Chunk;
        return tilemap;
    }

    private static void MigrateLayer(
        Tilemap logic,
        Tilemap visual,
        StageMapLayer layer)
    {
        Undo.RegisterCompleteObjectUndo(
            new Object[] { logic, visual },
            "Migrate Stage Map Layer");

        TileBase logicalTile = StageMapDefaultTiles.GetLogical(layer);
        TileBase defaultVisual =
            StageMapDefaultTiles.GetVisualDefault(layer);
        foreach (Vector3Int cell in logic.cellBounds.allPositionsWithin)
        {
            TileBase existing = logic.GetTile(cell);
            if (existing == null)
            {
                continue;
            }

            if (!visual.HasTile(cell))
            {
                visual.SetTile(
                    cell,
                    StageMapDefaultTiles.IsLogicalTile(existing)
                        ? defaultVisual
                        : existing);
            }

            logic.SetTile(cell, logicalTile);
        }

        foreach (Vector3Int cell in visual.cellBounds.allPositionsWithin)
        {
            if (!logic.HasTile(cell))
            {
                visual.SetTile(cell, null);
            }
        }

        logic.CompressBounds();
        visual.CompressBounds();
        logic.RefreshAllTiles();
        visual.RefreshAllTiles();
        EditorUtility.SetDirty(logic);
        EditorUtility.SetDirty(visual);
    }

    private static void ConfigurePlatformPhysics(GameObject target)
    {
        Rigidbody2D body = EnsureComponent<Rigidbody2D>(target);
        body.bodyType = RigidbodyType2D.Static;
        body.simulated = true;

        CompositeCollider2D composite =
            EnsureComponent<CompositeCollider2D>(target);
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType =
            CompositeCollider2D.GenerationType.Synchronous;

        TilemapCollider2D tilemapCollider =
            EnsureComponent<TilemapCollider2D>(target);
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    private static void RemoveDecorationPhysics(GameObject target)
    {
        RemoveComponent<TilemapCollider2D>(target);
        RemoveComponent<CompositeCollider2D>(target);
        RemoveComponent<Rigidbody2D>(target);
    }

    private static void RemoveAllPhysics(GameObject target)
    {
        RemoveDecorationPhysics(target);
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            NormalizeTransform(child);
            return child;
        }

        GameObject created = CreateObject(childName, parent);
        return created.transform;
    }

    private static GameObject CreateObject(string name, Transform parent)
    {
        GameObject created = new(name);
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        created.transform.SetParent(parent, false);
        NormalizeTransform(created.transform);
        return created;
    }

    private static void NormalizeTransform(Transform target)
    {
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;
    }

    private static T EnsureComponent<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = Undo.AddComponent<T>(target);
        }

        return component;
    }

    private static void RemoveComponent<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
        {
            Undo.DestroyObjectImmediate(component);
        }
    }
}
