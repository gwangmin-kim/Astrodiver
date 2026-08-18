#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string RegularFontPath = "Assets/Arts/99_Fonts/PFStardust/PFStardust-3.0-Regular.ttf";
    private const string BoldFontPath = "Assets/Arts/99_Fonts/PFStardust/PFStardust-3.0-Bold.ttf";
    private const string ExtraBoldFontPath = "Assets/Arts/99_Fonts/PFStardust/PFStardust-3.0-ExtraBold.ttf";
    private const string FontAssetFolder = "Assets/Arts/99_Fonts/PFStardust/TMP";
    private const string RegularFontAssetPath = FontAssetFolder + "/PFStardust-Regular SDF.asset";
    private const string BoldFontAssetPath = FontAssetFolder + "/PFStardust-Bold SDF.asset";
    private const string ExtraBoldFontAssetPath = FontAssetFolder + "/PFStardust-ExtraBold SDF.asset";

    [MenuItem("Astrodiver/UI/Rebuild Main Menu Scene")]
    public static void Rebuild()
    {
        TMP_FontAsset regularFont = GetOrCreateFontAsset(RegularFontPath, RegularFontAssetPath);
        TMP_FontAsset extraBoldFont = GetOrCreateFontAsset(ExtraBoldFontPath, ExtraBoldFontAssetPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();

        GameObject canvasObject = new("Main Menu Canvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreateTitle(canvasObject.transform, extraBoldFont);

        GameObject menuLayout = CreateMenuLayout(canvasObject.transform);
        Button newGameButton = CreateTextButton(menuLayout.transform, "New Game Button", "새 게임", regularFont);
        Button continueButton = CreateTextButton(menuLayout.transform, "Continue Button", "이어하기", regularFont);
        Button quitButton = CreateTextButton(menuLayout.transform, "Quit Button", "게임 종료", regularFont);

        TMP_Text continueLabel = continueButton.GetComponentInChildren<TMP_Text>();
        TMP_Text quitLabel = quitButton.GetComponentInChildren<TMP_Text>();

        MainMenuController controller = canvasObject.AddComponent<MainMenuController>();
        SerializedObject serializedController = new(controller);
        serializedController.FindProperty("_newGameButton").objectReferenceValue = newGameButton;
        serializedController.FindProperty("_continueButton").objectReferenceValue = continueButton;
        serializedController.FindProperty("_quitButton").objectReferenceValue = quitButton;
        serializedController.FindProperty("_continueLabel").objectReferenceValue = continueLabel;
        serializedController.FindProperty("_quitLabel").objectReferenceValue = quitLabel;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        QuitButtonFocusHandler quitFocusHandler =
            quitButton.gameObject.AddComponent<QuitButtonFocusHandler>();
        SerializedObject serializedFocusHandler = new(quitFocusHandler);
        serializedFocusHandler.FindProperty("_mainMenuController").objectReferenceValue = controller;
        serializedFocusHandler.ApplyModifiedPropertiesWithoutUndo();

        CreateEventSystem();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureMainMenuIsFirstBuildScene();
        Selection.activeGameObject = canvasObject;
        Debug.Log($"Main menu scene rebuilt at {ScenePath}");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.035f, 0.075f, 1f);
        camera.orthographic = true;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateTitle(Transform parent, TMP_FontAsset font)
    {
        GameObject titleObject = new("Game Title", typeof(RectTransform));
        titleObject.transform.SetParent(parent, false);
        RectTransform rect = titleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -110f);
        rect.sizeDelta = new Vector2(900f, 140f);

        TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
        title.text = "Astrodiver";
        title.font = font;
        title.fontSize = 76f;
        title.fontStyle = FontStyles.Bold;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.92f, 0.95f, 1f, 1f);
        title.raycastTarget = false;
    }

    private static GameObject CreateMenuLayout(Transform parent)
    {
        GameObject layoutObject = new("Menu Buttons", typeof(RectTransform));
        layoutObject.transform.SetParent(parent, false);
        RectTransform rect = layoutObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -30f);
        rect.sizeDelta = new Vector2(640f, 280f);

        VerticalLayoutGroup layout = layoutObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 24f;
        return layoutObject;
    }

    private static Button CreateTextButton(
        Transform parent,
        string objectName,
        string labelText,
        TMP_FontAsset font)
    {
        GameObject buttonObject = new(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 72f;
        layoutElement.minHeight = 72f;

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = null;

        GameObject labelObject = new("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.font = font;
        label.fontSize = 36f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = true;
        return button;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static TMP_FontAsset GetOrCreateFontAsset(string sourcePath, string assetPath)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null && existing.atlasTexture != null && existing.material != null)
        {
            return existing;
        }

        if (existing != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (sourceFont == null)
        {
            throw new FileNotFoundException($"PF Stardust font was not found at {sourcePath}");
        }

        if (!AssetDatabase.IsValidFolder(FontAssetFolder))
        {
            AssetDatabase.CreateFolder("Assets/Arts/99_Fonts/PFStardust", "TMP");
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic,
            true);
        fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;

        Texture2D atlasTexture = fontAsset.atlasTexture;
        Material material = fontAsset.material;
        atlasTexture.name = fontAsset.name + " Atlas";
        material.name = fontAsset.name + " Material";

        AssetDatabase.CreateAsset(fontAsset, assetPath);
        AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
        AssetDatabase.AddObjectToAsset(material, fontAsset);
        fontAsset.atlasTextures = new[] { atlasTexture };
        fontAsset.material = material;
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    private static void EnsureMainMenuIsFirstBuildScene()
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        System.Collections.Generic.List<EditorBuildSettingsScene> result = new();
        result.Add(new EditorBuildSettingsScene(ScenePath, true));

        for (int i = 0; i < currentScenes.Length; i++)
        {
            if (!string.Equals(currentScenes[i].path, ScenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                result.Add(currentScenes[i]);
            }
        }

        EditorBuildSettings.scenes = result.ToArray();
    }
}
#endif
