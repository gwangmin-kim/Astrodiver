using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StageSystemSceneBuilder
{
    private const string StageDataFolder = "Assets/Data/Stages";
    private const string StageOneScene = "Assets/Scenes/Stage_1.unity";
    private const string StageTwoScene = "Assets/Scenes/Stage_2.unity";

    private const string StoneCreaturePrefab =
        "Assets/Prefabs/Resources/Creatures/Creature_1_Stone.prefab";
    private const string IronCreaturePrefab =
        "Assets/Prefabs/Resources/Creatures/Creature_2_Iron.prefab";
    private const string StoneFloatagePrefab =
        "Assets/Prefabs/Resources/Floatages/Floatage_1_Stone.prefab";
    private const string IronFloatagePrefab =
        "Assets/Prefabs/Resources/Floatages/Floatage_2_Iron.prefab";

    [MenuItem("Astrodiver/Stage/Setup Stage Scenes")]
    public static void SetupStageScenes()
    {
        string returnScenePath = SceneManager.GetActiveScene().path;
        EnsureFolder("Assets/Data", "Stages");

        SetupStage(
            StageOneScene,
            "stage_1",
            Population(
                9,
                0.5f,
                Entry("stone_creature", StoneCreaturePrefab, 2f / 3f),
                Entry("iron_creature", IronCreaturePrefab, 1f / 3f)),
            Population(
                7,
                0.5f,
                Entry("stone_floatage", StoneFloatagePrefab, 5f / 7f),
                Entry("iron_floatage", IronFloatagePrefab, 2f / 7f)));

        SetupStage(
            StageTwoScene,
            "stage_2",
            Population(
                9,
                0.5f,
                Entry("stone_creature", StoneCreaturePrefab, 1f / 3f),
                Entry("iron_creature", IronCreaturePrefab, 2f / 3f)),
            Population(
                7,
                0.5f,
                Entry("stone_floatage", StoneFloatagePrefab, 2f / 7f),
                Entry("iron_floatage", IronFloatagePrefab, 5f / 7f)));

        AssetDatabase.SaveAssets();
        if (!string.IsNullOrWhiteSpace(returnScenePath))
        {
            EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
        }

        Debug.Log("Stage_1 and Stage_2 spawn systems are ready.");
    }

    private static void SetupStage(
        string scenePath,
        string stageId,
        PopulationSetup creatures,
        PopulationSetup resources)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        StageDefinition definition = GetOrCreateDefinition(
            stageId,
            creatures,
            resources);

        GameObject stageRoot = GameObject.Find("StageRoot");
        if (stageRoot == null)
        {
            stageRoot = new GameObject("StageRoot");
        }

        Transform runtimeRoot = EnsureChild(stageRoot.transform, "Runtime");
        Transform creatureRuntimeRoot = EnsureChild(runtimeRoot, "Creatures");
        Transform resourceRuntimeRoot = EnsureChild(runtimeRoot, "ResourceFloatages");

        RectSetup[] creatureAreas =
        {
            RectFromCenterSize(
                new Vector2(-7f, 3f),
                new Vector2(10f, 8f)),
            RectFromCenterSize(
                new Vector2(7f, 3f),
                new Vector2(6f, 10f))
        };
        RectSetup[] resourceAreas =
        {
            RectFromCenterSize(
                new Vector2(-6f, -5f),
                new Vector2(8f, 5f)),
            RectFromCenterSize(
                new Vector2(7f, -4f),
                new Vector2(10f, 6f))
        };

        StagePopulationManager manager =
            stageRoot.GetComponent<StagePopulationManager>() ??
            stageRoot.AddComponent<StagePopulationManager>();
        SerializedObject managerSerialized = new(manager);
        managerSerialized.FindProperty("_definition").objectReferenceValue = definition;
        ConfigureAreasIfEmpty(
            managerSerialized.FindProperty("_spawnAreas"),
            creatureAreas,
            resourceAreas);
        managerSerialized.FindProperty("_creatureRuntimeRoot").objectReferenceValue = creatureRuntimeRoot;
        managerSerialized.FindProperty("_resourceRuntimeRoot").objectReferenceValue = resourceRuntimeRoot;
        managerSerialized.ApplyModifiedPropertiesWithoutUndo();

        ConfigureWorldBounds();

        Transform obsoleteAreaRoot = stageRoot.transform.Find("SpawnAreas");
        if (obsoleteAreaRoot != null)
        {
            Object.DestroyImmediate(obsoleteAreaRoot.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureWorldBounds()
    {
        GameObject boundsObject = GameObject.Find("WorldBounds");
        if (boundsObject == null)
        {
            boundsObject = new GameObject("WorldBounds");
            boundsObject.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            boundsObject.transform.localScale = Vector3.one;
        }

        WorldBounds2D worldBounds =
            boundsObject.GetComponent<WorldBounds2D>() ??
            boundsObject.AddComponent<WorldBounds2D>();
        if (boundsObject.GetComponent<PolygonCollider2D>() == null)
        {
            boundsObject.AddComponent<PolygonCollider2D>();
        }

        BoxCollider2D obsoleteBoxCollider =
            boundsObject.GetComponent<BoxCollider2D>();
        if (obsoleteBoxCollider != null)
        {
            Object.DestroyImmediate(obsoleteBoxCollider);
        }

        CinemachineCamera virtualCamera =
            Object.FindAnyObjectByType<CinemachineCamera>();
        if (virtualCamera == null)
        {
            Debug.LogWarning(
                "A CinemachineCamera was not found. WorldBounds was created, " +
                "but a Confiner2D could not be configured.");
            return;
        }

        CinemachineConfiner2D confiner =
            virtualCamera.GetComponent<CinemachineConfiner2D>() ??
            virtualCamera.gameObject.AddComponent<CinemachineConfiner2D>();
        confiner.BoundingShape2D = worldBounds.BoundaryCollider;
        confiner.Damping = 0f;
        confiner.SlowingDistance = 0f;
        confiner.InvalidateBoundingShapeCache();
        EditorUtility.SetDirty(virtualCamera.gameObject);
    }

    private static StageDefinition GetOrCreateDefinition(
        string stageId,
        PopulationSetup creatures,
        PopulationSetup resources)
    {
        string assetPath = $"{StageDataFolder}/{stageId}.asset";
        StageDefinition definition =
            AssetDatabase.LoadAssetAtPath<StageDefinition>(assetPath);
        bool isNew = definition == null;
        if (isNew)
        {
            definition = ScriptableObject.CreateInstance<StageDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);
        }

        SerializedObject serialized = new(definition);
        serialized.FindProperty("_stageId").stringValue = stageId;
        SerializedProperty respawnInterval =
            serialized.FindProperty("_respawnIntervalSeconds");
        if (isNew || respawnInterval.floatValue < 0.1f)
        {
            respawnInterval.floatValue = 5f;
        }

        ConfigurePopulationIfEmpty(
            serialized.FindProperty("_creatures"),
            creatures);
        ConfigurePopulationIfEmpty(
            serialized.FindProperty("_resourceFloatages"),
            resources);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void ConfigurePopulationIfEmpty(
        SerializedProperty population,
        PopulationSetup setup)
    {
        SerializedProperty entries = population.FindPropertyRelative("_entries");
        if (entries.arraySize > 0)
        {
            return;
        }

        population.FindPropertyRelative("_maxCount").intValue = setup.MaxCount;
        population.FindPropertyRelative("_respawnProbability").floatValue =
            setup.RespawnProbability;
        ConfigureEntries(entries, setup.Entries);
    }

    private static void ConfigureEntries(
        SerializedProperty entries,
        IReadOnlyList<EntrySetup> setups)
    {
        entries.arraySize = setups.Count;
        for (int i = 0; i < setups.Count; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("_entryId").stringValue = setups[i].Id;
            entry.FindPropertyRelative("_prefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(setups[i].PrefabPath);
            entry.FindPropertyRelative("_spawnWeight").floatValue =
                setups[i].Weight;
        }
    }

    private static void ConfigureAreasIfEmpty(
        SerializedProperty collection,
        IReadOnlyList<RectSetup> creatureAreas,
        IReadOnlyList<RectSetup> resourceAreas)
    {
        SerializedProperty creatures =
            collection.FindPropertyRelative("_creatureAreas");
        SerializedProperty resources =
            collection.FindPropertyRelative("_resourceAreas");
        if (creatures.arraySize > 0 || resources.arraySize > 0)
        {
            return;
        }

        ConfigureAreas(creatures, creatureAreas);
        ConfigureAreas(resources, resourceAreas);
    }

    private static void ConfigureAreas(
        SerializedProperty areas,
        IReadOnlyList<RectSetup> setups)
    {
        areas.arraySize = setups.Count;
        for (int i = 0; i < setups.Count; i++)
        {
            SerializedProperty area = areas.GetArrayElementAtIndex(i);
            area.FindPropertyRelative("_min").vector2Value = setups[i].Min;
            area.FindPropertyRelative("_max").vector2Value = setups[i].Max;
        }
    }

    private static RectSetup RectFromCenterSize(Vector2 center, Vector2 size)
    {
        Vector2 halfSize = size * 0.5f;
        return new RectSetup(center - halfSize, center + halfSize);
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        child = new GameObject(childName).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static PopulationSetup Population(
        int maxCount,
        float respawnProbability,
        params EntrySetup[] entries)
    {
        return new PopulationSetup(
            maxCount,
            respawnProbability,
            entries);
    }

    private static EntrySetup Entry(string id, string prefabPath, float weight)
    {
        return new EntrySetup(id, prefabPath, weight);
    }

    private readonly struct EntrySetup
    {
        public EntrySetup(string id, string prefabPath, float weight)
        {
            Id = id;
            PrefabPath = prefabPath;
            Weight = weight;
        }

        public string Id { get; }
        public string PrefabPath { get; }
        public float Weight { get; }
    }

    private readonly struct PopulationSetup
    {
        public PopulationSetup(
            int maxCount,
            float respawnProbability,
            EntrySetup[] entries)
        {
            MaxCount = maxCount;
            RespawnProbability = respawnProbability;
            Entries = entries;
        }

        public int MaxCount { get; }
        public float RespawnProbability { get; }
        public EntrySetup[] Entries { get; }
    }

    private readonly struct RectSetup
    {
        public RectSetup(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public Vector2 Min { get; }
        public Vector2 Max { get; }
    }
}
