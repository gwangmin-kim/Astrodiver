namespace Astrodiver.Editor.Localization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Localization;
    using UnityEngine.SceneManagement;

    public static class TextReferenceScanner
    {
        public sealed class Usage
        {
            public LocalizedString Reference;
            public string Location;
        }

        /// <summary>Read-only: includes disabled objects, prefab contents and Timeline subassets.</summary>
        public static List<Usage> Scan()
        {
            var result = new List<Usage>();
            foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    Scene scene = SceneManager.GetSceneByPath(path);
                    bool preview = !scene.IsValid() || !scene.isLoaded;
                    if (preview) scene = EditorSceneManager.OpenPreviewScene(path);
                    try
                    {
                        foreach (GameObject root in scene.GetRootGameObjects()) ScanHierarchy(root, path, result);
                    }
                    finally { if (preview) EditorSceneManager.ClosePreviewScene(scene); }
                }
                else if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (root != null) ScanHierarchy(root, path, result);
                }
                else if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                         path.EndsWith(".playable", StringComparison.OrdinalIgnoreCase) ||
                         path.EndsWith(".controller", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    {
                        // TMP glyph atlases and Unity's table internals contain no LocalizedString fields.
                        if (asset is ScriptableObject && asset.GetType().Namespace?.StartsWith("TMPro", StringComparison.Ordinal) != true &&
                            asset.GetType().Namespace?.StartsWith("UnityEngine.Localization.Tables", StringComparison.Ordinal) != true)
                            ScanObject(asset, path + " :: " + asset.name, result);
                    }
                }
            }
            return result;
        }

        private static void ScanHierarchy(GameObject root, string path, List<Usage> result)
        {
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) throw new InvalidOperationException($"Cannot validate text references: missing script in {path}.");
                string hierarchy = component.name;
                for (Transform parent = component.transform.parent; parent != null; parent = parent.parent)
                    hierarchy = parent.name + "/" + hierarchy;
                ScanObject(component, path + " :: " + hierarchy + " :: " + component.GetType().Name, result);
            }
        }

        private static void ScanObject(UnityEngine.Object target, string location, List<Usage> result)
        {
            using var serialized = new SerializedObject(target);
            var property = serialized.GetIterator();
            bool children = true;
            while (property.Next(children))
            {
                children = true;
                if (property.propertyType != SerializedPropertyType.Generic || property.type != nameof(LocalizedString)) continue;
                if (property.boxedValue is LocalizedString reference && !reference.IsEmpty)
                    result.Add(new Usage { Reference = reference, Location = location + " :: " + property.propertyPath });
                children = false;
            }
        }
    }
}
