using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(PlayerMovementController))]
public sealed class PlayerMovementControllerEditor : Editor
{
    private static readonly Color _boundsColor = new(0.95f, 0.35f, 0.75f, 1f);

    private readonly BoxBoundsHandle _boundsHandle = new();
    private SerializedProperty _boundsMin;
    private SerializedProperty _boundsMax;

    private void OnEnable()
    {
        _boundsMin = serializedObject.FindProperty("_playerBoundsMin");
        _boundsMax = serializedObject.FindProperty("_playerBoundsMax");
        _boundsHandle.axes =
            PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        if (serializedObject.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();
        GetNormalizedBounds(out Vector2 min, out Vector2 max);

        PlayerMovementController controller =
            (PlayerMovementController)target;
        Vector3[] corners =
        {
            controller.transform.TransformPoint(new Vector3(min.x, min.y)),
            controller.transform.TransformPoint(new Vector3(min.x, max.y)),
            controller.transform.TransformPoint(new Vector3(max.x, max.y)),
            controller.transform.TransformPoint(new Vector3(max.x, min.y))
        };
        Handles.DrawSolidRectangleWithOutline(
            corners,
            new Color(_boundsColor.r, _boundsColor.g, _boundsColor.b, 0.1f),
            _boundsColor);

        Vector2 center = (min + max) * 0.5f;
        Vector2 size = max - min;
        _boundsHandle.center = new Vector3(center.x, center.y, 0f);
        _boundsHandle.size = new Vector3(size.x, size.y, 0f);
        _boundsHandle.SetColor(_boundsColor);

        Matrix4x4 previousMatrix = Handles.matrix;
        Handles.matrix = controller.transform.localToWorldMatrix;
        float moveHandleSize = HandleUtility.GetHandleSize(
            controller.transform.TransformPoint(_boundsHandle.center)) * 0.08f;

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
            "Player Bounds");

        if (!changed)
        {
            return;
        }

        Undo.RecordObject(target, "Edit Player Bounds");
        Vector2 newCenter = _boundsHandle.center;
        Vector2 newHalfSize = new(
            Mathf.Max(0.01f, Mathf.Abs(_boundsHandle.size.x)) * 0.5f,
            Mathf.Max(0.01f, Mathf.Abs(_boundsHandle.size.y)) * 0.5f);
        _boundsMin.vector2Value = newCenter - newHalfSize;
        _boundsMax.vector2Value = newCenter + newHalfSize;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void GetNormalizedBounds(out Vector2 min, out Vector2 max)
    {
        Vector2 first = _boundsMin.vector2Value;
        Vector2 second = _boundsMax.vector2Value;
        min = Vector2.Min(first, second);
        max = Vector2.Max(first, second);
    }
}
