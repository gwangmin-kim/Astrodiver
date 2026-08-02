using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(WorldBounds2D))]
public sealed class WorldBounds2DEditor : Editor
{
    private static readonly Color BoundsColor = new(0.15f, 0.7f, 1f, 1f);

    private readonly BoxBoundsHandle _boundsHandle = new();
    private SerializedProperty _min;
    private SerializedProperty _max;

    private void OnEnable()
    {
        _min = serializedObject.FindProperty("_min");
        _max = serializedObject.FindProperty("_max");
        _boundsHandle.axes =
            PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");

        WorldBounds2D worldBounds = (WorldBounds2D)target;
        if (worldBounds.transform.rotation != Quaternion.identity ||
            worldBounds.transform.lossyScale != Vector3.one)
        {
            EditorGUILayout.HelpBox(
                "World bounds are axis-aligned. Keep this object's rotation at zero " +
                "and scale at one; move the object or edit the AABB instead.",
                MessageType.Warning);
        }

        EditorGUILayout.HelpBox(
            "The rectangular trigger collider is synchronized automatically and used by " +
            "Cinemachine Confiner2D as its bounding shape.",
            MessageType.Info);

        if (serializedObject.ApplyModifiedProperties())
        {
            _ = worldBounds.BoundaryCollider;
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();
        GetNormalizedBounds(out Vector2 min, out Vector2 max);

        WorldBounds2D worldBounds = (WorldBounds2D)target;
        Vector3[] corners =
        {
            worldBounds.transform.TransformPoint(new Vector3(min.x, min.y)),
            worldBounds.transform.TransformPoint(new Vector3(min.x, max.y)),
            worldBounds.transform.TransformPoint(new Vector3(max.x, max.y)),
            worldBounds.transform.TransformPoint(new Vector3(max.x, min.y))
        };
        Handles.DrawSolidRectangleWithOutline(
            corners,
            new Color(BoundsColor.r, BoundsColor.g, BoundsColor.b, 0.06f),
            BoundsColor);

        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min;
        _boundsHandle.center = new Vector3(center.x, center.y, 0f);
        _boundsHandle.size = new Vector3(size.x, size.y, 0f);
        _boundsHandle.SetColor(BoundsColor);

        Matrix4x4 previousMatrix = Handles.matrix;
        Handles.matrix = worldBounds.transform.localToWorldMatrix;
        float moveHandleSize = HandleUtility.GetHandleSize(
            worldBounds.transform.TransformPoint(_boundsHandle.center)) * 0.08f;

        EditorGUI.BeginChangeCheck();
        Vector3 movedCenter = Handles.FreeMoveHandle(
            _boundsHandle.center,
            moveHandleSize,
            Vector3.zero,
            Handles.RectangleHandleCap);
        _boundsHandle.center = new Vector3(movedCenter.x, movedCenter.y, 0f);
        _boundsHandle.DrawHandle();
        bool changed = EditorGUI.EndChangeCheck();
        Handles.matrix = previousMatrix;

        Handles.Label(
            (corners[0] + corners[2]) * 0.5f,
            "World Bounds");

        if (!changed)
        {
            return;
        }

        Undo.RecordObject(target, "Edit World Bounds");
        Vector2 newCenter = _boundsHandle.center;
        Vector2 newHalfSize = new(
            Mathf.Max(0.01f, Mathf.Abs(_boundsHandle.size.x)) * 0.5f,
            Mathf.Max(0.01f, Mathf.Abs(_boundsHandle.size.y)) * 0.5f);
        _min.vector2Value = newCenter - newHalfSize;
        _max.vector2Value = newCenter + newHalfSize;
        serializedObject.ApplyModifiedProperties();
        _ = worldBounds.BoundaryCollider;
        EditorUtility.SetDirty(target);
    }

    private void GetNormalizedBounds(out Vector2 min, out Vector2 max)
    {
        Vector2 first = _min.vector2Value;
        Vector2 second = _max.vector2Value;
        min = Vector2.Min(first, second);
        max = Vector2.Max(first, second);
    }
}
