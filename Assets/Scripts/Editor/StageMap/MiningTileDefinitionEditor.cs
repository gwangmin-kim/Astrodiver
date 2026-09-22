using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(MiningTileDefinition))]
public sealed class MiningTileDefinitionEditor : AutoTileEditor
{
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = base.CreateInspectorGUI();
        Foldout miningProperties = new()
        {
            text = "Mining Properties",
            value = true
        };

        foreach (FieldInfo field in typeof(MiningTileDefinition)
                     .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(IsUnitySerializedField)
                     .OrderBy(field => field.MetadataToken))
        {
            SerializedProperty property = serializedObject.FindProperty(field.Name);
            if (property != null)
                miningProperties.Add(new PropertyField(property));
        }

        root.Add(miningProperties);
        return root;
    }

    private static bool IsUnitySerializedField(FieldInfo field)
    {
        if (field.IsStatic || field.IsNotSerialized || field.IsDefined(typeof(HideInInspector), false))
            return false;

        return field.IsPublic || field.IsDefined(typeof(SerializeField), false) ||
            field.IsDefined(typeof(SerializeReference), false);
    }
}
