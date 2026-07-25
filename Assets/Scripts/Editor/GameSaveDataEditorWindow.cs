using System;
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
        LoadFromDisk();
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
                "플레이 모드에서는 런타임 자동 저장과의 충돌을 막기 위해 저장, 초기화, 삭제가 비활성화됩니다.",
                MessageType.Warning);
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
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("다시 읽기"))
            {
                ReloadWithUnsavedChangesCheck();
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
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
        EditorGUILayout.PropertyField(dataProperty, true);
        EditorGUILayout.EndScrollView();
        _serializedContainer.ApplyModifiedProperties();
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
