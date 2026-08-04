using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class StageTemplateSceneUtility
{
    private const string ReferenceScenePath = "Assets/Scenes/Stage_1.unity";
    private const string TemplateFolder = "Assets/Scenes/Templates";
    private const string TemplateScenePath = TemplateFolder + "/EmptyStageTemplate.unity";
    private const string PlayerPrefabPath =
        "Assets/Prefabs/Player/SessionPlayer.prefab";
    private const string PersistentPrefabFolder =
        "Assets/Resources/Prefabs/DontDestroyOnLoad";

    private static readonly string[] PersistentObjectNames =
    {
        "GameDataManager",
        "SceneTransitionManager",
        "PlayerInventory"
    };

    [MenuItem("Astrodiver/Stage/Create Basic Stage Scene...")]
    public static void CreateBasicStageScene()
    {
        if (!CanEditScenes() || !EnsureTemplateExists())
        {
            return;
        }

        string absolutePath = EditorUtility.SaveFilePanel(
            "Create Basic Stage Scene",
            Path.GetFullPath("Assets/Scenes"),
            "NewStage",
            "unity");
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return;
        }

        string projectPath = FileUtil.GetProjectRelativePath(absolutePath);
        if (string.IsNullOrWhiteSpace(projectPath) ||
            !projectPath.StartsWith("Assets/"))
        {
            EditorUtility.DisplayDialog(
                "Invalid Location",
                "Create the stage scene inside this project's Assets folder.",
                "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(projectPath) != null &&
            !EditorUtility.DisplayDialog(
                "Replace Scene?",
                $"'{projectPath}' already exists. Replace it?",
                "Replace",
                "Cancel"))
        {
            return;
        }

        AssetDatabase.DeleteAsset(projectPath);
        if (!AssetDatabase.CopyAsset(TemplateScenePath, projectPath))
        {
            Debug.LogError($"Could not create stage scene at '{projectPath}'.");
            return;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(projectPath, OpenSceneMode.Single);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(projectPath);
        Debug.Log($"Created a basic stage scene at '{projectPath}'.");
    }

    [MenuItem("Astrodiver/Stage/Rebuild Basic Stage Template")]
    public static void RebuildTemplateMenu()
    {
        if (!CanEditScenes() ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        ApplyProjectMigration();
    }

    /// <summary>
    /// Creates the persistent-service prefabs, removes their scene copies, fixes the
    /// world-bounds hierarchy, and rebuilds the empty stage template.
    /// </summary>
    public static void ApplyProjectMigration()
    {
        string returnScenePath = SceneManager.GetActiveScene().path;
        EnsureFolder(PersistentPrefabFolder);
        CreatePersistentServicePrefabs();
        NormalizeAllScenes();
        RebuildTemplateAsset();
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrWhiteSpace(returnScenePath) &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(returnScenePath) != null)
        {
            EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
        }

        Debug.Log(
            "Persistent-service prefabs, scene bootstrap migration, and the basic stage template are ready.");
    }

    public static void ApplyStageRuntimeMigration()
    {
        string returnScenePath = SceneManager.GetActiveScene().path;
        string[] scenePaths =
        {
            "Assets/Scenes/Stage_1.unity",
            "Assets/Scenes/Stage_2.unity",
            TemplateScenePath
        };

        foreach (string scenePath in scenePaths)
        {
            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);
            if (NormalizeStageRuntime(scene))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        if (!string.IsNullOrWhiteSpace(returnScenePath) &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(returnScenePath) != null)
        {
            EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Stage player spawning and Runtime hierarchies are ready.");
    }

    private static bool CanEditScenes()
    {
        if (!EditorApplication.isPlaying)
        {
            return true;
        }

        Debug.LogWarning("Stage template tools are only available in Edit Mode.");
        return false;
    }

    private static bool EnsureTemplateExists()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplateScenePath) != null)
        {
            return true;
        }

        EditorUtility.DisplayDialog(
            "Template Missing",
            "Run Astrodiver/Stage/Rebuild Basic Stage Template first.",
            "OK");
        return false;
    }

    private static void CreatePersistentServicePrefabs()
    {
        if (PersistentPrefabExists("GameDataManager") &&
            PersistentPrefabExists("SceneTransitionManager") &&
            PersistentPrefabExists("PlayerInventory"))
        {
            return;
        }

        Scene hub = EditorSceneManager.OpenScene(
            "Assets/Scenes/Hub.unity",
            OpenSceneMode.Single);

        SaveServicePrefabIfMissing<GameDataManager>(hub, "GameDataManager");
        SaveServicePrefabIfMissing<SceneTransitionManager>(
            hub,
            "SceneTransitionManager");
        SaveServicePrefabIfMissing<PlayerInventoryController>(
            hub,
            "PlayerInventory");
    }

    private static void SaveServicePrefabIfMissing<T>(Scene scene, string prefabName)
        where T : Component
    {
        if (PersistentPrefabExists(prefabName))
        {
            return;
        }

        T component = FindSceneComponent<T>(scene);
        if (component == null)
        {
            Debug.LogError(
                $"Cannot create persistent prefab '{prefabName}': " +
                $"{typeof(T).Name} is missing from the Hub scene.");
            return;
        }

        string path = $"{PersistentPrefabFolder}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(component.gameObject, path);
    }

    private static bool PersistentPrefabExists(string prefabName)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{PersistentPrefabFolder}/{prefabName}.prefab") != null;
    }

    private static void NormalizeAllScenes()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == TemplateScenePath)
            {
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool changed = RemovePersistentServices(scene);
            if (IsStageScene(scene))
            {
                changed |= MoveWorldBoundsUnderMap(scene);
                changed |= NormalizeStageRuntime(scene);
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

    private static bool RemovePersistentServices(Scene scene)
    {
        bool changed = false;
        changed |= DestroyComponentsInScene<GameDataManager>(scene);
        changed |= DestroyComponentsInScene<SceneTransitionManager>(scene);
        changed |= DestroyComponentsInScene<PlayerInventoryController>(scene);
        return changed;
    }

    private static bool DestroyComponentsInScene<T>(Scene scene)
        where T : Component
    {
        bool changed = false;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                Object.DestroyImmediate(component.gameObject);
                changed = true;
            }
        }

        return changed;
    }

    private static bool IsStageScene(Scene scene)
    {
        return FindRoot(scene, "StageRoot") != null ||
               FindSceneComponent<SessionManager>(scene) != null;
    }

    private static bool MoveWorldBoundsUnderMap(Scene scene)
    {
        GameObject stageRoot = FindRoot(scene, "StageRoot");
        if (stageRoot == null)
        {
            stageRoot = new GameObject("StageRoot");
            SceneManager.MoveGameObjectToScene(stageRoot, scene);
        }

        Transform map = stageRoot.transform.Find("Map");
        if (map == null)
        {
            map = new GameObject("Map").transform;
            map.SetParent(stageRoot.transform, false);
        }

        WorldBounds2D bounds = FindSceneComponent<WorldBounds2D>(scene);
        if (bounds == null)
        {
            return true;
        }

        if (bounds.transform.parent == map)
        {
            return false;
        }

        bounds.transform.SetParent(map, true);
        return true;
    }

    private static bool NormalizeStageRuntime(Scene scene)
    {
        bool changed = false;
        GameObject stageRoot = FindRoot(scene, "StageRoot");
        if (stageRoot == null)
        {
            stageRoot = new GameObject("StageRoot");
            SceneManager.MoveGameObjectToScene(stageRoot, scene);
            changed = true;
        }

        Transform runtimeRoot = stageRoot.transform.Find("Runtime");
        if (runtimeRoot == null)
        {
            runtimeRoot = new GameObject("Runtime").transform;
            runtimeRoot.SetParent(stageRoot.transform, false);
            changed = true;
        }

        GameObject spaceShip = FindByName(scene, "SpaceShip");
        if (spaceShip == null)
        {
            Debug.LogError($"'{scene.path}' has no SpaceShip spawn point.");
            return changed;
        }

        if (spaceShip.transform.parent != stageRoot.transform)
        {
            spaceShip.transform.SetParent(stageRoot.transform, true);
            changed = true;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PlayerContext[] players = root.GetComponentsInChildren<PlayerContext>(true);
            foreach (PlayerContext player in players)
            {
                Object.DestroyImmediate(player.gameObject);
                changed = true;
            }
        }

        SessionManager manager = FindSceneComponent<SessionManager>(scene);
        GameObject playerPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (manager == null || playerPrefab == null)
        {
            Debug.LogError(
                $"Cannot configure player spawning in '{scene.path}': " +
                "SessionManager or SessionPlayer prefab is missing.");
            return changed;
        }

        SerializedObject serialized = new(manager);
        SerializedProperty prefabProperty = serialized.FindProperty("_playerPrefab");
        SerializedProperty spawnProperty = serialized.FindProperty("_playerSpawnPoint");
        SerializedProperty runtimeProperty = serialized.FindProperty("_runtimeRoot");
        if (prefabProperty.objectReferenceValue != playerPrefab ||
            spawnProperty.objectReferenceValue != spaceShip.transform ||
            runtimeProperty.objectReferenceValue != runtimeRoot)
        {
            prefabProperty.objectReferenceValue = playerPrefab;
            spawnProperty.objectReferenceValue = spaceShip.transform;
            runtimeProperty.objectReferenceValue = runtimeRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        return changed;
    }

    private static void RebuildTemplateAsset()
    {
        EnsureFolder(TemplateFolder);
        AssetDatabase.DeleteAsset(TemplateScenePath);
        if (!AssetDatabase.CopyAsset(ReferenceScenePath, TemplateScenePath))
        {
            Debug.LogError("Could not copy the reference scene for the stage template.");
            return;
        }

        AssetDatabase.ImportAsset(TemplateScenePath, ImportAssetOptions.ForceSynchronousImport);
        Scene template = EditorSceneManager.OpenScene(
            TemplateScenePath,
            OpenSceneMode.Single);
        RemovePersistentServices(template);
        MoveWorldBoundsUnderMap(template);
        NormalizeStageRuntime(template);

        GameObject stageRoot = FindRoot(template, "StageRoot");
        if (stageRoot != null)
        {
            StagePopulationManager population =
                stageRoot.GetComponent<StagePopulationManager>();
            if (population != null)
            {
                Object.DestroyImmediate(population);
            }

            Transform runtime = stageRoot.transform.Find("Runtime");
            if (runtime != null)
            {
                for (int i = runtime.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(runtime.GetChild(i).gameObject);
                }
            }

        }

        foreach (Tilemap tilemap in Object.FindObjectsByType<Tilemap>(
                     FindObjectsInactive.Include))
        {
            if (tilemap.gameObject.scene == template)
            {
                tilemap.ClearAllTiles();
                // ClearAllTiles does not reset the serialized origin/size. Without
                // compression, an empty template keeps the previous map's gizmo bounds.
                tilemap.CompressBounds();
            }
        }

        EditorSceneManager.MarkSceneDirty(template);
        EditorSceneManager.SaveScene(template);
    }

    private static T FindSceneComponent<T>(Scene scene)
        where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return root;
            }
        }

        return null;
    }

    private static GameObject FindByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindByName(root.transform, name);
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Transform FindByName(Transform root, string name)
    {
        if (string.Equals(
                root.name,
                name,
                System.StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindByName(root.GetChild(i), name);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
