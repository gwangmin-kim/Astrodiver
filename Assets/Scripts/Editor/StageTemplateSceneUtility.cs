using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StageTemplateSceneUtility
{
    private const string TemplateScenePath = "Assets/Scenes/Templates/EmptyStageTemplate.unity";

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
            "Restore or create Assets/Scenes/Templates/EmptyStageTemplate.unity first.",
            "OK");
        return false;
    }

}
