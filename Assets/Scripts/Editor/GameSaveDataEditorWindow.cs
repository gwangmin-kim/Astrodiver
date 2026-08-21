using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class GameSaveDataEditorWindow : EditorWindow
{
    private const string SaveFileName = "player-save.json";
    private const string DefaultsResourcePath = "GameDataDefaults";

    private SaveDataContainer _container;
    private SerializedObject _serializedContainer;
    private Vector2 _scrollPosition;
    private string _statusMessage;
    private MessageType _statusType = MessageType.Info;
    private DateTime? _lastLoadedWriteTime;
    private string _loadedDataJson;
    private CreatureDefinition[] _creatureDefinitions = Array.Empty<CreatureDefinition>();
    private ResourceDefinition[] _resourceDefinitions = Array.Empty<ResourceDefinition>();
    private UpgradeNodeDefinition[] _upgradeDefinitions = Array.Empty<UpgradeNodeDefinition>();
    // Keep the window safe by default. Opening it should never make a save editable
    // until the user explicitly opts into write mode.
    private bool _isWriteMode;

    private string SaveFilePath =>
        Path.Combine(Application.persistentDataPath, SaveFileName);

    [MenuItem("Astrodiver/Save System/Save Data Editor")]
    public static void Open()
    {
        GetWindow<GameSaveDataEditorWindow>("Save Data");
    }

    private void OnEnable()
    {
        _container = CreateInstance<SaveDataContainer>();
        _container.hideFlags = HideFlags.HideAndDontSave;
        _serializedContainer = new SerializedObject(_container);
        RefreshDefinitionChoices();
        LoadFromDisk();
    }

    private void OnProjectChange()
    {
        RefreshDefinitionChoices();
        Repaint();
    }

    private void OnDisable()
    {
        if (_container != null)
        {
            DestroyImmediate(_container);
        }
    }

    private void OnGUI()
    {
        DrawFileHeader();

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox(
                "플레이 모드에서는 런타임 자동 저장과의 충돌을 막기 위해 읽기 전용으로 표시됩니다.",
                MessageType.Warning);
        }
        else if (!_isWriteMode)
        {
            EditorGUILayout.HelpBox(
                "읽기 모드입니다. 데이터를 수정하거나 파일을 조작하려면 '쓰기 모드'를 켜세요.",
                MessageType.Info);
        }

        EditorGUILayout.Space();
        DrawToolbar();
        EditorGUILayout.Space();
        DrawSaveData();
    }

    private void DrawFileHeader()
    {
        EditorGUILayout.LabelField("Save File", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.SelectableLabel(
                SaveFilePath,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            if (GUILayout.Button("폴더 열기", GUILayout.Width(80f)))
            {
                EditorUtility.RevealInFinder(
                    File.Exists(SaveFilePath)
                        ? SaveFilePath
                        : Application.persistentDataPath);
            }
        }

        string fileState = File.Exists(SaveFilePath)
            ? $"존재함 · {File.GetLastWriteTime(SaveFilePath):yyyy-MM-dd HH:mm:ss}"
            : "파일 없음";
        EditorGUILayout.LabelField("상태", fileState);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            _isWriteMode = EditorGUILayout.ToggleLeft(
                "쓰기 모드 (세이브 데이터 변경 허용)",
                _isWriteMode);
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("다시 읽기"))
            {
                ReloadWithUnsavedChangesCheck();
            }

            using (new EditorGUI.DisabledScope(!CanWrite))
            {
                if (GUILayout.Button("변경사항 저장"))
                {
                    SaveToDisk();
                }

                if (GUILayout.Button("기본값으로 초기화"))
                {
                    ResetToDefaults();
                }

                GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
                if (GUILayout.Button("세이브 파일 삭제"))
                {
                    DeleteSaveFiles();
                }

                GUI.backgroundColor = Color.white;
            }
        }
    }

    private void DrawSaveData()
    {
        if (_serializedContainer == null)
        {
            return;
        }

        _serializedContainer.Update();
        SerializedProperty dataProperty =
            _serializedContainer.FindProperty(nameof(SaveDataContainer.data));

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        if (CanWrite)
        {
            DrawEditableSaveData(dataProperty);
        }
        else
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(dataProperty, true);
            }
        }
        EditorGUILayout.EndScrollView();
        _serializedContainer.ApplyModifiedProperties();
    }

    private void DrawEditableSaveData(SerializedProperty data)
    {
        data.isExpanded = EditorGUILayout.Foldout(data.isExpanded, "Save Data", true);
        if (!data.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        SerializedProperty schemaVersion = data.FindPropertyRelative("schemaVersion");
        schemaVersion.intValue = EditorGUILayout.IntField("Schema Version", schemaVersion.intValue);

        DrawInventory(
            data.FindPropertyRelative("inventory"),
            "Player Inventory",
            allowsCreatures: true,
            allowsResources: true);
        DrawInventory(
            data.FindPropertyRelative("resourceChest"),
            "Resource Chest",
            allowsCreatures: false,
            allowsResources: true);
        DrawWorktable(data.FindPropertyRelative("worktable"));
        DrawUpgradeNodes(data.FindPropertyRelative("upgradeNodes"));
        DrawCompletedEvents(data.FindPropertyRelative("completedEvents"));
        EditorGUI.indentLevel--;
    }

    private void DrawInventory(
        SerializedProperty inventory,
        string label,
        bool allowsCreatures,
        bool allowsResources)
    {
        inventory.isExpanded = EditorGUILayout.Foldout(inventory.isExpanded, label, true);
        if (!inventory.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        DrawInventoryEntries(
            inventory.FindPropertyRelative("_creatures"),
            "Creatures",
            "Creature",
            _creatureDefinitions,
            allowsCreatures);
        DrawInventoryEntries(
            inventory.FindPropertyRelative("_resourceAmounts"),
            "Resources",
            "Resource",
            _resourceDefinitions,
            allowsResources);
        EditorGUI.indentLevel--;
    }

    private static void DrawInventoryEntries<T>(
        SerializedProperty entries,
        string label,
        string entryLabel,
        IReadOnlyList<T> definitions,
        bool canAdd)
        where T : GameDefinition
    {
        entries.isExpanded = EditorGUILayout.Foldout(
            entries.isExpanded,
            $"{label} ({entries.arraySize})",
            true);
        if (!entries.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        for (int index = 0; index < entries.arraySize; index++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            SerializedProperty definitionId = entry.FindPropertyRelative("_definitionId");
            SerializedProperty amount = entry.FindPropertyRelative(
                entryLabel == "Creature" ? "_count" : "_amount");

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawDefinitionPopup(
                    $"{entryLabel} {index + 1}",
                    definitionId,
                    definitions);
                amount.intValue = EditorGUILayout.IntField(amount.intValue, GUILayout.Width(70f));

                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    entries.DeleteArrayElementAtIndex(index);
                    index--;
                }
            }
        }

        if (canAdd && GUILayout.Button($"Add {entryLabel}"))
        {
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("_definitionId").stringValue = string.Empty;
            entry.FindPropertyRelative(
                entryLabel == "Creature" ? "_count" : "_amount").intValue = 1;
        }

        EditorGUI.indentLevel--;
    }

    private void DrawWorktable(SerializedProperty worktable)
    {
        worktable.isExpanded = EditorGUILayout.Foldout(
            worktable.isExpanded,
            "Worktable",
            true);
        if (!worktable.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        DrawInventory(
            worktable.FindPropertyRelative("_inventory"),
            "Inventory",
            allowsCreatures: true,
            allowsResources: false);

        SerializedProperty processingCreatureId =
            worktable.FindPropertyRelative("_processingCreatureId");
        SerializedProperty remainingSeconds =
            worktable.FindPropertyRelative("_remainingBaseProcessSeconds");
        DrawDefinitionPopup(
            "Processing Creature ID",
            processingCreatureId,
            _creatureDefinitions);
        remainingSeconds.floatValue = EditorGUILayout.FloatField(
            "Remaining Base Process Seconds",
            remainingSeconds.floatValue);
        EditorGUI.indentLevel--;
    }

    private void DrawUpgradeNodes(SerializedProperty upgradeNodes)
    {
        upgradeNodes.isExpanded = EditorGUILayout.Foldout(
            upgradeNodes.isExpanded,
            $"Upgrade Nodes ({upgradeNodes.arraySize})",
            true);
        if (!upgradeNodes.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        for (int index = 0; index < upgradeNodes.arraySize; index++)
        {
            SerializedProperty node = upgradeNodes.GetArrayElementAtIndex(index);
            SerializedProperty nodeId = node.FindPropertyRelative("nodeId");
            SerializedProperty level = node.FindPropertyRelative("level");

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawDefinitionPopup(
                    $"Node {index + 1}",
                    nodeId,
                    _upgradeDefinitions);
                level.intValue = EditorGUILayout.IntField(level.intValue, GUILayout.Width(70f));

                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    upgradeNodes.DeleteArrayElementAtIndex(index);
                    index--;
                }
            }
        }

        if (GUILayout.Button("Add Upgrade Node"))
        {
            int index = upgradeNodes.arraySize;
            upgradeNodes.InsertArrayElementAtIndex(index);
            SerializedProperty node = upgradeNodes.GetArrayElementAtIndex(index);
            node.FindPropertyRelative("nodeId").stringValue = string.Empty;
            node.FindPropertyRelative("level").intValue = 1;
        }

        EditorGUI.indentLevel--;
    }

    private static void DrawCompletedEvents(SerializedProperty completedEvents)
    {
        completedEvents.isExpanded = EditorGUILayout.Foldout(
            completedEvents.isExpanded,
            $"Completed Events ({completedEvents.arraySize})",
            true);
        if (!completedEvents.isExpanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        GameProgressEventId[] eventIds =
            (GameProgressEventId[])Enum.GetValues(typeof(GameProgressEventId));
        for (int index = 0; index < completedEvents.arraySize; index++)
        {
            SerializedProperty eventId = completedEvents.GetArrayElementAtIndex(index);
            using (new EditorGUILayout.HorizontalScope())
            {
                int currentIndex = Mathf.Clamp(
                    eventId.enumValueIndex,
                    0,
                    eventIds.Length - 1);
                GameProgressEventId selectedId = (GameProgressEventId)EditorGUILayout.EnumPopup(
                    $"Event {index + 1}",
                    eventIds[currentIndex]);
                eventId.enumValueIndex = Array.IndexOf(eventIds, selectedId);

                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    completedEvents.DeleteArrayElementAtIndex(index);
                    index--;
                }
            }
        }

        if (GUILayout.Button("Add Completed Event"))
        {
            int index = completedEvents.arraySize;
            completedEvents.InsertArrayElementAtIndex(index);
            completedEvents.GetArrayElementAtIndex(index).enumValueIndex = 0;
        }

        EditorGUI.indentLevel--;
    }

    private void RefreshDefinitionChoices()
    {
        _creatureDefinitions = FindDefinitionAssets<CreatureDefinition>();
        _resourceDefinitions = FindDefinitionAssets<ResourceDefinition>();
        _upgradeDefinitions = FindDefinitionAssets<UpgradeNodeDefinition>();
    }

    private static T[] FindDefinitionAssets<T>() where T : GameDefinition
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        List<T> definitions = new(guids.Length);
        for (int index = 0; index < guids.Length; index++)
        {
            T definition = AssetDatabase.LoadAssetAtPath<T>(
                AssetDatabase.GUIDToAssetPath(guids[index]));
            if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
            {
                definitions.Add(definition);
            }
        }

        definitions.Sort((left, right) =>
        {
            int sortOrder = left.SortOrder.CompareTo(right.SortOrder);
            return sortOrder != 0
                ? sortOrder
                : string.Compare(left.name, right.name, StringComparison.Ordinal);
        });
        return definitions.ToArray();
    }

    private static void DrawDefinitionPopup<T>(
        string label,
        SerializedProperty definitionId,
        IReadOnlyList<T> definitions)
        where T : GameDefinition
    {
        int selectedDefinitionIndex = -1;
        for (int index = 0; index < definitions.Count; index++)
        {
            if (string.Equals(
                    definitions[index].Id,
                    definitionId.stringValue,
                    StringComparison.Ordinal))
            {
                selectedDefinitionIndex = index;
                break;
            }
        }

        string[] options = new string[definitions.Count + 1];
        options[0] = string.IsNullOrEmpty(definitionId.stringValue)
            ? "<Select Definition>"
            : $"<Missing: {definitionId.stringValue}>";
        for (int index = 0; index < definitions.Count; index++)
        {
            T definition = definitions[index];
            options[index + 1] = $"{definition.name} ({definition.Id})";
        }

        int currentIndex = selectedDefinitionIndex + 1;
        int selectedIndex = EditorGUILayout.Popup(label, currentIndex, options);
        if (selectedIndex > 0)
        {
            definitionId.stringValue = definitions[selectedIndex - 1].Id;
        }
        else if (currentIndex > 0)
        {
            definitionId.stringValue = string.Empty;
        }
    }

    private void ReloadWithUnsavedChangesCheck()
    {
        _serializedContainer.ApplyModifiedProperties();
        if (HasUnsavedChanges() &&
            !EditorUtility.DisplayDialog(
                "변경사항 버리기",
                "저장하지 않은 변경사항을 버리고 파일을 다시 읽을까요?",
                "다시 읽기",
                "취소"))
        {
            return;
        }

        LoadFromDisk();
    }

    private void LoadFromDisk()
    {
        if (GameDataFileStore.TryLoad(
                SaveFilePath,
                out GameSaveData loadedData,
                out string error))
        {
            SetData(loadedData);
            RecordCurrentDataAsLoaded();
            _lastLoadedWriteTime = File.Exists(SaveFilePath)
                ? File.GetLastWriteTimeUtc(SaveFilePath)
                : null;
            SetStatus("세이브 데이터를 읽었습니다.", MessageType.Info);
            return;
        }

        SetData(CreateDefaultData());
        RecordCurrentDataAsLoaded();
        _lastLoadedWriteTime = null;

        if (string.Equals(error, "Save file does not exist.", StringComparison.Ordinal))
        {
            SetStatus(
                "세이브 파일이 없어 기본값을 표시합니다. '변경사항 저장'을 누르면 새 파일을 만듭니다.",
                MessageType.Info);
        }
        else
        {
            SetStatus(
                $"세이브 파일을 읽지 못해 기본값을 표시합니다.\n{error}",
                MessageType.Error);
        }
    }

    private void SaveToDisk()
    {
        if (!CanWrite)
        {
            SetStatus("쓰기 모드에서만 세이브 파일을 저장할 수 있습니다.", MessageType.Warning);
            return;
        }

        if (HasFileChangedSinceLoad() &&
            !EditorUtility.DisplayDialog(
                "파일이 변경됨",
                "이 창에서 읽은 뒤 세이브 파일이 외부에서 변경되었습니다. 덮어쓸까요?",
                "덮어쓰기",
                "취소"))
        {
            return;
        }

        _serializedContainer.ApplyModifiedProperties();
        _container.data ??= new GameSaveData();
        _container.data.RepairAfterLoad();

        if (!GameDataFileStore.TrySave(SaveFilePath, _container.data, out string error))
        {
            SetStatus($"저장하지 못했습니다.\n{error}", MessageType.Error);
            return;
        }

        SetData(_container.data);
        RecordCurrentDataAsLoaded();
        _lastLoadedWriteTime = File.GetLastWriteTimeUtc(SaveFilePath);
        SetStatus("변경사항을 세이브 파일에 저장했습니다.", MessageType.Info);
    }

    private void ResetToDefaults()
    {
        if (!CanWrite)
        {
            SetStatus("쓰기 모드에서만 세이브 데이터를 초기화할 수 있습니다.", MessageType.Warning);
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "세이브 데이터 초기화",
                "현재 세이브 데이터를 프로젝트 기본값으로 바꾸고 즉시 저장할까요?",
                "초기화",
                "취소"))
        {
            return;
        }

        SetData(CreateDefaultData());
        SaveToDisk();
    }

    private void DeleteSaveFiles()
    {
        if (!CanWrite)
        {
            SetStatus("쓰기 모드에서만 세이브 파일을 삭제할 수 있습니다.", MessageType.Warning);
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "세이브 파일 삭제",
                "기본 세이브와 백업/임시 파일을 모두 삭제합니다. 이 작업은 되돌릴 수 없습니다.",
                "모두 삭제",
                "취소"))
        {
            return;
        }

        try
        {
            DeleteIfExists(SaveFilePath);
            DeleteIfExists(SaveFilePath + ".bak");
            DeleteIfExists(SaveFilePath + ".tmp");
            SetData(CreateDefaultData());
            RecordCurrentDataAsLoaded();
            _lastLoadedWriteTime = null;
            SetStatus(
                "세이브 파일을 삭제했습니다. 화면에는 아직 저장되지 않은 기본값이 표시됩니다.",
                MessageType.Info);
        }
        catch (Exception exception)
        {
            SetStatus($"파일을 삭제하지 못했습니다.\n{exception}", MessageType.Error);
        }
    }

    private void SetData(GameSaveData data)
    {
        _container.data = data ?? new GameSaveData();
        _container.data.RepairAfterLoad();
        _serializedContainer = new SerializedObject(_container);
        Repaint();
    }

    private static GameSaveData CreateDefaultData()
    {
        GameDataDefaults defaults =
            Resources.Load<GameDataDefaults>(DefaultsResourcePath);
        return defaults != null ? defaults.CreateSaveData() : new GameSaveData();
    }

    private bool HasFileChangedSinceLoad()
    {
        if (!File.Exists(SaveFilePath))
        {
            return _lastLoadedWriteTime.HasValue;
        }

        return !_lastLoadedWriteTime.HasValue ||
               File.GetLastWriteTimeUtc(SaveFilePath) != _lastLoadedWriteTime.Value;
    }

    private bool CanWrite =>
        _isWriteMode && !EditorApplication.isPlayingOrWillChangePlaymode;

    private bool HasUnsavedChanges()
    {
        return !string.Equals(
            JsonUtility.ToJson(_container.data),
            _loadedDataJson,
            StringComparison.Ordinal);
    }

    private void RecordCurrentDataAsLoaded()
    {
        _loadedDataJson = JsonUtility.ToJson(_container.data);
    }

    private void SetStatus(string message, MessageType type)
    {
        _statusMessage = message;
        _statusType = type;
        Repaint();
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private sealed class SaveDataContainer : ScriptableObject
    {
        public GameSaveData data = new();
    }
}
