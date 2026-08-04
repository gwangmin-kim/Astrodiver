using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public sealed class StageMapEditorWindow : EditorWindow
{
    private enum PlacementOperation
    {
        Paint = 0,
        Erase = 1
    }

    private const float InactiveLayerAlphaMultiplier = 0.25f;
    private static readonly Color _stageBoundsColor =
        new(0.15f, 0.7f, 1f, 1f);

    private StageMapLayer _selectedLayer = StageMapLayer.Platform;
    private bool _placementMode;
    private Vector3Int _lastEditedCell = new(int.MinValue, int.MinValue, 0);
    private int _undoGroup = -1;
    private int _strokeButton = -1;
    private PlacementOperation _strokeOperation = PlacementOperation.Paint;
    private Tilemap _strokeTilemap;
    private Tool _previousTool;
    private bool _toolCaptured;

    private StageMap _visualStageMap;
    private Color _platformOriginalColor;
    private Color _decorationBackOriginalColor;
    private Color _decorationFrontOriginalColor;
    private bool _originalColorsCaptured;
    private WorldBounds2D _stageBounds;
    private StageMapPlacementShortcutContext _shortcutContext;

    internal bool IsPlacementShortcutContextActive =>
        _placementMode && !EditorApplication.isPlaying;

    [MenuItem("Astrodiver/Stage Map/Open Editor")]
    public static void Open()
    {
        GetWindow<StageMapEditorWindow>("Stage Map");
    }

    private void OnEnable()
    {
        _shortcutContext = new StageMapPlacementShortcutContext(this);
        ShortcutManager.RegisterContext(_shortcutContext);
        SceneView.beforeSceneGui += OnBeforeSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    private void OnDisable()
    {
        SetPlacementMode(false);
        if (_shortcutContext != null)
        {
            ShortcutManager.UnregisterContext(_shortcutContext);
            _shortcutContext = null;
        }
        SceneView.beforeSceneGui -= OnBeforeSceneGUI;
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorSceneManager.sceneSaving -= OnSceneSaving;
    }

    private void OnGUI()
    {
        StageMap stageMap = StageMapSetupUtility.FindCurrentStageMap();

        EditorGUILayout.LabelField(
            "Logical Tilemap Placement",
            EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        if (stageMap == null)
        {
            if (_placementMode)
            {
                SetPlacementMode(false);
            }

            EditorGUILayout.HelpBox(
                "The active scene has no StageMap. Set up the current stage first.",
                MessageType.Warning);
            if (GUILayout.Button("Setup Current Stage"))
            {
                StageMapSetupUtility.SetupCurrentStage();
            }
            return;
        }

        if (!stageMap.TryValidate(out string validationError))
        {
            EditorGUILayout.HelpBox(validationError, MessageType.Error);
            if (GUILayout.Button("Repair Stage Map"))
            {
                StageMapSetupUtility.SetupCurrentStage();
            }
            return;
        }

        EditorGUI.BeginChangeCheck();
        StageMapLayer nextLayer = (StageMapLayer)EditorGUILayout.EnumPopup(
            "Tilemap",
            _selectedLayer);
        if (EditorGUI.EndChangeCheck())
        {
            SelectLayer(stageMap, nextLayer);
        }

        EditorGUILayout.Space(6f);
        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = _placementMode
            ? new Color(1f, 0.65f, 0.35f)
            : Color.white;
        if (GUILayout.Button(
                _placementMode ? "Stop Placement Mode" : "Start Placement Mode",
                GUILayout.Height(30f)))
        {
            SetPlacementMode(!_placementMode);
        }
        GUI.backgroundColor = previousColor;

        EditorGUILayout.HelpBox(
            "Left-click/drag: Paint  |  Right-click/drag: Erase\n" +
            "`: Next Tilemap  |  1/2/3: Select Tilemap\n" +
            "Escape: Stop Placement\n" +
            "Hold Alt to use normal Scene View navigation.",
            MessageType.Info);

        Tilemap selected = stageMap.GetTilemap(_selectedLayer);
        EditorGUILayout.LabelField("Selected Object", selected.name);
        EditorGUILayout.LabelField(
            "Occupied Cells",
            CountOccupiedCells(selected).ToString());
    }

    private void OnBeforeSceneGUI(SceneView sceneView)
    {
        if (!_placementMode || EditorApplication.isPlaying)
        {
            return;
        }

        Event current = Event.current;
        if (current == null || current.type != EventType.KeyDown)
        {
            return;
        }

        if (current.keyCode == KeyCode.Escape)
        {
            SetPlacementMode(false);
            current.Use();
            return;
        }

        if (current.keyCode == KeyCode.BackQuote)
        {
            StageMap stageMap = StageMapSetupUtility.FindCurrentStageMap();
            if (stageMap != null)
            {
                CycleLayer(stageMap);
            }

            current.Use();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_placementMode || EditorApplication.isPlaying)
        {
            return;
        }

        StageMap stageMap = StageMapSetupUtility.FindCurrentStageMap();
        if (stageMap == null)
        {
            SetPlacementMode(false);
            return;
        }

        EnsureVisualStageMap(stageMap);
        stageMap.EnforceTransformLock();
        Tools.current = Tool.None;

        Tilemap tilemap = stageMap.GetTilemap(_selectedLayer);
        if (tilemap == null)
        {
            SetPlacementMode(false);
            return;
        }

        WorldBounds2D stageBounds = FindStageBounds(
            stageMap.gameObject.scene);
        if (stageBounds != null)
        {
            DrawStageBounds(stageBounds);
        }

        Event current = Event.current;

        if (current.type == EventType.Layout && !current.alt)
        {
            HandleUtility.AddDefaultControl(
                GUIUtility.GetControlID(FocusType.Passive));
        }

        if (!TryGetCell(tilemap, current.mousePosition, out Vector3Int cell))
        {
            return;
        }

        DrawCellPreview(tilemap, cell);
        sceneView.Repaint();

        bool supportedButton = current.button == 0 || current.button == 1;
        bool beginsStroke = current.type == EventType.MouseDown &&
                            supportedButton && !current.alt;
        bool continuesStroke = current.type == EventType.MouseDrag &&
                               current.button == _strokeButton &&
                               !current.alt;
        bool endsStroke = current.type == EventType.MouseUp &&
                          current.button == _strokeButton;

        if (beginsStroke)
        {
            PlacementOperation operation = current.button == 0
                ? PlacementOperation.Paint
                : PlacementOperation.Erase;
            BeginStroke(tilemap, operation, current.button);
            EditCell(tilemap, cell);
            current.Use();
        }
        else if (continuesStroke && cell != _lastEditedCell)
        {
            EditCell(tilemap, cell);
            current.Use();
        }
        else if (endsStroke)
        {
            FinishStroke();
            current.Use();
        }
    }

    private void SetPlacementMode(bool enabled)
    {
        if (_placementMode == enabled)
        {
            return;
        }

        FinishStroke();
        _placementMode = enabled;
        _lastEditedCell = new Vector3Int(int.MinValue, int.MinValue, 0);

        StageMap stageMap = StageMapSetupUtility.FindCurrentStageMap();
        if (enabled && stageMap != null)
        {
            _previousTool = Tools.current;
            _toolCaptured = true;
            Tools.current = Tool.None;
            Selection.activeObject = stageMap;
            EnsureVisualStageMap(stageMap);
            ApplyLayerVisibility();
        }
        else
        {
            RestoreLayerVisibility(clearCapture: true);
            _stageBounds = null;
            if (_toolCaptured && Tools.current == Tool.None)
            {
                Tools.current = _previousTool;
            }
            _toolCaptured = false;
        }

        SceneView.RepaintAll();
        Repaint();
    }

    private void SelectLayer(StageMap stageMap, StageMapLayer layer)
    {
        if (_selectedLayer == layer)
        {
            return;
        }

        FinishStroke();
        _selectedLayer = layer;
        if (_placementMode)
        {
            EnsureVisualStageMap(stageMap);
            ApplyLayerVisibility();
            Selection.activeObject = stageMap;
        }

        SceneView.RepaintAll();
        Repaint();
    }

    private void CycleLayer(StageMap stageMap)
    {
        int layerCount = System.Enum.GetValues(typeof(StageMapLayer)).Length;
        StageMapLayer next =
            (StageMapLayer)(((int)_selectedLayer + 1) % layerCount);
        SelectLayer(stageMap, next);
    }

    [Shortcut(
        "Astrodiver/Stage Map/Select Tilemap 1",
        typeof(StageMapPlacementShortcutContext),
        KeyCode.Alpha1)]
    private static void SelectLayerOneShortcut(ShortcutArguments arguments)
    {
        SelectLayerShortcut(arguments, 0);
    }

    [Shortcut(
        "Astrodiver/Stage Map/Select Tilemap 2",
        typeof(StageMapPlacementShortcutContext),
        KeyCode.Alpha2)]
    private static void SelectLayerTwoShortcut(ShortcutArguments arguments)
    {
        SelectLayerShortcut(arguments, 1);
    }

    [Shortcut(
        "Astrodiver/Stage Map/Select Tilemap 3",
        typeof(StageMapPlacementShortcutContext),
        KeyCode.Alpha3)]
    private static void SelectLayerThreeShortcut(ShortcutArguments arguments)
    {
        SelectLayerShortcut(arguments, 2);
    }

    [Shortcut(
        "Astrodiver/Stage Map/Select Tilemap Numpad 1",
        typeof(StageMapPlacementShortcutContext),
        KeyCode.Keypad1)]
    private static void SelectLayerNumpadOneShortcut(
        ShortcutArguments arguments)
    {
        SelectLayerShortcut(arguments, 0);
    }

    [Shortcut(
        "Astrodiver/Stage Map/Select Tilemap Numpad 2",
        typeof(StageMapPlacementShortcutContext),
        KeyCode.Keypad2)]
    private static void SelectLayerNumpadTwoShortcut(
        ShortcutArguments arguments)
    {
        SelectLayerShortcut(arguments, 1);
    }

    [Shortcut(
        "Astrodiver/Stage Map/Select Tilemap Numpad 3",
        typeof(StageMapPlacementShortcutContext),
        KeyCode.Keypad3)]
    private static void SelectLayerNumpadThreeShortcut(
        ShortcutArguments arguments)
    {
        SelectLayerShortcut(arguments, 2);
    }

    private static void SelectLayerShortcut(
        ShortcutArguments arguments,
        int index)
    {
        StageMapEditorWindow window = GetShortcutWindow(arguments);
        StageMap stageMap = StageMapSetupUtility.FindCurrentStageMap();
        System.Array values = System.Enum.GetValues(typeof(StageMapLayer));
        if (window == null || stageMap == null || index < 0 ||
            index >= values.Length)
        {
            return;
        }

        window.SelectLayer(stageMap, (StageMapLayer)values.GetValue(index));
    }

    private static StageMapEditorWindow GetShortcutWindow(
        ShortcutArguments arguments)
    {
        StageMapPlacementShortcutContext context =
            arguments.context as StageMapPlacementShortcutContext;
        return context?.Window;
    }

    private void BeginStroke(
        Tilemap tilemap,
        PlacementOperation operation,
        int mouseButton)
    {
        FinishStroke();
        _strokeTilemap = tilemap;
        _strokeOperation = operation;
        _strokeButton = mouseButton;
        Undo.IncrementCurrentGroup();
        _undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(
            operation == PlacementOperation.Paint
                ? "Paint Stage Map Tiles"
                : "Erase Stage Map Tiles");
        _lastEditedCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    }

    private void FinishStroke()
    {
        if (_strokeOperation == PlacementOperation.Erase &&
            _strokeTilemap != null)
        {
            Undo.RegisterCompleteObjectUndo(
                _strokeTilemap,
                "Compress Stage Map Bounds");
            _strokeTilemap.CompressBounds();
            EditorUtility.SetDirty(_strokeTilemap);
            EditorSceneManager.MarkSceneDirty(_strokeTilemap.gameObject.scene);
        }

        if (_undoGroup >= 0)
        {
            Undo.CollapseUndoOperations(_undoGroup);
            _undoGroup = -1;
        }

        _strokeButton = -1;
        _strokeOperation = PlacementOperation.Paint;
        _strokeTilemap = null;
        _lastEditedCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    }

    private void EditCell(Tilemap tilemap, Vector3Int cell)
    {
        TileBase nextTile = _strokeOperation == PlacementOperation.Paint
            ? StageMapDefaultTiles.Get(_selectedLayer)
            : null;
        TileBase currentTile = tilemap.GetTile(cell);
        if (currentTile == nextTile ||
            (_strokeOperation == PlacementOperation.Paint &&
             currentTile != null))
        {
            _lastEditedCell = cell;
            return;
        }

        Undo.RegisterCompleteObjectUndo(tilemap, Undo.GetCurrentGroupName());
        tilemap.SetTile(cell, nextTile);
        tilemap.RefreshTile(cell);
        EditorUtility.SetDirty(tilemap);
        EditorSceneManager.MarkSceneDirty(tilemap.gameObject.scene);
        _lastEditedCell = cell;
        Repaint();
    }

    private void EnsureVisualStageMap(StageMap stageMap)
    {
        if (_visualStageMap == stageMap && _originalColorsCaptured)
        {
            return;
        }

        RestoreLayerVisibility(clearCapture: true);
        _visualStageMap = stageMap;
        _platformOriginalColor = stageMap.Platform.color;
        _decorationBackOriginalColor = stageMap.DecorationBack.color;
        _decorationFrontOriginalColor = stageMap.DecorationFront.color;
        _originalColorsCaptured = true;
    }

    private void ApplyLayerVisibility()
    {
        if (!_originalColorsCaptured || _visualStageMap == null)
        {
            return;
        }

        _visualStageMap.Platform.color = GetPlacementColor(
            StageMapLayer.Platform,
            _platformOriginalColor);
        _visualStageMap.DecorationBack.color = GetPlacementColor(
            StageMapLayer.DecorationBack,
            _decorationBackOriginalColor);
        _visualStageMap.DecorationFront.color = GetPlacementColor(
            StageMapLayer.DecorationFront,
            _decorationFrontOriginalColor);
    }

    private Color GetPlacementColor(StageMapLayer layer, Color original)
    {
        if (layer != _selectedLayer)
        {
            original.a *= InactiveLayerAlphaMultiplier;
        }

        return original;
    }

    private void RestoreLayerVisibility(bool clearCapture)
    {
        if (_originalColorsCaptured && _visualStageMap != null)
        {
            if (_visualStageMap.Platform != null)
            {
                _visualStageMap.Platform.color = _platformOriginalColor;
            }
            if (_visualStageMap.DecorationBack != null)
            {
                _visualStageMap.DecorationBack.color =
                    _decorationBackOriginalColor;
            }
            if (_visualStageMap.DecorationFront != null)
            {
                _visualStageMap.DecorationFront.color =
                    _decorationFrontOriginalColor;
            }
        }

        if (clearCapture)
        {
            _visualStageMap = null;
            _originalColorsCaptured = false;
        }
    }

    private void OnSceneSaving(Scene scene, string path)
    {
        if (!_placementMode || !_originalColorsCaptured ||
            _visualStageMap == null || _visualStageMap.gameObject.scene != scene)
        {
            return;
        }

        RestoreLayerVisibility(clearCapture: false);
        EditorApplication.delayCall += ReapplyLayerVisibilityAfterSave;
    }

    private void ReapplyLayerVisibilityAfterSave()
    {
        if (_placementMode)
        {
            ApplyLayerVisibility();
            SceneView.RepaintAll();
        }
    }

    private static bool TryGetCell(
        Tilemap tilemap,
        Vector2 guiPosition,
        out Vector3Int cell)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);
        Plane plane = new(
            tilemap.transform.forward,
            tilemap.transform.position);
        if (!plane.Raycast(ray, out float distance))
        {
            cell = default;
            return false;
        }

        cell = tilemap.WorldToCell(ray.GetPoint(distance));
        cell.z = 0;
        return true;
    }

    private void DrawCellPreview(Tilemap tilemap, Vector3Int cell)
    {
        Vector3 bottomLeft = tilemap.CellToWorld(cell);
        Vector3 topLeft = tilemap.CellToWorld(cell + Vector3Int.up);
        Vector3 topRight = tilemap.CellToWorld(cell + Vector3Int.one);
        Vector3 bottomRight = tilemap.CellToWorld(cell + Vector3Int.right);
        Color color = _strokeOperation == PlacementOperation.Erase
            ? new Color(1f, 0.2f, 0.2f, 1f)
            : GetLayerColor(_selectedLayer);

        Handles.DrawSolidRectangleWithOutline(
            new[] { bottomLeft, topLeft, topRight, bottomRight },
            new Color(color.r, color.g, color.b, 0.22f),
            color);
        Handles.Label(
            (bottomLeft + topRight) * 0.5f,
            $"{_selectedLayer}  {cell.x}, {cell.y}");
    }

    private WorldBounds2D FindStageBounds(Scene scene)
    {
        if (_stageBounds != null && _stageBounds.gameObject.scene == scene)
        {
            return _stageBounds;
        }

        WorldBounds2D[] candidates =
            Object.FindObjectsByType<WorldBounds2D>(
                FindObjectsInactive.Include);
        foreach (WorldBounds2D candidate in candidates)
        {
            if (candidate.gameObject.scene == scene)
            {
                _stageBounds = candidate;
                return _stageBounds;
            }
        }

        _stageBounds = null;
        return null;
    }

    private static void DrawStageBounds(WorldBounds2D stageBounds)
    {
        Vector2 min = stageBounds.WorldMin;
        Vector2 max = stageBounds.WorldMax;
        float z = stageBounds.transform.position.z;
        Vector3[] corners =
        {
            new(min.x, min.y, z),
            new(min.x, max.y, z),
            new(max.x, max.y, z),
            new(max.x, min.y, z)
        };
        Vector3[] outline =
        {
            corners[0],
            corners[1],
            corners[2],
            corners[3],
            corners[0]
        };

        Color previousHandlesColor = Handles.color;
        Handles.color = _stageBoundsColor;
        Handles.DrawAAPolyLine(3f, outline);
        Handles.Label(
            corners[1],
            $"Stage Bounds  {max.x - min.x:0.##} x " +
            $"{max.y - min.y:0.##}");
        Handles.color = previousHandlesColor;
    }

    private static Color GetLayerColor(StageMapLayer layer)
    {
        return layer switch
        {
            StageMapLayer.Platform => Color.white,
            StageMapLayer.DecorationBack => new Color(0.2f, 0.2f, 0.2f),
            StageMapLayer.DecorationFront => new Color(0.7f, 0.7f, 0.7f),
            _ => Color.magenta
        };
    }

    private static int CountOccupiedCells(Tilemap tilemap)
    {
        int count = 0;
        foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(position))
            {
                count++;
            }
        }

        return count;
    }
}

internal sealed class StageMapPlacementShortcutContext : IShortcutContext
{
    internal StageMapPlacementShortcutContext(StageMapEditorWindow window)
    {
        Window = window;
    }

    internal StageMapEditorWindow Window { get; }

    public bool active =>
        Window != null && Window.IsPlacementShortcutContextActive;
}
