using UnityEngine;

/// <summary>
/// Creates the services that must survive scene changes before the first scene loads.
/// Their prefabs intentionally live under Resources so their serialized setup remains
/// visible and inspectable without placing copies in every scene.
/// </summary>
public static class PersistentServicesBootstrap
{
    private const string PrefabRoot = "Prefabs/DontDestroyOnLoad/";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // GameDataManager must exist before PlayerInventoryController subscribes for data initialization.
        Ensure<GameDataManager>("GameDataManager");
        Ensure<SceneTransitionManager>("SceneTransitionManager");
        Ensure<PlayerInventoryController>("PlayerInventory");
        Ensure<WorktableService>("WorktableService");
        Ensure<TutorialGuideSystem>("TutorialGuideSystem");
    }

    private static void Ensure<T>(string prefabName)
        where T : Component
    {
        if (Object.FindAnyObjectByType<T>() != null)
        {
            return;
        }

        string resourcePath = PrefabRoot + prefabName;
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogError(
                $"Persistent service prefab is missing at Resources/{resourcePath}.prefab");
            return;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.name = prefab.name;
    }
}
