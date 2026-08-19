using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public sealed class StageMapEditorWindow : EditorWindow
{
    private enum StageMapToolMode
    {
        Placement = 0,
        AutoTexture = 1
    }

    private enum PlacementEditMode
    {
        Draw = 0,
        Fill = 1
    }

    private enum PlacementOperation
    {
        Paint = 0,
        Erase = 1
    }

    private const float InactiveLayerAlphaMultiplier = 0.25f;
    private const float TileSetPickerHeight = 220f;
    private const float TileSetCardWidth = 104f;
    private const float TileSetCardHeight = 116f;
    private const float TileSetCardSpacing = 4f;
    // Keep one tile available outside each WorldBounds2D edge so automatic
    // tile textures can render their connected border without being clipped.
    private const int EditableBorderCellCount = 1;
    private static readonly Color _stageBoundsColor =
        new(0.15f, 0.7f, 1f, 1f);
    private static readonly Vector3Int[] _cardinalDirections =
    {
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.down,
        Vector3Int.up
    };
    private static readonly StageMapLayer[] _layers =
        (StageMapLayer[])System.Enum.GetValues(typeof(StageMapLayer));

    [SerializeField] private StageMapLayer _selectedLayer =
        StageMapLayer.Platform;
    [SerializeField] private StageMapToolMode _toolMode =
        StageMapToolMode.Placement;
    [SerializeField] private PlacementEditMode _editMode =
        PlacementEditMode.Draw;
    [SerializeField] private StageTileSet _selectedTileSet;
    [SerializeField] private StageTileSet[] _lastTileSetByLayer =
        new StageTileSet[3];
    [SerializeField] private string _tileSetSearch = string.Empty;
    [SerializeField] private Vector2 _tileSetScroll;
    private readonly List<StageTileSet> _tileSetCache = new();
    private readonly List<StageTileSet> _visibleTileSets = new();
    private bool _tileSetCacheDirty = true;
    private bool _tileSetPreviewRepaintQueued;
    private GUIStyle _tileSetNameStyle;
    private bool _placementMode;
    private Vector3Int _lastEditedCell = new(int.MinValue, int.MinValue, 0);
    private int _undoGroup = -1;
    private int _strokeButton = -1;
    private PlacementOperation _strokeOperation = PlacementOperation.Paint;
    private Tilemap _strokeTilemap;
    private Tilemap _strokeVisualTilemap;
    private Tool _previousTool;
    private bool _toolCaptured;

    private StageMap _visualStageMap;
    private Color _platformOriginalColor;
    private Color _decorationBackOriginalColor;
    private Color _decorationFrontOriginalColor;
    private bool _originalColorsCaptured;
    private WorldBounds2D _stageBounds;

    [MenuItem("Astrodiver/Stage Map/Open Editor")]
    public static void Open()
    {
        GetWindow<StageMapEditorWindow>("Stage Map");
    }

    private void OnEnable()
    {
        SceneView.beforeSceneGui += OnBeforeSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.projectChanged += MarkTileSetCacheDirty;
        RefreshTileSetCache();
    }

    private void OnDisable()
    {
        SetPlacementMode(false);
        SceneView.beforeSceneGui -= OnBeforeSceneGUI;
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.projectChanged -= MarkTileSetCacheDirty;
        EditorApplication.delayCall -= RepaintTileSetPicker;
    }

    private void OnGUI()
    {
        StageMap stageMap = StageMapSetupUtility.FindCurrentStageMap();

        EditorGUILayout.LabelField(
            "Stage Map Editing",
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

        EditorGUI.BeginChangeCheck();
        StageMapToolMode nextToolMode =
            (StageMapToolMode)EditorGUILayout.EnumPopup(
                "Tool Mode",
                _toolMode);
        if (EditorGUI.EndChangeCheck())
        {
            SetToolMode(nextToolMode);
        }

        if (_toolMode == StageMapToolMode.Placement)
        {
            EditorGUI.BeginChangeCheck();
            PlacementEditMode nextEditMode =
                (PlacementEditMode)EditorGUILayout.EnumPopup(
                    "Editing Mode",
                    _editMode);
            if (EditorGUI.EndChangeCheck())
            {
                SetEditMode(nextEditMode);
            }
        }
        else
        {
            DrawAutoTextureTileSetPicker();

            if (_selectedTileSet == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a tile set to use Auto Texture Paint.",
                    MessageType.Warning);
            }
            else if (!_selectedTileSet.TryValidate(
                         _selectedLayer,
                         out string tileSetError))
            {
                EditorGUILayout.HelpBox(tileSetError, MessageType.Error);
            }
        }

        EditorGUILayout.Space(6f);
        Color previousColor = GUI.backgroundColor;
        GUI.backgroundColor = _placementMode
            ? new Color(1f, 0.65f, 0.35f)
            : Color.white;
        using (new EditorGUI.DisabledScope(
                   EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button(
                    _placementMode
                        ? "Stop Editing"
                        : $"Start {_toolMode}",
                    GUILayout.Height(30f)))
            {
                SetPlacementMode(!_placementMode);
            }
        }
        GUI.backgroundColor = previousColor;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox(
                "Placement Mode is unavailable during Play Mode.",
                MessageType.Info);
        }

        EditorGUILayout.HelpBox(
            GetModeHelpText(),
            MessageType.Info);

        if (_toolMode == StageMapToolMode.Placement &&
            _editMode == PlacementEditMode.Fill &&
            FindStageBounds(stageMap.gameObject.scene) == null)
        {
            EditorGUILayout.HelpBox(
                "Fill Mode requires a WorldBounds2D in the active scene.",
                MessageType.Warning);
        }

        Tilemap selected = stageMap.GetLogicalTilemap(_selectedLayer);
        EditorGUILayout.LabelField("Logic Tilemap", selected.name);
        EditorGUILayout.LabelField(
            "Visual Tilemap",
            stageMap.GetVisualTilemap(_selectedLayer).name);
        EditorGUILayout.LabelField(
            "Occupied Cells",
            CountOccupiedCells(selected).ToString());

        EditorGUILayout.Space(10f);
        Color clearButtonColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
        using (new EditorGUI.DisabledScope(
                   EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("Clear All Tilemaps"))
            {
                FinishStroke();
                ClearAllTilemaps(stageMap);
            }
        }
        GUI.backgroundColor = clearButtonColor;
    }

    private void DrawAutoTextureTileSetPicker()
    {
        RefreshTileSetCache();

        EditorGUILayout.LabelField(
            $"Tile Sets — {_selectedLayer}",
            EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        string nextSearch = EditorGUILayout.TextField(
            "Search",
            _tileSetSearch);
        if (EditorGUI.EndChangeCheck())
        {
            _tileSetSearch = nextSearch;
            _tileSetScroll = Vector2.zero;
        }

        if (GUILayout.Button("Refresh", GUILayout.Width(64f)))
        {
            _tileSetCacheDirty = true;
            RefreshTileSetCache();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        StageTileSet directSelection =
            (StageTileSet)EditorGUILayout.ObjectField(
                "Selected",
                _selectedTileSet,
                typeof(StageTileSet),
                false);
        if (EditorGUI.EndChangeCheck())
        {
            SelectTileSet(directSelection);
        }

        if (_selectedTileSet != null &&
            GUILayout.Button("Ping Selected", GUILayout.Height(18f)))
        {
            EditorGUIUtility.PingObject(_selectedTileSet);
            Selection.activeObject = _selectedTileSet;
        }

        CollectVisibleTileSets();
        if (_visibleTileSets.Count == 0)
        {
            EditorGUILayout.HelpBox(
                string.IsNullOrWhiteSpace(_tileSetSearch)
                    ? $"No tile sets support {_selectedLayer}."
                    : "No matching tile sets were found.",
                MessageType.Info);
            return;
        }

        _tileSetScroll = EditorGUILayout.BeginScrollView(
            _tileSetScroll,
            GUILayout.Height(TileSetPickerHeight));
        int columns = Mathf.Max(
            1,
            Mathf.FloorToInt(
                (position.width - 26f) /
                (TileSetCardWidth + TileSetCardSpacing)));
        for (int start = 0;
             start < _visibleTileSets.Count;
             start += columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int column = 0; column < columns; column++)
            {
                int index = start + column;
                if (index < _visibleTileSets.Count)
                {
                    DrawTileSetCard(_visibleTileSets[index]);
                }
                else
                {
                    GUILayout.Space(TileSetCardWidth);
                }

                if (column < columns - 1)
                {
                    GUILayout.Space(TileSetCardSpacing);
                }
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(TileSetCardSpacing);
        }
        EditorGUILayout.EndScrollView();

        if (AssetPreview.IsLoadingAssetPreviews() &&
            !_tileSetPreviewRepaintQueued)
        {
            _tileSetPreviewRepaintQueued = true;
            EditorApplication.delayCall += RepaintTileSetPicker;
        }
    }

    private void DrawTileSetCard(StageTileSet tileSet)
    {
        Rect cardRect = GUILayoutUtility.GetRect(
            TileSetCardWidth,
            TileSetCardHeight,
            GUILayout.Width(TileSetCardWidth),
            GUILayout.Height(TileSetCardHeight));
        bool isValid = tileSet.TryValidate(_selectedLayer, out string error);
        bool isSelected = tileSet == _selectedTileSet;
        Color background = isSelected
            ? new Color(0.16f, 0.43f, 0.78f, 0.8f)
            : new Color(0f, 0f, 0f, EditorGUIUtility.isProSkin ? 0.24f : 0.1f);
        if (!isValid)
        {
            background.a *= 0.55f;
        }

        EditorGUI.DrawRect(cardRect, background);
        DrawTileSetCardOutline(
            cardRect,
            isSelected
                ? new Color(0.45f, 0.8f, 1f, 1f)
                : new Color(0f, 0f, 0f, 0.35f));

        GUIContent tooltip = new(
            string.Empty,
            GetTileSetTooltip(tileSet, isValid ? null : error));
        using (new EditorGUI.DisabledScope(!isValid))
        {
            if (GUI.Button(cardRect, tooltip, GUIStyle.none))
            {
                SelectTileSet(tileSet);
                if (Event.current.clickCount >= 2)
                {
                    EditorGUIUtility.PingObject(tileSet);
                    Selection.activeObject = tileSet;
                }
            }
        }

        Rect iconRect = new(
            cardRect.x + 6f,
            cardRect.y + 6f,
            cardRect.width - 12f,
            72f);
        Texture preview = GetTileSetPreview(tileSet);
        if (preview != null)
        {
            Color previousColor = GUI.color;
            if (!isValid)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f);
            }
            GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;
        }
        else
        {
            GUI.Label(iconRect, "No Preview", EditorStyles.centeredGreyMiniLabel);
        }

        Rect nameRect = new(
            cardRect.x + 4f,
            cardRect.y + 82f,
            cardRect.width - 8f,
            cardRect.height - 86f);
        GUI.Label(nameRect, tileSet.DisplayName, TileSetNameStyle);
        if (!isValid)
        {
            GUI.Label(
                new Rect(cardRect.xMax - 18f, cardRect.y + 2f, 16f, 16f),
                new GUIContent("!", error),
                EditorStyles.boldLabel);
        }
    }

    private void RefreshTileSetCache()
    {
        if (!_tileSetCacheDirty)
        {
            return;
        }

        _tileSetCache.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:StageTileSet"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StageTileSet tileSet =
                AssetDatabase.LoadAssetAtPath<StageTileSet>(path);
            if (tileSet != null)
            {
                _tileSetCache.Add(tileSet);
            }
        }

        _tileSetCache.Sort((left, right) =>
        {
            int displayNameComparison = string.Compare(
                left.DisplayName,
                right.DisplayName,
                StringComparison.OrdinalIgnoreCase);
            if (displayNameComparison != 0)
            {
                return displayNameComparison;
            }

            return string.Compare(
                AssetDatabase.GetAssetPath(left),
                AssetDatabase.GetAssetPath(right),
                StringComparison.OrdinalIgnoreCase);
        });
        _tileSetCacheDirty = false;
    }

    private void CollectVisibleTileSets()
    {
        _visibleTileSets.Clear();
        foreach (StageTileSet tileSet in _tileSetCache)
        {
            if (tileSet == null || !tileSet.SupportsLayer(_selectedLayer) ||
                !MatchesTileSetSearch(tileSet))
            {
                continue;
            }

            _visibleTileSets.Add(tileSet);
        }
    }

    private bool MatchesTileSetSearch(StageTileSet tileSet)
    {
        if (string.IsNullOrWhiteSpace(_tileSetSearch))
        {
            return true;
        }

        return tileSet.DisplayName.IndexOf(
                   _tileSetSearch,
                   StringComparison.OrdinalIgnoreCase) >= 0 ||
               tileSet.name.IndexOf(
                   _tileSetSearch,
                   StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void SelectTileSet(StageTileSet tileSet)
    {
        _selectedTileSet = tileSet;
        EnsureLayerTileSetSelectionStorage();
        int layerIndex = (int)_selectedLayer;
        if (tileSet == null || tileSet.SupportsLayer(_selectedLayer))
        {
            _lastTileSetByLayer[layerIndex] = tileSet;
        }

        Repaint();
    }

    private void RestoreTileSetSelectionForLayer()
    {
        if (_selectedTileSet != null &&
            _selectedTileSet.SupportsLayer(_selectedLayer))
        {
            return;
        }

        EnsureLayerTileSetSelectionStorage();
        StageTileSet remembered = _lastTileSetByLayer[(int)_selectedLayer];
        _selectedTileSet = remembered != null &&
                           remembered.SupportsLayer(_selectedLayer)
            ? remembered
            : null;
    }

    private void EnsureLayerTileSetSelectionStorage()
    {
        if (_lastTileSetByLayer != null &&
            _lastTileSetByLayer.Length == _layers.Length)
        {
            return;
        }

        StageTileSet[] updated = new StageTileSet[_layers.Length];
        if (_lastTileSetByLayer != null)
        {
            Array.Copy(
                _lastTileSetByLayer,
                updated,
                Mathf.Min(_lastTileSetByLayer.Length, updated.Length));
        }
        _lastTileSetByLayer = updated;
    }

    private Texture GetTileSetPreview(StageTileSet tileSet)
    {
        if (tileSet.AutomaticTile != null &&
            tileSet.AutomaticTile.m_DefaultSprite != null)
        {
            Sprite sprite = tileSet.AutomaticTile.m_DefaultSprite;
            Texture2D preview = AssetPreview.GetAssetPreview(sprite);
            return preview != null
                ? preview
                : AssetPreview.GetMiniThumbnail(sprite);
        }

        return tileSet.AutomaticTile != null
            ? AssetPreview.GetMiniThumbnail(tileSet.AutomaticTile)
            : AssetPreview.GetMiniThumbnail(tileSet);
    }

    private string GetTileSetTooltip(StageTileSet tileSet, string error)
    {
        string path = AssetDatabase.GetAssetPath(tileSet);
        string result = $"{tileSet.DisplayName}\n{path}\nLayers: {tileSet.Layers}";
        return string.IsNullOrEmpty(error)
            ? result
            : $"{result}\nWarning: {error}";
    }

    private static void DrawTileSetCardOutline(Rect rect, Color color)
    {
        const float thickness = 1f;
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private GUIStyle TileSetNameStyle => _tileSetNameStyle ??=
        new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperCenter,
            wordWrap = true
        };

    private void MarkTileSetCacheDirty()
    {
        _tileSetCacheDirty = true;
        Repaint();
    }

    private void RepaintTileSetPicker()
    {
        _tileSetPreviewRepaintQueued = false;
        Repaint();
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

        if (_toolMode == StageMapToolMode.Placement &&
            current.keyCode == KeyCode.D)
        {
            SetEditMode(PlacementEditMode.Draw);
            current.Use();
            return;
        }

        if (_toolMode == StageMapToolMode.Placement &&
            current.keyCode == KeyCode.F)
        {
            SetEditMode(PlacementEditMode.Fill);
            current.Use();
            return;
        }

        int layerIndex = GetLayerShortcutIndex(current.keyCode);
        if (layerIndex >= 0)
        {
            StageMap stageMap = StageMapSetupUtility.FindCurrentStageMap();
            if (stageMap != null && layerIndex < _layers.Length)
            {
                SelectLayer(stageMap, _layers[layerIndex]);
            }

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

        Tilemap logicTilemap = stageMap.GetLogicalTilemap(_selectedLayer);
        Tilemap visualTilemap = stageMap.GetVisualTilemap(_selectedLayer);
        if (logicTilemap == null || visualTilemap == null)
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

        if (!TryGetCell(
                logicTilemap,
                current.mousePosition,
                out Vector3Int cell))
        {
            return;
        }

        DrawCellPreview(logicTilemap, cell);
        sceneView.Repaint();

        bool supportedButton = current.button == 0 || current.button == 1;

        if (_toolMode == StageMapToolMode.AutoTexture)
        {
            HandleAutoTextureInput(
                stageMap,
                cell,
                current,
                supportedButton);
            return;
        }

        if (_editMode == PlacementEditMode.Fill)
        {
            HandleFillModeInput(stageMap, cell, current, supportedButton);
            return;
        }

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
            BeginStroke(
                logicTilemap,
                visualTilemap,
                operation,
                current.button);
            EditCell(stageMap, cell);
            current.Use();
        }
        else if (continuesStroke && cell != _lastEditedCell)
        {
            EditCell(stageMap, cell);
            current.Use();
        }
        else if (endsStroke)
        {
            FinishStroke();
            current.Use();
        }
    }

    private void HandleFillModeInput(
        StageMap stageMap,
        Vector3Int cell,
        Event current,
        bool supportedButton)
    {
        if (current.alt || !supportedButton)
        {
            return;
        }

        if (current.type == EventType.MouseDown)
        {
            if (current.button == 0)
            {
                FillEmptyRegion(stageMap, cell);
            }
            else
            {
                EraseConnectedRegion(stageMap, _selectedLayer, cell);
            }

            SceneView.RepaintAll();
            Repaint();
            current.Use();
        }
        else if (current.type == EventType.MouseDrag ||
                 current.type == EventType.MouseUp)
        {
            current.Use();
        }
    }

    private void HandleAutoTextureInput(
        StageMap stageMap,
        Vector3Int cell,
        Event current,
        bool supportedButton)
    {
        if (current.alt || !supportedButton)
        {
            return;
        }

        if (current.type == EventType.MouseDown)
        {
            if (current.button == 0)
            {
                ApplyAutomaticTexture(stageMap, cell);
            }
            else
            {
                ResetAutomaticTexture(stageMap, cell);
            }

            SceneView.RepaintAll();
            Repaint();
            current.Use();
        }
        else if (current.type == EventType.MouseDrag ||
                 current.type == EventType.MouseUp)
        {
            current.Use();
        }
    }

    private void SetPlacementMode(bool enabled)
    {
        if (enabled && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

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

    private void SetToolMode(StageMapToolMode mode)
    {
        if (_toolMode == mode)
        {
            return;
        }

        FinishStroke();
        _toolMode = mode;
        if (_toolMode == StageMapToolMode.AutoTexture)
        {
            RestoreTileSetSelectionForLayer();
        }
        SceneView.RepaintAll();
        Repaint();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode && _placementMode)
        {
            RestoreLayerVisibility(clearCapture: false);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += ReapplyLayerVisibilityAfterSave;
            ShowNotification(new GUIContent(
                "Stop Stage Map editing before entering Play Mode."));
            SceneView.lastActiveSceneView?.ShowNotification(new GUIContent(
                "Play Mode is locked while Stage Map editing is active."));
        }

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
        RestoreTileSetSelectionForLayer();
        if (_placementMode)
        {
            EnsureVisualStageMap(stageMap);
            ApplyLayerVisibility();
            Selection.activeObject = stageMap;
        }

        SceneView.RepaintAll();
        Repaint();
    }

    private void SetEditMode(PlacementEditMode mode)
    {
        if (_editMode == mode)
        {
            return;
        }

        FinishStroke();
        _editMode = mode;
        SceneView.RepaintAll();
        Repaint();
    }

    private void CycleLayer(StageMap stageMap)
    {
        int layerCount = _layers.Length;
        StageMapLayer next =
            (StageMapLayer)(((int)_selectedLayer + 1) % layerCount);
        SelectLayer(stageMap, next);
    }

    private static int GetLayerShortcutIndex(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.Alpha1 or KeyCode.Keypad1 => 0,
            KeyCode.Alpha2 or KeyCode.Keypad2 => 1,
            KeyCode.Alpha3 or KeyCode.Keypad3 => 2,
            _ => -1
        };
    }

    private void BeginStroke(
        Tilemap logicTilemap,
        Tilemap visualTilemap,
        PlacementOperation operation,
        int mouseButton)
    {
        FinishStroke();
        _strokeTilemap = logicTilemap;
        _strokeVisualTilemap = visualTilemap;
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
                new Object[] { _strokeTilemap, _strokeVisualTilemap },
                "Compress Stage Map Bounds");
            _strokeTilemap.CompressBounds();
            _strokeVisualTilemap?.CompressBounds();
            EditorUtility.SetDirty(_strokeTilemap);
            if (_strokeVisualTilemap != null)
            {
                EditorUtility.SetDirty(_strokeVisualTilemap);
            }
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
        _strokeVisualTilemap = null;
        _lastEditedCell = new Vector3Int(int.MinValue, int.MinValue, 0);
    }

    private void EditCell(StageMap stageMap, Vector3Int cell)
    {
        Tilemap logic = stageMap.GetLogicalTilemap(_selectedLayer);
        Tilemap visual = stageMap.GetVisualTilemap(_selectedLayer);
        bool occupied = logic.HasTile(cell);
        if ((_strokeOperation == PlacementOperation.Paint && occupied) ||
            (_strokeOperation == PlacementOperation.Erase && !occupied))
        {
            _lastEditedCell = cell;
            return;
        }

        Undo.RegisterCompleteObjectUndo(
            new Object[] { logic, visual },
            Undo.GetCurrentGroupName());
        if (_strokeOperation == PlacementOperation.Paint)
        {
            logic.SetTile(
                cell,
                StageMapDefaultTiles.GetLogical(_selectedLayer));
            visual.SetTile(
                cell,
                StageMapDefaultTiles.GetVisualDefault(_selectedLayer));
        }
        else
        {
            logic.SetTile(cell, null);
            visual.SetTile(cell, null);
        }

        logic.RefreshTile(cell);
        RefreshVisualNeighborhood(visual, cell);
        EditorUtility.SetDirty(logic);
        EditorUtility.SetDirty(visual);
        EditorSceneManager.MarkSceneDirty(logic.gameObject.scene);
        _lastEditedCell = cell;
        Repaint();
    }

    private void FillEmptyRegion(StageMap stageMap, Vector3Int start)
    {
        WorldBounds2D stageBounds = FindStageBounds(stageMap.gameObject.scene);
        if (stageBounds == null)
        {
            Debug.LogWarning(
                "A WorldBounds2D is required to fill a Stage Map region.",
                stageMap);
            return;
        }

        BoundsInt editableCells = GetEditableCellBounds(
            stageMap.GetLogicalTilemap(_selectedLayer),
            stageBounds);
        if (!ContainsCell(editableCells, start) || HasAnyTile(stageMap, start))
        {
            return;
        }

        List<Vector3Int> region = CollectRegion(
            start,
            cell => ContainsCell(editableCells, cell) &&
                    !HasAnyTile(stageMap, cell));
        if (region.Count == 0)
        {
            return;
        }

        Tilemap logic = stageMap.GetLogicalTilemap(_selectedLayer);
        Tilemap visual = stageMap.GetVisualTilemap(_selectedLayer);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fill Stage Map Region");
        Undo.RegisterCompleteObjectUndo(
            new Object[] { logic, visual },
            "Fill Stage Map Region");
        TileBase logicalTile =
            StageMapDefaultTiles.GetLogical(_selectedLayer);
        TileBase visualTile =
            StageMapDefaultTiles.GetVisualDefault(_selectedLayer);
        foreach (Vector3Int cell in region)
        {
            logic.SetTile(cell, logicalTile);
            visual.SetTile(cell, visualTile);
        }

        FinalizeTilemapEdit(logic);
        FinalizeTilemapEdit(visual);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private void ApplyAutomaticTexture(StageMap stageMap, Vector3Int start)
    {
        if (_selectedTileSet == null)
        {
            ShowNotification(new GUIContent("Assign a valid StageTileSet."));
            return;
        }

        if (!_selectedTileSet.TryValidate(
                _selectedLayer,
                out string validationError))
        {
            ShowNotification(new GUIContent(validationError));
            return;
        }

        Tilemap logic = stageMap.GetLogicalTilemap(_selectedLayer);
        Tilemap visual = stageMap.GetVisualTilemap(_selectedLayer);
        if (!logic.HasTile(start))
        {
            return;
        }

        List<Vector3Int> region = CollectRegion(start, logic.HasTile);
        if (region.Count == 0)
        {
            return;
        }

        Vector3Int[] positions = region.ToArray();
        TileBase[] tiles = new TileBase[positions.Length];
        for (int index = 0; index < tiles.Length; index++)
        {
            tiles[index] = _selectedTileSet.AutomaticTile;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Automatic Stage Texture");
        Undo.RegisterCompleteObjectUndo(
            visual,
            "Apply Automatic Stage Texture");
        visual.SetTiles(positions, tiles);
        FinalizeTilemapEdit(visual);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private void ResetAutomaticTexture(StageMap stageMap, Vector3Int start)
    {
        Tilemap logic = stageMap.GetLogicalTilemap(_selectedLayer);
        Tilemap visual = stageMap.GetVisualTilemap(_selectedLayer);
        if (!logic.HasTile(start))
        {
            return;
        }

        List<Vector3Int> region = CollectRegion(start, logic.HasTile);
        if (region.Count == 0)
        {
            return;
        }

        Vector3Int[] positions = region.ToArray();
        TileBase[] tiles = new TileBase[positions.Length];
        TileBase defaultTile =
            StageMapDefaultTiles.GetVisualDefault(_selectedLayer);
        for (int index = 0; index < tiles.Length; index++)
        {
            tiles[index] = defaultTile;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Reset Automatic Stage Texture");
        Undo.RegisterCompleteObjectUndo(
            visual,
            "Reset Automatic Stage Texture");
        visual.SetTiles(positions, tiles);
        FinalizeTilemapEdit(visual);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static void EraseConnectedRegion(
        StageMap stageMap,
        StageMapLayer layer,
        Vector3Int start)
    {
        Tilemap logic = stageMap.GetLogicalTilemap(layer);
        Tilemap visual = stageMap.GetVisualTilemap(layer);
        if (logic == null || visual == null || !logic.HasTile(start))
        {
            return;
        }

        List<Vector3Int> region = CollectRegion(start, logic.HasTile);
        if (region.Count == 0)
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Erase Connected Stage Map Region");
        Undo.RegisterCompleteObjectUndo(
            new Object[] { logic, visual },
            "Erase Connected Stage Map Region");
        foreach (Vector3Int cell in region)
        {
            logic.SetTile(cell, null);
            visual.SetTile(cell, null);
        }

        logic.CompressBounds();
        visual.CompressBounds();
        FinalizeTilemapEdit(logic);
        FinalizeTilemapEdit(visual);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static List<Vector3Int> CollectRegion(
        Vector3Int start,
        System.Func<Vector3Int, bool> belongsToRegion)
    {
        List<Vector3Int> region = new();
        Queue<Vector3Int> pending = new();
        HashSet<Vector3Int> visited = new();
        pending.Enqueue(start);
        visited.Add(start);

        while (pending.Count > 0)
        {
            Vector3Int cell = pending.Dequeue();
            if (!belongsToRegion(cell))
            {
                continue;
            }

            region.Add(cell);
            foreach (Vector3Int direction in _cardinalDirections)
            {
                Vector3Int neighbor = cell + direction;
                neighbor.z = 0;
                if (visited.Add(neighbor))
                {
                    pending.Enqueue(neighbor);
                }
            }
        }

        return region;
    }

    private static bool HasAnyTile(StageMap stageMap, Vector3Int cell)
    {
        foreach (StageMapLayer layer in _layers)
        {
            Tilemap tilemap = stageMap.GetLogicalTilemap(layer);
            if (tilemap != null && tilemap.HasTile(cell))
            {
                return true;
            }
        }

        return false;
    }

    private static BoundsInt GetEditableCellBounds(
        Tilemap tilemap,
        WorldBounds2D stageBounds)
    {
        const float inwardOffset = 0.0001f;
        Vector2 worldMin = stageBounds.WorldMin;
        Vector2 worldMax = stageBounds.WorldMax;
        Vector3Int minCell = tilemap.WorldToCell(new Vector3(
            worldMin.x + inwardOffset,
            worldMin.y + inwardOffset,
            tilemap.transform.position.z));
        Vector3Int maxCell = tilemap.WorldToCell(new Vector3(
            worldMax.x - inwardOffset,
            worldMax.y - inwardOffset,
            tilemap.transform.position.z));
        minCell.z = 0;
        maxCell.z = 0;

        Vector3Int border = new(
            EditableBorderCellCount,
            EditableBorderCellCount,
            0);
        minCell -= border;
        maxCell += border;

        return new BoundsInt(
            minCell.x,
            minCell.y,
            0,
            maxCell.x - minCell.x + 1,
            maxCell.y - minCell.y + 1,
            1);
    }

    private static bool ContainsCell(BoundsInt bounds, Vector3Int cell)
    {
        return cell.x >= bounds.xMin && cell.x < bounds.xMax &&
               cell.y >= bounds.yMin && cell.y < bounds.yMax;
    }

    private static void ClearAllTilemaps(StageMap stageMap)
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Clear All Stage Map Tilemaps");

        foreach (StageMapLayer layer in _layers)
        {
            Tilemap logic = stageMap.GetLogicalTilemap(layer);
            Tilemap visual = stageMap.GetVisualTilemap(layer);
            if (logic == null || visual == null)
            {
                continue;
            }

            Undo.RegisterCompleteObjectUndo(
                new Object[] { logic, visual },
                "Clear All Stage Map Tilemaps");
            logic.ClearAllTiles();
            visual.ClearAllTiles();
            logic.CompressBounds();
            visual.CompressBounds();
            FinalizeTilemapEdit(logic);
            FinalizeTilemapEdit(visual);
        }

        Undo.CollapseUndoOperations(undoGroup);
        SceneView.RepaintAll();
    }

    private static void FinalizeTilemapEdit(Tilemap tilemap)
    {
        tilemap.RefreshAllTiles();
        EditorUtility.SetDirty(tilemap);
        EditorSceneManager.MarkSceneDirty(tilemap.gameObject.scene);
    }

    private static void RefreshVisualNeighborhood(
        Tilemap visual,
        Vector3Int center)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                visual.RefreshTile(center + new Vector3Int(x, y, 0));
            }
        }
    }

    private string GetModeHelpText()
    {
        string common =
            "`: Next Tilemap  |  1/2/3: Select Tilemap\n" +
            "Escape: Stop Editing\n" +
            "Hold Alt to use normal Scene View navigation.";
        if (_toolMode == StageMapToolMode.AutoTexture)
        {
            return "Left-click: Apply the selected tile set to the " +
                   "connected Logic region\n" +
                   "Right-click: Reset the connected region to its " +
                   "default visual\n" +
                   common;
        }

        return "D: Draw Mode  |  F: Fill Mode\n" +
               "Draw - Left/right-click + drag: Paint/erase cells\n" +
               "Fill - Left-click: Fill empty region  |  " +
               "Right-click: Erase selected Tilemap region\n" +
               common;
    }

    private void EnsureVisualStageMap(StageMap stageMap)
    {
        if (_visualStageMap == stageMap && _originalColorsCaptured)
        {
            return;
        }

        RestoreLayerVisibility(clearCapture: true);
        _visualStageMap = stageMap;
        _platformOriginalColor = stageMap.PlatformVisual.color;
        _decorationBackOriginalColor = stageMap.DecorationBackVisual.color;
        _decorationFrontOriginalColor = stageMap.DecorationFrontVisual.color;
        _originalColorsCaptured = true;
    }

    private void ApplyLayerVisibility()
    {
        if (!_originalColorsCaptured || _visualStageMap == null)
        {
            return;
        }

        _visualStageMap.PlatformVisual.color = GetPlacementColor(
            StageMapLayer.Platform,
            _platformOriginalColor);
        _visualStageMap.DecorationBackVisual.color = GetPlacementColor(
            StageMapLayer.DecorationBack,
            _decorationBackOriginalColor);
        _visualStageMap.DecorationFrontVisual.color = GetPlacementColor(
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
            if (_visualStageMap.PlatformVisual != null)
            {
                _visualStageMap.PlatformVisual.color = _platformOriginalColor;
            }
            if (_visualStageMap.DecorationBackVisual != null)
            {
                _visualStageMap.DecorationBackVisual.color =
                    _decorationBackOriginalColor;
            }
            if (_visualStageMap.DecorationFrontVisual != null)
            {
                _visualStageMap.DecorationFrontVisual.color =
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
            _toolMode == StageMapToolMode.AutoTexture
                ? $"Auto Texture  {_selectedLayer}  {cell.x}, {cell.y}"
                : $"{_editMode}  {_selectedLayer}  {cell.x}, {cell.y}");
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
        return StageMapDefaultTiles.GetColor(layer);
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
