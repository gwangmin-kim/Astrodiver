using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StageHudSceneBuilder
{
    private const string HubScenePath = "Assets/Scenes/Hub.unity";
    private const string StageOneScenePath = "Assets/Scenes/Stage_1.unity";
    private const string StageTwoScenePath = "Assets/Scenes/Stage_2.unity";

    [MenuItem("Astrodiver/UI/Rebuild Stage HUD")]
    public static void Rebuild()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != HubScenePath)
        {
            Debug.LogError("Open the Hub scene before rebuilding Stage HUD.");
            return;
        }

        Canvas hubHud = FindSceneComponent<Canvas>(activeScene, "HubHUD");
        SceneGate gate = FindSceneComponent<SceneGate>(activeScene, "Gate");
        PlayerInputHandler playerInput = Object.FindAnyObjectByType<PlayerInputHandler>();
        UIInputHandler uiInput = hubHud != null
            ? hubHud.GetComponent<UIInputHandler>()
            : null;

        if (hubHud == null || gate == null || playerInput == null || uiInput == null)
        {
            Debug.LogError(
                "Stage HUD requires HubHUD, Gate, PlayerInputHandler, and UIInputHandler in the Hub scene.");
            return;
        }

        Undo.SetCurrentGroupName("Rebuild Stage HUD");
        int undoGroup = Undo.GetCurrentGroup();

        Transform existing = hubHud.transform.Find("StageHUD");
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        GameObject stageHud = CreateUiObject("StageHUD", hubHud.transform);
        Stretch((RectTransform)stageHud.transform);
        Image dimBackground = Undo.AddComponent<Image>(stageHud);
        dimBackground.color = new Color(0.015f, 0.025f, 0.055f, 0.92f);

        GameObject mapBackground = CreateUiObject("MapBackground", stageHud.transform);
        RectTransform mapRect = (RectTransform)mapBackground.transform;
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.anchoredPosition = Vector2.zero;
        mapRect.sizeDelta = new Vector2(1280f, 720f);
        Image mapImage = Undo.AddComponent<Image>(mapBackground);
        mapImage.color = new Color(0.055f, 0.11f, 0.18f, 1f);

        Button stageOneButton = CreateStageButton(
            "Stage_1_Button",
            mapBackground.transform,
            new Vector2(-285f, 90f),
            new Vector2(270f, 220f),
            new Color(0.12f, 0.65f, 0.82f, 1f));
        Button stageTwoButton = CreateStageButton(
            "Stage_2_Button",
            mapBackground.transform,
            new Vector2(290f, -100f),
            new Vector2(250f, 250f),
            new Color(0.86f, 0.44f, 0.16f, 1f));

        StageSelectionUI selectionUi = Undo.AddComponent<StageSelectionUI>(stageHud);
        SerializedObject selectionSerialized = new(selectionUi);
        selectionSerialized.FindProperty("_playerInput").objectReferenceValue = playerInput;
        selectionSerialized.FindProperty("_uiInput").objectReferenceValue = uiInput;
        SerializedProperty destinations = selectionSerialized.FindProperty("_destinations");
        destinations.arraySize = 2;
        ConfigureDestination(destinations.GetArrayElementAtIndex(0), stageOneButton, "Stage_1");
        ConfigureDestination(destinations.GetArrayElementAtIndex(1), stageTwoButton, "Stage_2");
        selectionSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject gateSerialized = new(gate);
        gateSerialized.FindProperty("_stageSelectionUI").objectReferenceValue = selectionUi;
        gateSerialized.FindProperty("_destinationSceneName").stringValue = string.Empty;
        gateSerialized.ApplyModifiedPropertiesWithoutUndo();

        stageHud.SetActive(false);
        EnsureBuildScene(HubScenePath);
        EnsureBuildScene(StageOneScenePath);
        EnsureBuildScene(StageTwoScenePath);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = stageHud;

        Debug.Log("Stage HUD rebuilt with Stage_1 and Stage_2 destinations.", stageHud);
    }

    private static Button CreateStageButton(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = Undo.AddComponent<Image>(buttonObject);
        image.color = color;

        Button button = Undo.AddComponent<Button>(buttonObject);
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.55f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        return button;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void ConfigureDestination(
        SerializedProperty destination,
        Button button,
        string sceneName)
    {
        destination.FindPropertyRelative("_button").objectReferenceValue = button;
        destination.FindPropertyRelative("_sceneName").stringValue = sceneName;
    }

    private static T FindSceneComponent<T>(Scene scene, string objectName)
        where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform target = root.transform.name == objectName
                ? root.transform
                : FindDescendant(root.transform, objectName);
            if (target != null && target.TryGetComponent(out T component))
            {
                return component;
            }
        }

        return null;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
            {
                return child;
            }

            Transform match = FindDescendant(child, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void EnsureBuildScene(string scenePath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            Debug.LogError($"Cannot add missing scene '{scenePath}' to Build Settings.");
            return;
        }

        List<EditorBuildSettingsScene> scenes =
            new(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                }

                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
