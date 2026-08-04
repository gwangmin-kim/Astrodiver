using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StageHudSceneBuilder
{
    private const string HubScenePath = "Assets/Scenes/Hub.unity";
    private const string StageOneScenePath = "Assets/Scenes/Stage_1_1.unity";
    private const string StageTwoScenePath = "Assets/Scenes/Stage_1_2.unity";
    private const string StageThreeScenePath = "Assets/Scenes/Stage_1_3.unity";

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

        GameObject viewport = CreateUiObject("Viewport", stageHud.transform);
        RectTransform viewportRect = (RectTransform)viewport.transform;
        Stretch(viewportRect);
        Undo.AddComponent<RectMask2D>(viewport);

        GameObject mapBackground = CreateUiObject("MapContent", viewport.transform);
        RectTransform mapRect = (RectTransform)mapBackground.transform;
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.anchoredPosition = Vector2.zero;
        mapRect.sizeDelta = new Vector2(1280f, 720f);
        Image mapImage = Undo.AddComponent<Image>(mapBackground);
        mapImage.color = new Color(0.055f, 0.11f, 0.18f, 1f);

        Button stageOneButton = CreateStageButton(
            "Stage_1_1_Button",
            mapBackground.transform,
            new Vector2(-285f, 90f),
            new Vector2(270f, 220f),
            new Color(0.12f, 0.65f, 0.82f, 1f));
        Button stageTwoButton = CreateStageButton(
            "Stage_1_2_Button",
            mapBackground.transform,
            new Vector2(290f, -100f),
            new Vector2(250f, 250f),
            new Color(0.86f, 0.44f, 0.16f, 1f));
        Button stageThreeButton = CreateStageButton(
            "Stage_1_3_Button",
            mapBackground.transform,
            new Vector2(981f, 68f),
            new Vector2(200f, 200f),
            new Color(0.081960805f, 0.64114475f, 0.81960785f, 1f));

        EnsureBuildScene(HubScenePath);
        EnsureBuildScene(StageOneScenePath);
        EnsureBuildScene(StageTwoScenePath);
        EnsureBuildScene(StageThreeScenePath);

        StageSelectionUI selectionUi = Undo.AddComponent<StageSelectionUI>(stageHud);
        SerializedObject selectionSerialized = new(selectionUi);
        selectionSerialized.FindProperty("_playerInput").objectReferenceValue = playerInput;
        selectionSerialized.FindProperty("_uiInput").objectReferenceValue = uiInput;
        SerializedProperty destinations = selectionSerialized.FindProperty("_destinations");
        destinations.arraySize = 3;
        ConfigureDestination(
            destinations.GetArrayElementAtIndex(0),
            stageOneButton,
            StageOneScenePath);
        ConfigureDestination(
            destinations.GetArrayElementAtIndex(1),
            stageTwoButton,
            StageTwoScenePath);
        ConfigureDestination(
            destinations.GetArrayElementAtIndex(2),
            stageThreeButton,
            StageThreeScenePath);
        selectionSerialized.ApplyModifiedPropertiesWithoutUndo();

        StageMapNavigationUI navigationUi = Undo.AddComponent<StageMapNavigationUI>(stageHud);
        SerializedObject navigationSerialized = new(navigationUi);
        navigationSerialized.FindProperty("_input").objectReferenceValue = uiInput;
        navigationSerialized.FindProperty("_viewport").objectReferenceValue = viewportRect;
        navigationSerialized.FindProperty("_mapContent").objectReferenceValue = mapRect;
        navigationSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject gateSerialized = new(gate);
        gateSerialized.FindProperty("_stageSelectionUI").objectReferenceValue = selectionUi;
        gateSerialized.FindProperty("_destinationSceneName").stringValue = string.Empty;
        gateSerialized.ApplyModifiedPropertiesWithoutUndo();

        stageHud.SetActive(false);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = stageHud;

        Debug.Log(
            "Stage HUD rebuilt with Stage_1_1 through Stage_1_3 destinations and map navigation.",
            stageHud);
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
        string scenePath)
    {
        destination.FindPropertyRelative("_button").objectReferenceValue = button;
        SerializedProperty scene = destination.FindPropertyRelative("_scene");
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        scene.FindPropertyRelative("_sceneAsset").objectReferenceValue = sceneAsset;
        scene.FindPropertyRelative("_buildIndex").intValue =
            GetEnabledBuildIndex(scenePath);
    }

    private static int GetEnabledBuildIndex(string scenePath)
    {
        int buildIndex = 0;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled)
            {
                continue;
            }

            if (scene.path == scenePath)
            {
                return buildIndex;
            }

            buildIndex++;
        }

        return -1;
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
