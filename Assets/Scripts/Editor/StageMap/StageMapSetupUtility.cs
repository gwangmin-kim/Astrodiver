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

        Tilemap platform = EnsureTilemap(
            gridRoot,
            StageMapLayer.Platform,
            sortingOrder: 0);
        Tilemap decorationBack = EnsureTilemap(
            gridRoot,
            StageMapLayer.DecorationBack,
            sortingOrder: -10);
        Tilemap decorationFront = EnsureTilemap(
            gridRoot,
            StageMapLayer.DecorationFront,
            sortingOrder: 10);

        ConfigurePlatformPhysics(platform.gameObject);
        RemoveDecorationPhysics(decorationBack.gameObject);
        RemoveDecorationPhysics(decorationFront.gameObject);

        StageMap stageMap = EnsureComponent<StageMap>(mapRoot.gameObject);
        Undo.RecordObject(stageMap, "Configure Stage Map");
        stageMap.Configure(grid, platform, decorationBack, decorationFront);
        stageMap.EnforceTransformLock();
        EditorUtility.SetDirty(stageMap);

        EditorSceneManager.MarkSceneDirty(stageRoot.scene);
        Selection.activeObject = stageMap;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("Stage Map hierarchy and default logical tiles are ready.", stageMap);
    }

    internal static StageMap FindCurrentStageMap()
    {
        return Object.FindAnyObjectByType<StageMap>();
    }

    private static Tilemap EnsureTilemap(
        Transform gridRoot,
        StageMapLayer layer,
        int sortingOrder)
    {
        Transform child = EnsureChild(gridRoot, layer.ToString());
        Tilemap tilemap = EnsureComponent<Tilemap>(child.gameObject);
        TilemapRenderer renderer = EnsureComponent<TilemapRenderer>(child.gameObject);
        renderer.sortingOrder = sortingOrder;
        renderer.mode = TilemapRenderer.Mode.Chunk;
        return tilemap;
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
