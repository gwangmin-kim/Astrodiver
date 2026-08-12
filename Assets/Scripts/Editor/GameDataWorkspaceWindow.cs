using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class GameDataWorkspaceWindow : EditorWindow
{
    private const string MenuPath = "Astrodiver/Data/Game Data Workspace";
    private const float SheetPanelWidth = 180f;
    private const float DetailPanelWidth = 360f;
    private const float RowHeight = 22f;
    private const string EntryDragDataKey =
        "Astrodiver.GameDataWorkspaceWindow.Entry";

    private readonly List<GameDataSheetDefinition> _sheets = new();
    private readonly List<GameDefinition> _entries = new();
    private readonly Dictionary<string, string> _identityDrafts = new();

    private GameDataSheetDefinition _selectedSheet;
    private GameDefinition _selectedEntry;
    private GameDefinition _dragCandidate;
    private GameDefinition _pendingDraggedEntry;
    private GameDefinition _pendingDropTarget;
    private bool _pendingDropAfter;
    private Editor _selectedEditor;
    private Vector2 _dragStartPosition;
    private Vector2 _sheetScroll;
    private Vector2 _entryScroll;
    private Vector2 _detailScroll;
    private Vector2 _validationScroll;
    private string _search = string.Empty;
    private string _newKey = string.Empty;
    private string _statusMessage = string.Empty;
    private MessageType _statusType = MessageType.Info;
    private List<string> _validationMessages = new();

    [MenuItem(MenuPath)]
    public static void Open()
    {
        GameDataWorkspaceWindow window = GetWindow<GameDataWorkspaceWindow>();
        window.titleContent = new GUIContent("Game Data");
        window.minSize = new Vector2(980f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        ReloadSheets();
    }

    private void OnDisable()
    {
        DestroySelectedEditor();
    }

    private void OnProjectChange()
    {
        ReloadSheets();
        Repaint();
    }

    private void OnGUI()
    {
        DrawGlobalToolbar();

        if (_sheets.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No GameDataSheetDefinition assets were found.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawSheetPanel();
        DrawEntryPanel();
        DrawDetailPanel();
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }
    }

    private void DrawGlobalToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(62f)))
        {
            ReloadSheets();
        }

        if (GUILayout.Button("Refresh Catalog", EditorStyles.toolbarButton, GUILayout.Width(105f)))
        {
            GameDefinitionCatalogEditor.RefreshDefaultCatalog();
            SetStatus("Game definition catalog refreshed.", MessageType.Info);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(
            _selectedSheet != null ? _selectedSheet.CategoryKey : "No sheet selected",
            EditorStyles.miniLabel,
            GUILayout.Width(180f));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSheetPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(SheetPanelWidth));
        EditorGUILayout.LabelField("Data Sheets", EditorStyles.boldLabel);
        _sheetScroll = EditorGUILayout.BeginScrollView(_sheetScroll);

        foreach (GameDataSheetDefinition sheet in _sheets)
        {
            bool selected = sheet == _selectedSheet;
            GUIStyle style = selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
            string label = string.IsNullOrEmpty(sheet.CategoryKey)
                ? sheet.name
                : sheet.CategoryKey;
            if (GUILayout.Button(label, style, GUILayout.Height(24f)))
            {
                SelectSheet(sheet);
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();
        if (_selectedSheet != null &&
            GUILayout.Button("Select Sheet Asset", GUILayout.Height(24f)))
        {
            Selection.activeObject = _selectedSheet;
            EditorGUIUtility.PingObject(_selectedSheet);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawEntryPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        if (_selectedSheet == null)
        {
            EditorGUILayout.HelpBox("Select a data sheet.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        if (!_selectedSheet.TryValidate(out string sheetError))
        {
            EditorGUILayout.HelpBox(sheetError, MessageType.Error);
            EditorGUILayout.EndVertical();
            return;
        }

        DrawEntryToolbar();
        DrawCreateToolbar();
        DrawTableHeader();
        DrawEntryTable();
        DrawValidationResults();
        EditorGUILayout.EndVertical();
    }

    private void DrawEntryToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        _search = EditorGUILayout.TextField(
            GUIContent.none,
            _search,
            EditorStyles.toolbarSearchField,
            GUILayout.MinWidth(130f));

        if (GUILayout.Button("Validate", GUILayout.Width(70f)))
        {
            ValidateCurrentSheet();
        }

        if (GUILayout.Button("Reindex", GUILayout.Width(68f)))
        {
            NormalizeOrders();
        }

        if (GUILayout.Button("Sync Filenames", GUILayout.Width(108f)))
        {
            SyncFilenames();
        }

        Color originalBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(1f, 0.65f, 0.65f);
        using (new EditorGUI.DisabledScope(_selectedEntry == null))
        {
            if (GUILayout.Button("Delete Selected", GUILayout.Width(105f)))
            {
                DeleteSelectedEntry();
            }
        }
        GUI.backgroundColor = originalBackground;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCreateToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("New key", GUILayout.Width(52f));
        _newKey = EditorGUILayout.TextField(_newKey, GUILayout.MinWidth(130f));
        if (GUILayout.Button("Create Entry", GUILayout.Width(92f)))
        {
            CreateEntry();
        }
        EditorGUILayout.LabelField(
            "ID and key are locked after creation.",
            EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTableHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(RowHeight));
        GUILayout.Label("Drag", GUILayout.Width(28f));
        GUILayout.Label(string.Empty, GUILayout.Width(20f));
        GUILayout.Label("Order", GUILayout.Width(44f));
        GUILayout.Label("Key", GUILayout.Width(128f));
        GUILayout.Label("ID", GUILayout.Width(180f));
        GUILayout.Label("Asset", GUILayout.Width(170f));
        DrawTypeHeaders(_selectedSheet.DefinitionType);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEntryTable()
    {
        _entryScroll = EditorGUILayout.BeginScrollView(_entryScroll);
        string query = _search?.Trim() ?? string.Empty;

        for (int i = 0; i < _entries.Count; i++)
        {
            GameDefinition entry = _entries[i];
            if (entry == null || !MatchesSearch(entry, query))
            {
                continue;
            }

            DrawEntryRow(entry);
        }

        EditorGUILayout.EndScrollView();
        ApplyPendingReorder();
    }

    private void DrawEntryRow(GameDefinition entry)
    {
        SerializedObject serialized = new(entry);
        serialized.Update();
        bool initialized = !string.IsNullOrWhiteSpace(entry.Key);
        bool selected = entry == _selectedEntry;
        Color originalBackground = GUI.backgroundColor;
        if (selected)
        {
            GUI.backgroundColor = new Color(0.65f, 0.82f, 1f);
        }

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(RowHeight + 2f));
        Rect dragHandleRect = GUILayoutUtility.GetRect(
            new GUIContent("≡", "Drag to reorder"),
            EditorStyles.miniButton,
            GUILayout.Width(28f),
            GUILayout.Height(RowHeight));
        GUI.Box(
            dragHandleRect,
            new GUIContent("≡", "Drag to reorder"),
            EditorStyles.miniButton);
        HandleDragSource(entry, dragHandleRect);

        if (GUILayout.Toggle(selected, GUIContent.none, GUILayout.Width(20f)) != selected)
        {
            SelectEntry(entry);
        }

        EditorGUILayout.LabelField(
            entry.SortOrder.ToString(),
            GUILayout.Width(44f));

        if (initialized)
        {
            EditorGUILayout.SelectableLabel(
                entry.Key,
                EditorStyles.label,
                GUILayout.Width(128f),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }
        else
        {
            string assetPath = AssetDatabase.GetAssetPath(entry);
            if (!_identityDrafts.TryGetValue(assetPath, out string draft))
            {
                draft = SuggestKey(
                    entry.name,
                    _selectedSheet.CategoryKey,
                    _selectedSheet.OrderDigits);
            }

            draft = EditorGUILayout.TextField(draft, GUILayout.Width(82f));
            _identityDrafts[assetPath] = draft;
            if (GUILayout.Button("Set", GUILayout.Width(42f)))
            {
                InitializeIdentity(entry, draft);
            }
        }

        EditorGUILayout.SelectableLabel(
            entry.Id ?? string.Empty,
            EditorStyles.miniLabel,
            GUILayout.Width(180f),
            GUILayout.Height(EditorGUIUtility.singleLineHeight));

        string currentFile = Path.GetFileNameWithoutExtension(
            AssetDatabase.GetAssetPath(entry));
        string expectedFile = initialized ? GetExpectedAssetName(entry) : string.Empty;
        GUIContent assetLabel = new(
            currentFile,
            initialized && !string.Equals(currentFile, expectedFile, StringComparison.Ordinal)
                ? $"Expected: {expectedFile}"
                : AssetDatabase.GetAssetPath(entry));
        if (GUILayout.Button(assetLabel, EditorStyles.miniButton, GUILayout.Width(170f)))
        {
            SelectEntry(entry);
            EditorGUIUtility.PingObject(entry);
        }

        DrawTypeCells(entry, serialized);

        if (serialized.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(entry);
        }

        EditorGUILayout.EndHorizontal();
        HandleDropTarget(entry, GUILayoutUtility.GetLastRect());
        GUI.backgroundColor = originalBackground;
    }

    private static void DrawTypeHeaders(Type type)
    {
        if (type == typeof(ResourceDefinition))
        {
            GUILayout.Label("Icon", GUILayout.Width(75f));
            GUILayout.Label("Particle Row", GUILayout.Width(82f));
        }
        else if (type == typeof(CreatureDefinition))
        {
            GUILayout.Label("Icon", GUILayout.Width(75f));
            GUILayout.Label("Max Stack", GUILayout.Width(72f));
        }
        else if (type == typeof(FloatageDefinition))
        {
            GUILayout.Label("HP", GUILayout.Width(55f));
            GUILayout.Label("Drop Resource", GUILayout.Width(110f));
            GUILayout.Label("Count", GUILayout.Width(55f));
        }
        else if (type == typeof(UpgradeNodeDefinition))
        {
            GUILayout.Label("Icon", GUILayout.Width(75f));
            GUILayout.Label("Parent", GUILayout.Width(130f));
            GUILayout.Label("Max Level", GUILayout.Width(70f));
        }
        else if (type == typeof(StageDefinition))
        {
            GUILayout.Label("Respawn", GUILayout.Width(70f));
        }
    }

    private static void DrawTypeCells(GameDefinition entry, SerializedObject serialized)
    {
        if (entry is ResourceDefinition)
        {
            DrawProperty(serialized, "_icon", 75f);
            DrawProperty(serialized, "_particleRowIndex", 82f);
        }
        else if (entry is CreatureDefinition)
        {
            DrawProperty(serialized, "_icon", 75f);
            DrawProperty(serialized, "_maxStackCount", 72f);
        }
        else if (entry is FloatageDefinition)
        {
            DrawProperty(serialized, "_hp", 55f);
            SerializedProperty dropData = serialized.FindProperty("_dropData");
            DrawProperty(dropData?.FindPropertyRelative("resource"), 110f);
            DrawProperty(dropData?.FindPropertyRelative("count"), 55f);
        }
        else if (entry is UpgradeNodeDefinition)
        {
            DrawProperty(serialized, "_icon", 75f);
            DrawProperty(serialized, "_parent", 130f);
            DrawProperty(serialized, "_maxLevel", 70f);
        }
        else if (entry is StageDefinition)
        {
            DrawProperty(serialized, "_respawnIntervalSeconds", 70f);
        }
    }

    private static void DrawProperty(
        SerializedObject serialized,
        string propertyName,
        float width)
    {
        DrawProperty(serialized.FindProperty(propertyName), width);
    }

    private static void DrawProperty(SerializedProperty property, float width)
    {
        if (property == null)
        {
            GUILayout.Label("-", GUILayout.Width(width));
            return;
        }

        EditorGUILayout.PropertyField(
            property,
            GUIContent.none,
            false,
            GUILayout.Width(width));
    }

    private void DrawDetailPanel()
    {
        EditorGUILayout.BeginVertical(
            EditorStyles.helpBox,
            GUILayout.Width(DetailPanelWidth));
        EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
        if (_selectedEntry == null)
        {
            EditorGUILayout.HelpBox(
                "Select an entry to edit references, lists, and nested values.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField("Key", _selectedEntry.Key ?? string.Empty);
        EditorGUILayout.LabelField("ID", _selectedEntry.Id ?? string.Empty);
        EditorGUILayout.LabelField(
            "Path",
            AssetDatabase.GetAssetPath(_selectedEntry),
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ping"))
        {
            EditorGUIUtility.PingObject(_selectedEntry);
        }
        if (GUILayout.Button("Open Inspector"))
        {
            Selection.activeObject = _selectedEntry;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
        Editor.CreateCachedEditor(_selectedEntry, null, ref _selectedEditor);
        _selectedEditor?.OnInspectorGUI();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawValidationResults()
    {
        if (_validationMessages.Count == 0)
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        _validationScroll = EditorGUILayout.BeginScrollView(
            _validationScroll,
            GUILayout.MaxHeight(120f));
        foreach (string message in _validationMessages)
        {
            EditorGUILayout.HelpBox(message, MessageType.Error);
        }
        EditorGUILayout.EndScrollView();
    }

    private void ReloadSheets()
    {
        string selectedPath = _selectedSheet != null
            ? AssetDatabase.GetAssetPath(_selectedSheet)
            : string.Empty;
        _sheets.Clear();
        _sheets.AddRange(
            AssetDatabase.FindAssets("t:GameDataSheetDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameDataSheetDefinition>)
                .Where(sheet => sheet != null)
                .OrderBy(sheet => sheet.CategoryKey, StringComparer.Ordinal));

        _selectedSheet = !string.IsNullOrEmpty(selectedPath)
            ? AssetDatabase.LoadAssetAtPath<GameDataSheetDefinition>(selectedPath)
            : _sheets.FirstOrDefault();
        ReloadEntries();
    }

    private void ReloadEntries()
    {
        string selectedPath = _selectedEntry != null
            ? AssetDatabase.GetAssetPath(_selectedEntry)
            : string.Empty;
        _entries.Clear();
        _identityDrafts.Clear();
        _validationMessages.Clear();

        if (_selectedSheet == null || !_selectedSheet.TryValidate(out _))
        {
            SelectEntry(null);
            return;
        }

        string[] guids = AssetDatabase.FindAssets(
            $"t:{_selectedSheet.DefinitionType.Name}",
            new[] { _selectedSheet.AssetFolderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameDefinition definition = AssetDatabase.LoadAssetAtPath(
                path,
                _selectedSheet.DefinitionType) as GameDefinition;
            if (definition != null)
            {
                _entries.Add(definition);
            }
        }

        SortEntries();
        GameDefinition restored = !string.IsNullOrEmpty(selectedPath)
            ? AssetDatabase.LoadAssetAtPath<GameDefinition>(selectedPath)
            : null;
        SelectEntry(restored != null && _entries.Contains(restored) ? restored : null);
    }

    private void SortEntries()
    {
        _entries.Sort((left, right) =>
        {
            int order = left.SortOrder.CompareTo(right.SortOrder);
            if (order != 0)
            {
                return order;
            }

            string leftKey = string.IsNullOrWhiteSpace(left.Key) ? left.name : left.Key;
            string rightKey = string.IsNullOrWhiteSpace(right.Key) ? right.name : right.Key;
            return string.Compare(leftKey, rightKey, StringComparison.Ordinal);
        });
    }

    private void SelectSheet(GameDataSheetDefinition sheet)
    {
        if (_selectedSheet == sheet)
        {
            return;
        }

        _selectedSheet = sheet;
        _statusMessage = string.Empty;
        ReloadEntries();
        GUI.FocusControl(null);
    }

    private void SelectEntry(GameDefinition entry)
    {
        if (_selectedEntry == entry)
        {
            return;
        }

        _selectedEntry = entry;
        DestroySelectedEditor();
    }

    private void DestroySelectedEditor()
    {
        if (_selectedEditor != null)
        {
            DestroyImmediate(_selectedEditor);
            _selectedEditor = null;
        }
    }

    private void CreateEntry()
    {
        string key = _newKey.Trim();
        if (!ValidateNewIdentity(key, null, out string error))
        {
            SetStatus(error, MessageType.Error);
            return;
        }

        int sortOrder = _entries.Count;
        string id = BuildId(key);
        string assetName = BuildAssetName(sortOrder, key);
        string path = $"{_selectedSheet.AssetFolderPath}/{assetName}.asset";
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
        {
            SetStatus($"An asset already exists at '{path}'.", MessageType.Error);
            return;
        }

        GameDefinition entry = CreateInstance(_selectedSheet.DefinitionType) as GameDefinition;
        if (entry == null)
        {
            SetStatus("Could not create the configured definition type.", MessageType.Error);
            return;
        }

        SerializedObject serialized = new(entry);
        serialized.FindProperty("_id").stringValue = id;
        serialized.FindProperty("_key").stringValue = key;
        serialized.FindProperty("_sortOrder").intValue = sortOrder;
        TryAssignIcon(serialized, key);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(entry, path);
        AssetDatabase.SaveAssets();
        _newKey = string.Empty;
        ReloadEntries();
        SelectEntry(AssetDatabase.LoadAssetAtPath<GameDefinition>(path));
        GameDefinitionCatalogEditor.RefreshDefaultCatalog();
        SetStatus($"Created '{id}'.", MessageType.Info);
    }

    private void InitializeIdentity(GameDefinition entry, string draft)
    {
        string key = draft.Trim();
        if (!ValidateNewIdentity(key, entry, out string error))
        {
            SetStatus(error, MessageType.Error);
            return;
        }

        string id = BuildId(key);
        string oldId = entry.Id;
        string prompt = string.IsNullOrWhiteSpace(oldId)
            ? $"Initialize this entry as '{id}'? The key and ID will then be locked."
            : $"Replace the legacy ID '{oldId}' with '{id}'? The key and ID will then be locked.";
        if (!EditorUtility.DisplayDialog("Initialize Identity", prompt, "Initialize", "Cancel"))
        {
            return;
        }

        Undo.RecordObject(entry, "Initialize game definition identity");
        SerializedObject serialized = new(entry);
        serialized.FindProperty("_id").stringValue = id;
        serialized.FindProperty("_key").stringValue = key;
        TryAssignIcon(serialized, key);
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(entry);
        AssetDatabase.SaveAssets();
        _identityDrafts.Remove(AssetDatabase.GetAssetPath(entry));
        GameDefinitionCatalogEditor.RefreshDefaultCatalog();
        SetStatus($"Initialized '{id}'. Use Sync Filenames when ready.", MessageType.Info);
    }

    private bool ValidateNewIdentity(
        string key,
        GameDefinition ignored,
        out string error)
    {
        if (!GameDataSheetDefinition.IsValidKey(key))
        {
            error = "Key must start with a lowercase letter and contain only lowercase letters, numbers, and underscores.";
            return false;
        }

        string id = BuildId(key);
        foreach (GameDefinition definition in FindAllDefinitions())
        {
            if (definition != ignored &&
                (string.Equals(definition.Id, id, StringComparison.Ordinal) ||
                 (_entries.Contains(definition) &&
                  string.Equals(definition.Key, key, StringComparison.Ordinal))))
            {
                error = $"The key or ID '{id}' is already in use.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private void NormalizeOrders()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        SortEntries();
        ReindexEntries("Reindex data entries");
        SetStatus($"Reindexed {_entries.Count} entries from 0. Filenames were not changed.", MessageType.Info);
    }

    private void DeleteSelectedEntry()
    {
        if (_selectedEntry == null)
        {
            return;
        }

        GameDefinition entry = _selectedEntry;
        GameDataSheetDefinition sheet = _selectedSheet;
        string path = AssetDatabase.GetAssetPath(entry);
        string label = string.IsNullOrWhiteSpace(entry.Id) ? entry.name : entry.Id;
        if (!EditorUtility.DisplayDialog(
                "Delete Data Entry",
                $"Move '{label}' to the system Trash/Recycle Bin?\n\n" +
                $"Asset: {path}\n\n" +
                "References to this entry may become missing.",
                "Move to Trash",
                "Cancel"))
        {
            return;
        }

        if (!MoveEntryToTrash(entry))
        {
            SetStatus($"Could not move '{path}' to the Trash/Recycle Bin.", MessageType.Error);
            return;
        }

        _selectedSheet = sheet;
        ReloadEntries();
        ReindexEntries("Reindex after deleting data entry");
        GameDefinitionCatalogEditor.RefreshDefaultCatalog();
        SetStatus($"Moved '{label}' to the Trash/Recycle Bin and reindexed the sheet.", MessageType.Info);
    }

    private bool MoveEntryToTrash(GameDefinition entry)
    {
        if (entry == null)
        {
            return false;
        }

        string path = AssetDatabase.GetAssetPath(entry);
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (_selectedEntry == entry)
        {
            SelectEntry(null);
        }

        bool moved = AssetDatabase.MoveAssetToTrash(path);
        if (moved)
        {
            AssetDatabase.SaveAssets();
        }

        return moved;
    }

    private void HandleDragSource(GameDefinition entry, Rect handleRect)
    {
        Event current = Event.current;
        if (current.type == EventType.MouseDown && current.button == 0 &&
            handleRect.Contains(current.mousePosition))
        {
            _dragCandidate = entry;
            _dragStartPosition = current.mousePosition;
            SelectEntry(entry);
            current.Use();
            return;
        }

        if (current.type == EventType.MouseDrag && _dragCandidate == entry &&
            Vector2.Distance(_dragStartPosition, current.mousePosition) >= 4f)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { entry };
            DragAndDrop.SetGenericData(EntryDragDataKey, entry);
            DragAndDrop.StartDrag($"Reorder {entry.name}");
            _dragCandidate = null;
            current.Use();
        }
        else if (current.type == EventType.MouseUp && _dragCandidate == entry)
        {
            _dragCandidate = null;
        }
    }

    private void HandleDropTarget(GameDefinition target, Rect rowRect)
    {
        Event current = Event.current;
        GameDefinition dragged =
            DragAndDrop.GetGenericData(EntryDragDataKey) as GameDefinition;
        if (dragged == null || dragged == target || !_entries.Contains(dragged) ||
            !rowRect.Contains(current.mousePosition))
        {
            return;
        }

        if (current.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            current.Use();
        }
        else if (current.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            _pendingDraggedEntry = dragged;
            _pendingDropTarget = target;
            _pendingDropAfter = current.mousePosition.y >= rowRect.center.y;
            DragAndDrop.SetGenericData(EntryDragDataKey, null);
            current.Use();
        }
    }

    private void ApplyPendingReorder()
    {
        if (_pendingDraggedEntry == null || _pendingDropTarget == null)
        {
            return;
        }

        GameDefinition dragged = _pendingDraggedEntry;
        GameDefinition target = _pendingDropTarget;
        bool insertAfter = _pendingDropAfter;
        _pendingDraggedEntry = null;
        _pendingDropTarget = null;

        if (!_entries.Remove(dragged))
        {
            return;
        }

        int targetIndex = _entries.IndexOf(target);
        if (targetIndex < 0)
        {
            _entries.Add(dragged);
            SortEntries();
            return;
        }

        int insertIndex = insertAfter ? targetIndex + 1 : targetIndex;
        _entries.Insert(Mathf.Clamp(insertIndex, 0, _entries.Count), dragged);
        ReindexEntries("Reorder data entries");
        SetStatus("Reordered entries and assigned sequential orders from 0. Filenames were not changed.", MessageType.Info);
    }

    private void ReindexEntries(string undoName)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        Undo.RecordObjects(_entries.Cast<UnityEngine.Object>().ToArray(), undoName);
        for (int i = 0; i < _entries.Count; i++)
        {
            SerializedObject serialized = new(_entries[i]);
            serialized.FindProperty("_sortOrder").intValue = i;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_entries[i]);
        }

        AssetDatabase.SaveAssets();
    }

    private void SyncFilenames()
    {
        List<RenameOperation> operations = new();
        HashSet<string> targetPaths = new(StringComparer.OrdinalIgnoreCase);
        foreach (GameDefinition entry in _entries)
        {
            if (!GameDataSheetDefinition.IsValidKey(entry.Key))
            {
                SetStatus($"'{entry.name}' has no valid initialized key.", MessageType.Error);
                return;
            }

            string originalPath = AssetDatabase.GetAssetPath(entry);
            string desiredName = GetExpectedAssetName(entry);
            string desiredPath = $"{_selectedSheet.AssetFolderPath}/{desiredName}.asset";
            if (!targetPaths.Add(desiredPath))
            {
                SetStatus($"Multiple entries resolve to '{desiredPath}'.", MessageType.Error);
                return;
            }

            if (!string.Equals(originalPath, desiredPath, StringComparison.Ordinal))
            {
                operations.Add(new RenameOperation(entry, originalPath, desiredName, desiredPath));
            }
        }

        foreach (RenameOperation operation in operations)
        {
            UnityEngine.Object occupied = AssetDatabase.LoadMainAssetAtPath(operation.DesiredPath);
            if (occupied != null && !_entries.Contains(occupied as GameDefinition))
            {
                SetStatus($"Target path is occupied: '{operation.DesiredPath}'.", MessageType.Error);
                return;
            }
        }

        if (operations.Count == 0)
        {
            SetStatus("All filenames are already synchronized.", MessageType.Info);
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Sync Filenames",
                $"Rename {operations.Count} assets? Unity GUID references will be preserved.",
                "Rename",
                "Cancel"))
        {
            return;
        }

        List<string> errors = new();
        foreach (RenameOperation operation in operations)
        {
            operation.TempName = $"__sheet_tmp_{Guid.NewGuid():N}";
            string renameError = AssetDatabase.RenameAsset(
                operation.OriginalPath,
                operation.TempName);
            if (!string.IsNullOrEmpty(renameError))
            {
                errors.Add(renameError);
                break;
            }
            operation.TempPath = $"{Path.GetDirectoryName(operation.OriginalPath)?.Replace('\\', '/')}/{operation.TempName}.asset";
        }

        if (errors.Count == 0)
        {
            foreach (RenameOperation operation in operations)
            {
                string renameError = AssetDatabase.RenameAsset(
                    operation.TempPath,
                    operation.DesiredName);
                if (!string.IsNullOrEmpty(renameError))
                {
                    errors.Add(renameError);
                    break;
                }
            }
        }

        if (errors.Count > 0)
        {
            RollbackRenames(operations, errors);
        }

        AssetDatabase.SaveAssets();
        ReloadEntries();
        if (errors.Count == 0)
        {
            SetStatus($"Renamed {operations.Count} assets.", MessageType.Info);
        }
        else
        {
            SetStatus(string.Join(Environment.NewLine, errors), MessageType.Error);
        }
    }

    private void ValidateCurrentSheet()
    {
        _validationMessages = CollectValidationMessages();
        SetStatus(
            _validationMessages.Count == 0
                ? $"Validated {_entries.Count} entries with no errors."
                : $"Validation found {_validationMessages.Count} errors.",
            _validationMessages.Count == 0 ? MessageType.Info : MessageType.Error);
    }

    private List<string> CollectValidationMessages()
    {
        List<string> messages = new();
        if (!_selectedSheet.TryValidate(out string sheetError))
        {
            messages.Add(sheetError);
            return messages;
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        HashSet<string> keys = new(StringComparer.Ordinal);
        HashSet<int> orders = new();
        HashSet<string> filenames = new(StringComparer.OrdinalIgnoreCase);
        foreach (GameDefinition entry in _entries)
        {
            string label = string.IsNullOrWhiteSpace(entry.Key) ? entry.name : entry.Key;
            if (!GameDataSheetDefinition.IsValidKey(entry.Key))
            {
                messages.Add($"{label}: key is missing or invalid.");
            }
            else
            {
                if (!keys.Add(entry.Key))
                {
                    messages.Add($"{label}: duplicate key '{entry.Key}'.");
                }

                string expectedId = BuildId(entry.Key);
                if (!string.Equals(entry.Id, expectedId, StringComparison.Ordinal))
                {
                    messages.Add($"{label}: ID must be '{expectedId}', but is '{entry.Id}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(entry.Id) || !ids.Add(entry.Id))
            {
                messages.Add($"{label}: ID is empty or duplicated.");
            }

            if (!orders.Add(entry.SortOrder))
            {
                messages.Add($"{label}: sort order {entry.SortOrder} is duplicated.");
            }

            if (GameDataSheetDefinition.IsValidKey(entry.Key))
            {
                string expectedFile = GetExpectedAssetName(entry);
                if (!filenames.Add(expectedFile))
                {
                    messages.Add($"{label}: target filename '{expectedFile}' is duplicated.");
                }

                string currentFile = Path.GetFileNameWithoutExtension(
                    AssetDatabase.GetAssetPath(entry));
                if (!string.Equals(currentFile, expectedFile, StringComparison.Ordinal))
                {
                    messages.Add($"{label}: filename must be '{expectedFile}.asset'.");
                }
            }

            if (entry is UpgradeNodeDefinition upgrade &&
                !upgrade.TryValidate(out string upgradeError))
            {
                messages.Add($"{label}: {upgradeError}");
            }
            else if (entry is FloatageDefinition floatage &&
                     !floatage.TryValidate(out string floatageError))
            {
                messages.Add($"{label}: {floatageError}");
            }
            else if (entry is StageDefinition stage &&
                     !stage.TryValidate(out string stageError))
            {
                messages.Add($"{label}: {stageError}");
            }
        }

        return messages;
    }

    private void TryAssignIcon(SerializedObject serialized, string key)
    {
        SerializedProperty icon = serialized.FindProperty("_icon");
        if (icon == null || icon.objectReferenceValue != null ||
            string.IsNullOrEmpty(_selectedSheet.IconSearchFolderPath))
        {
            return;
        }

        string expected = _selectedSheet.IconNamePattern.Replace("{key}", key);
        List<Sprite> matches = new();
        string[] guids = AssetDatabase.FindAssets(
            $"{expected} t:Sprite",
            new[] { _selectedSheet.IconSearchFolderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            matches.AddRange(
                AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<Sprite>()
                    .Where(sprite => string.Equals(
                        sprite.name,
                        expected,
                        StringComparison.OrdinalIgnoreCase)));
        }

        if (matches.Count == 1)
        {
            icon.objectReferenceValue = matches[0];
        }
    }

    private IEnumerable<GameDefinition> FindAllDefinitions()
    {
        foreach (Type type in TypeCache.GetTypesDerivedFrom<GameDefinition>()
                     .Where(type => !type.IsAbstract))
        {
            foreach (string guid in AssetDatabase.FindAssets($"t:{type.Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameDefinition definition = AssetDatabase.LoadAssetAtPath(path, type)
                    as GameDefinition;
                if (definition != null)
                {
                    yield return definition;
                }
            }
        }
    }

    private bool MatchesSearch(GameDefinition entry, string query)
    {
        return string.IsNullOrEmpty(query) ||
            entry.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (entry.Key?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
            (entry.Id?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
    }

    private string BuildId(string key)
    {
        return $"{_selectedSheet.CategoryKey}.{key}";
    }

    private string BuildAssetName(int sortOrder, string key)
    {
        string order = sortOrder.ToString($"D{_selectedSheet.OrderDigits}");
        return $"{_selectedSheet.CategoryKey}_{order}_{key}";
    }

    private string GetExpectedAssetName(GameDefinition entry)
    {
        return BuildAssetName(entry.SortOrder, entry.Key);
    }

    private static string SuggestKey(
        string assetName,
        string categoryKey,
        int orderDigits)
    {
        string value = assetName.Trim();
        value = System.Text.RegularExpressions.Regex.Replace(
            value,
            "([a-z0-9])([A-Z])",
            "$1_$2");
        value = value.ToLowerInvariant();
        value = System.Text.RegularExpressions.Regex.Replace(value, "[^a-z0-9]+", "_");
        value = System.Text.RegularExpressions.Regex.Replace(
            value,
            $"^{System.Text.RegularExpressions.Regex.Escape(categoryKey)}_[0-9]{{{orderDigits}}}_",
            string.Empty);
        value = System.Text.RegularExpressions.Regex.Replace(value, "^[a-z]{1,3}_[0-9]+_", string.Empty);
        value = value.Trim('_');
        return string.IsNullOrEmpty(value) || !char.IsLetter(value[0])
            ? $"entry_{value}".TrimEnd('_')
            : value;
    }

    private void SetStatus(string message, MessageType type)
    {
        _statusMessage = message;
        _statusType = type;
        Repaint();
    }

    private static void RollbackRenames(
        IReadOnlyList<RenameOperation> operations,
        ICollection<string> errors)
    {
        foreach (RenameOperation operation in operations)
        {
            string currentPath = AssetDatabase.GetAssetPath(operation.Entry);
            if (string.IsNullOrEmpty(currentPath) ||
                string.Equals(currentPath, operation.OriginalPath, StringComparison.Ordinal))
            {
                continue;
            }

            string rollbackName = $"__sheet_rollback_{Guid.NewGuid():N}";
            string rollbackError = AssetDatabase.RenameAsset(currentPath, rollbackName);
            if (!string.IsNullOrEmpty(rollbackError))
            {
                errors.Add($"Rollback staging failed for '{currentPath}': {rollbackError}");
                continue;
            }

            operation.RollbackPath =
                $"{Path.GetDirectoryName(currentPath)?.Replace('\\', '/')}/{rollbackName}.asset";
        }

        foreach (RenameOperation operation in operations)
        {
            if (string.IsNullOrEmpty(operation.RollbackPath))
            {
                continue;
            }

            string rollbackError = AssetDatabase.RenameAsset(
                operation.RollbackPath,
                Path.GetFileNameWithoutExtension(operation.OriginalPath));
            if (!string.IsNullOrEmpty(rollbackError))
            {
                errors.Add($"Could not restore '{operation.OriginalPath}': {rollbackError}");
            }
        }
    }

    private sealed class RenameOperation
    {
        public RenameOperation(
            GameDefinition entry,
            string originalPath,
            string desiredName,
            string desiredPath)
        {
            Entry = entry;
            OriginalPath = originalPath;
            DesiredName = desiredName;
            DesiredPath = desiredPath;
        }

        public GameDefinition Entry { get; }
        public string OriginalPath { get; }
        public string DesiredName { get; }
        public string DesiredPath { get; }
        public string TempName { get; set; }
        public string TempPath { get; set; }
        public string RollbackPath { get; set; }
    }
}

[CustomEditor(typeof(GameDataSheetDefinition))]
public sealed class GameDataSheetDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GameDataSheetDefinition sheet = (GameDataSheetDefinition)target;
        serializedObject.Update();
        bool identityLocked = HasInitializedEntries(sheet);

        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(identityLocked))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_categoryKey"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_definitionScript"));
        }

        if (identityLocked)
        {
            EditorGUILayout.HelpBox(
                "Category key and definition type are locked because this sheet contains initialized entries.",
                MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Asset Locations", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_assetFolder"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_iconSearchFolder"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_iconNamePattern"));
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Filename", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_orderDigits"));
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (sheet.TryValidate(out string error))
        {
            EditorGUILayout.HelpBox("Sheet configuration is valid.", MessageType.Info);
            if (GUILayout.Button("Open Game Data Workspace"))
            {
                GameDataWorkspaceWindow.Open();
            }
        }
        else
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }
    }

    private static bool HasInitializedEntries(GameDataSheetDefinition sheet)
    {
        if (sheet.DefinitionType == null ||
            string.IsNullOrEmpty(sheet.AssetFolderPath) ||
            !AssetDatabase.IsValidFolder(sheet.AssetFolderPath))
        {
            return false;
        }

        string[] guids = AssetDatabase.FindAssets(
            $"t:{sheet.DefinitionType.Name}",
            new[] { sheet.AssetFolderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameDefinition definition = AssetDatabase.LoadAssetAtPath(
                path,
                sheet.DefinitionType) as GameDefinition;
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Key))
            {
                return true;
            }
        }

        return false;
    }
}
