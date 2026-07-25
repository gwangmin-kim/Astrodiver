using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameDefinitionCatalog))]
public sealed class GameDefinitionCatalogEditor : Editor
{
    private const string CatalogPath = "Assets/Data/GameDefinitionCatalog.asset";
    private const string RefreshMenuPath =
        "Astrodiver/Data/Refresh Game Definition Catalog";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        if (GUILayout.Button("Refresh Definitions From Project"))
        {
            RefreshCatalog((GameDefinitionCatalog)target);
        }

        GameDefinitionCatalog catalog = (GameDefinitionCatalog)target;
        if (!catalog.TryValidate(out string error))
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }

    [MenuItem(RefreshMenuPath)]
    public static void RefreshDefaultCatalog()
    {
        GameDefinitionCatalog catalog =
            AssetDatabase.LoadAssetAtPath<GameDefinitionCatalog>(CatalogPath);

        if (catalog == null)
        {
            catalog = CreateInstance<GameDefinitionCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        RefreshCatalog(catalog);
        Selection.activeObject = catalog;
    }

    private static void RefreshCatalog(GameDefinitionCatalog catalog)
    {
        ResourceDefinition[] resources = FindAll<ResourceDefinition>();
        CreatureDefinition[] creatures = FindAll<CreatureDefinition>();
        UpgradeNodeDefinition[] upgrades = FindAll<UpgradeNodeDefinition>();

        Undo.RecordObject(catalog, "Refresh Game Definition Catalog");
        catalog.SetDefinitionsForEditor(resources, creatures, upgrades);
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        if (catalog.TryValidate(out string error))
        {
            Debug.Log(
                $"Refreshed '{catalog.name}': " +
                $"{resources.Length} resources, {creatures.Length} creatures, " +
                $"{upgrades.Length} upgrades.",
                catalog);
        }
        else
        {
            Debug.LogError($"Definition catalog refresh failed validation.\n{error}", catalog);
        }
    }

    private static T[] FindAll<T>() where T : ScriptableObject
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .OrderBy(asset => asset.name, StringComparer.Ordinal)
            .ToArray();
    }
}
