using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(StagePopulationManager))]
public sealed class StagePopulationManagerEditor : Editor
{
    private static readonly Color CreatureColor =
        new(0.2f, 0.95f, 0.45f, 1f);
    private static readonly Color ResourceColor =
        new(1f, 0.72f, 0.18f, 1f);

    private readonly BoxBoundsHandle _boundsHandle = new();
    private SerializedProperty _creatureAreas;
    private SerializedProperty _resourceAreas;
    private StageSpawnCategory _selectedCategory;
    private int _selectedIndex = -1;

    private void OnEnable()
    {
        SerializedProperty collection =
            serializedObject.FindProperty("_spawnAreas");
        _creatureAreas = collection.FindPropertyRelative("_creatureAreas");
        _resourceAreas = collection.FindPropertyRelative("_resourceAreas");
        _boundsHandle.axes =
            PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "_spawnAreas");

        StagePopulationManager manager = (StagePopulationManager)target;
        if (manager.transform.rotation != Quaternion.identity ||
            manager.transform.lossyScale != Vector3.one)
        {
            EditorGUILayout.HelpBox(
                "Spawn areas use the manager's local XY coordinates. " +
                "Keep its rotation at zero and scale at one to preserve AABBs.",
                MessageType.Warning);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scene-owned Spawn Areas", EditorStyles.boldLabel);
        DrawAreaList(
            "Creature Areas",
            StageSpawnCategory.Creature,
            _creatureAreas,
            CreatureColor);
        DrawAreaList(
            "Resource Floatage Areas",
            StageSpawnCategory.ResourceFloatage,
            _resourceAreas,
            ResourceColor);

        if (serializedObject.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }
    }

    private void DrawAreaList(
        string title,
        StageSpawnCategory category,
        SerializedProperty areas,
        Color color)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        for (int i = 0; i < areas.arraySize; i++)
        {
            SerializedProperty area = areas.GetArrayElementAtIndex(i);
            SerializedProperty min = area.FindPropertyRelative("_min");
            SerializedProperty max = area.FindPropertyRelative("_max");
            bool selected =
                _selectedCategory == category && _selectedIndex == i;

            using (new EditorGUILayout.HorizontalScope())
            {
                Color previousColor = GUI.backgroundColor;
                GUI.backgroundColor = selected ? color : previousColor;
                if (GUILayout.Toggle(selected, $"[{i}]", "Button", GUILayout.Width(42f)))
                {
                    SelectArea(category, i);
                }
                GUI.backgroundColor = previousColor;

                EditorGUILayout.PropertyField(min, GUIContent.none);
                EditorGUILayout.LabelField("→", GUILayout.Width(14f));
                EditorGUILayout.PropertyField(max, GUIContent.none);

                if (GUILayout.Button("×", GUILayout.Width(24f)))
                {
                    areas.DeleteArrayElementAtIndex(i);
                    if (selected)
                    {
                        _selectedIndex = -1;
                    }
                    else if (_selectedCategory == category && _selectedIndex > i)
                    {
                        _selectedIndex--;
                    }
                    return;
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Area"))
            {
                int index = areas.arraySize;
                areas.InsertArrayElementAtIndex(index);
                SerializedProperty area = areas.GetArrayElementAtIndex(index);
                Vector2 center = GetDefaultLocalCenter();
                area.FindPropertyRelative("_min").vector2Value =
                    center - new Vector2(2f, 2f);
                area.FindPropertyRelative("_max").vector2Value =
                    center + new Vector2(2f, 2f);
                SelectArea(category, index);
            }

            using (new EditorGUI.DisabledScope(
                       _selectedCategory != category ||
                       _selectedIndex < 0 ||
                       _selectedIndex >= areas.arraySize))
            {
                if (GUILayout.Button("Duplicate Selected"))
                {
                    int sourceIndex = _selectedIndex;
                    int newIndex = areas.arraySize;
                    areas.InsertArrayElementAtIndex(newIndex);
                    SerializedProperty source =
                        areas.GetArrayElementAtIndex(sourceIndex);
                    SerializedProperty duplicate =
                        areas.GetArrayElementAtIndex(newIndex);
                    Vector2 offset = new(1f, -1f);
                    duplicate.FindPropertyRelative("_min").vector2Value =
                        source.FindPropertyRelative("_min").vector2Value + offset;
                    duplicate.FindPropertyRelative("_max").vector2Value =
                        source.FindPropertyRelative("_max").vector2Value + offset;
                    SelectArea(category, newIndex);
                }
            }
        }
    }

    private Vector2 GetDefaultLocalCenter()
    {
        StagePopulationManager manager = (StagePopulationManager)target;
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            return Vector2.zero;
        }

        Vector3 local = manager.transform.InverseTransformPoint(sceneView.pivot);
        return new Vector2(local.x, local.y);
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();
        DrawAreas(
            StageSpawnCategory.Creature,
            _creatureAreas,
            CreatureColor);
        DrawAreas(
            StageSpawnCategory.ResourceFloatage,
            _resourceAreas,
            ResourceColor);
        DrawSelectedHandle();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAreas(
        StageSpawnCategory category,
        SerializedProperty areas,
        Color color)
    {
        StagePopulationManager manager = (StagePopulationManager)target;
        for (int i = 0; i < areas.arraySize; i++)
        {
            GetNormalizedBounds(
                areas.GetArrayElementAtIndex(i),
                out Vector2 min,
                out Vector2 max);
            Vector3[] corners =
            {
                manager.transform.TransformPoint(new Vector3(min.x, min.y)),
                manager.transform.TransformPoint(new Vector3(min.x, max.y)),
                manager.transform.TransformPoint(new Vector3(max.x, max.y)),
                manager.transform.TransformPoint(new Vector3(max.x, min.y))
            };
            bool selected =
                _selectedCategory == category && _selectedIndex == i;
            Color fill = new(color.r, color.g, color.b, selected ? 0.2f : 0.08f);
            Color outline = new(color.r, color.g, color.b, selected ? 1f : 0.65f);
            Handles.DrawSolidRectangleWithOutline(corners, fill, outline);

            Vector3 center = (corners[0] + corners[2]) * 0.5f;
            float handleSize = HandleUtility.GetHandleSize(center) * 0.06f;
            if (Handles.Button(
                    center,
                    Quaternion.identity,
                    handleSize,
                    handleSize,
                    Handles.RectangleHandleCap))
            {
                SelectArea(category, i);
                Repaint();
            }

            Handles.Label(
                center + Vector3.up * handleSize * 1.5f,
                $"{GetCategoryLabel(category)} [{i}]");
        }
    }

    private void DrawSelectedHandle()
    {
        SerializedProperty areas = GetSelectedAreas();
        if (areas == null || _selectedIndex < 0 ||
            _selectedIndex >= areas.arraySize)
        {
            return;
        }

        SerializedProperty area = areas.GetArrayElementAtIndex(_selectedIndex);
        GetNormalizedBounds(area, out Vector2 min, out Vector2 max);
        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min;

        _boundsHandle.center = new Vector3(center.x, center.y, 0f);
        _boundsHandle.size = new Vector3(size.x, size.y, 0f);
        Color color = _selectedCategory == StageSpawnCategory.Creature
            ? CreatureColor
            : ResourceColor;
        _boundsHandle.SetColor(color);

        Matrix4x4 previousMatrix = Handles.matrix;
        StagePopulationManager manager = (StagePopulationManager)target;
        float moveHandleSize = HandleUtility.GetHandleSize(
            manager.transform.TransformPoint(_boundsHandle.center)) * 0.08f;
        Handles.matrix = manager.transform.localToWorldMatrix;
        EditorGUI.BeginChangeCheck();
        Vector3 movedCenter = Handles.FreeMoveHandle(
            _boundsHandle.center,
            moveHandleSize,
            Vector3.zero,
            Handles.RectangleHandleCap);
        _boundsHandle.center = new Vector3(
            movedCenter.x,
            movedCenter.y,
            0f);
        _boundsHandle.DrawHandle();
        bool changed = EditorGUI.EndChangeCheck();
        Handles.matrix = previousMatrix;

        if (!changed)
        {
            return;
        }

        Undo.RecordObject(target, "Edit Stage Spawn Area");
        Vector2 newCenter = _boundsHandle.center;
        Vector2 newHalfSize = new(
            Mathf.Max(0.01f, Mathf.Abs(_boundsHandle.size.x)) * 0.5f,
            Mathf.Max(0.01f, Mathf.Abs(_boundsHandle.size.y)) * 0.5f);
        area.FindPropertyRelative("_min").vector2Value =
            newCenter - newHalfSize;
        area.FindPropertyRelative("_max").vector2Value =
            newCenter + newHalfSize;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private SerializedProperty GetSelectedAreas()
    {
        return _selectedCategory == StageSpawnCategory.Creature
            ? _creatureAreas
            : _resourceAreas;
    }

    private void SelectArea(StageSpawnCategory category, int index)
    {
        _selectedCategory = category;
        _selectedIndex = index;
        SceneView.RepaintAll();
    }

    private static void GetNormalizedBounds(
        SerializedProperty area,
        out Vector2 min,
        out Vector2 max)
    {
        Vector2 first = area.FindPropertyRelative("_min").vector2Value;
        Vector2 second = area.FindPropertyRelative("_max").vector2Value;
        min = Vector2.Min(first, second);
        max = Vector2.Max(first, second);
    }

    private static string GetCategoryLabel(StageSpawnCategory category)
    {
        return category == StageSpawnCategory.Creature
            ? "Creature"
            : "Resource";
    }
}
