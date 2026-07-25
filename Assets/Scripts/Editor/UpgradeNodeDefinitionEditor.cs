using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpgradeNodeDefinition))]
public sealed class UpgradeNodeDefinitionEditor : Editor
{
    private SerializedProperty _effects;

    private void OnEnable()
    {
        _effects = serializedObject.FindProperty("_effects");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "_effects");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_effects, true);

        if (GUILayout.Button("Add Numeric Effect"))
        {
            int index = _effects.arraySize;
            _effects.arraySize++;
            _effects.GetArrayElementAtIndex(index).managedReferenceValue =
                new NumericUpgradeEffect();
        }

        serializedObject.ApplyModifiedProperties();

        UpgradeNodeDefinition node = (UpgradeNodeDefinition)target;
        if (!node.TryValidate(out string error))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }
}
