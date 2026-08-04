using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public sealed class BuildSceneReference
{
    [SerializeField, HideInInspector] private int _buildIndex = -1;

#if UNITY_EDITOR
    [SerializeField, InspectorName("Scene")]
    private SceneAsset _sceneAsset;
#endif

    public int BuildIndex => _buildIndex;

    public bool CanLoad()
    {
        return _buildIndex >= 0 &&
            Application.CanStreamedLevelBeLoaded(_buildIndex);
    }

#if UNITY_EDITOR
    public void RefreshBuildIndex()
    {
        _buildIndex = FindBuildIndex(_sceneAsset);
    }

    private static int FindBuildIndex(SceneAsset sceneAsset)
    {
        if (sceneAsset == null)
        {
            return -1;
        }

        string assetPath = AssetDatabase.GetAssetPath(sceneAsset);
        string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

        int buildIndex = 0;
        for (int i = 0; i < buildScenes.Length; i++)
        {
            EditorBuildSettingsScene buildScene = buildScenes[i];
            if (!buildScene.enabled)
            {
                continue;
            }

            if (string.Equals(
                    buildScene.guid.ToString(),
                    assetGuid,
                    StringComparison.OrdinalIgnoreCase))
            {
                return buildIndex;
            }

            buildIndex++;
        }

        return -1;
    }
#endif
}
