#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class MainMenuPlayModeStartScene
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";

    static MainMenuPlayModeStartScene()
    {
        EditorApplication.delayCall += Configure;
    }

    private static void Configure()
    {
        SceneAsset mainMenuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (mainMenuScene != null && EditorSceneManager.playModeStartScene != mainMenuScene)
        {
            EditorSceneManager.playModeStartScene = mainMenuScene;
        }
    }
}
#endif
