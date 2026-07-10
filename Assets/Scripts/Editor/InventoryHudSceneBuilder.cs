#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class InventoryHudSceneBuilder
{
    private const string HudName = "Inventory HUD";
    private const string CreatureBarName = "Creature Inventory Bar";
    private const string ResourceListName = "Resource Fragment List";
    private const string CreatureSlotPrefabPath = "Assets/Prefabs/UI/CreatureInventorySlot.prefab";
    private const float CreatureSlotSize = 64f;
    private const float CreatureSlotSpacing = 8f;

    [MenuItem("Astrodiver/UI/Rebuild Inventory HUD")]
    public static void RebuildInventoryHud()
    {
        GameObject hud = FindOrCreateHudRoot();
        EnsureRectTransform(hud);

        Canvas canvas = EnsureComponent<Canvas>(hud);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = EnsureComponent<CanvasScaler>(hud);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureComponent<GraphicRaycaster>(hud);
        InventoryHudUI hudUi = EnsureComponent<InventoryHudUI>(hud);
        SerializedObject serializedHud = new(hudUi);
        serializedHud.FindProperty("_playerInventory").objectReferenceValue = Object.FindAnyObjectByType<PlayerInventoryController>();
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        BuildCreatureInventoryBar(hud.transform);
        BuildResourceFragmentList(hud.transform);
        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(hud.scene);
        Selection.activeGameObject = hud;
    }

    private static void BuildCreatureInventoryBar(Transform parent)
    {
        CreatureInventorySlotUI slotPrefab = GetOrCreateCreatureSlotPrefab();
        PlayerInventoryController playerInventory = Object.FindAnyObjectByType<PlayerInventoryController>();

        GameObject barObject = FindOrCreateChild(parent, CreatureBarName);
        RectTransform rectTransform = EnsureRectTransform(barObject);
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, 24f);
        rectTransform.sizeDelta = new Vector2(0f, CreatureSlotSize);

        GridLayoutGroup layoutGroup = EnsureComponent<GridLayoutGroup>(barObject);
        layoutGroup.cellSize = new Vector2(CreatureSlotSize, CreatureSlotSize);
        layoutGroup.spacing = new Vector2(CreatureSlotSpacing, 0f);
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
        layoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        layoutGroup.constraintCount = 1;

        CreatureInventoryBarUI bar = EnsureComponent<CreatureInventoryBarUI>(barObject);
        SerializedObject serializedBar = new(bar);
        serializedBar.FindProperty("_playerInventory").objectReferenceValue = playerInventory;
        serializedBar.FindProperty("_slotPrefab").objectReferenceValue = slotPrefab;
        serializedBar.ApplyModifiedPropertiesWithoutUndo();

        for (int i = barObject.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(barObject.transform.GetChild(i).gameObject);
        }

        int slotCount = playerInventory != null && playerInventory.CreatureSlots != null
            ? playerInventory.CreatureSlots.Count
            : 0;

        rectTransform.sizeDelta = new Vector2(
            slotCount * CreatureSlotSize + Mathf.Max(0, slotCount - 1) * CreatureSlotSpacing,
            CreatureSlotSize);

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject = InstantiateSlotPrefab(slotPrefab, barObject.transform, $"Creature Slot {i + 1:00}");
            CreatureInventorySlotUI slot = EnsureComponent<CreatureInventorySlotUI>(slotObject);
            slot.SetEmpty();
        }
    }

    private static void BuildResourceFragmentList(Transform parent)
    {
        GameObject listObject = FindOrCreateChild(parent, ResourceListName);
        RectTransform rectTransform = EnsureRectTransform(listObject);
        rectTransform.anchorMin = new Vector2(1f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(1f, 0f);
        rectTransform.anchoredPosition = new Vector2(-32f, 24f);
        rectTransform.sizeDelta = new Vector2(190f, 240f);

        VerticalLayoutGroup layoutGroup = EnsureComponent<VerticalLayoutGroup>(listObject);
        layoutGroup.childAlignment = TextAnchor.LowerRight;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.spacing = 4f;

        EnsureComponent<ResourceFragmentListUI>(listObject);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new("EventSystem");
        EnsureComponent<EventSystem>(eventSystem);
        EnsureComponent<InputSystemUIInputModule>(eventSystem);
        EditorSceneManager.MarkSceneDirty(eventSystem.scene);
    }

    private static GameObject FindOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null) return child.gameObject;

        GameObject childObject = new(childName, typeof(RectTransform));
        childObject.transform.SetParent(parent, false);
        return childObject;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static GameObject FindOrCreateHudRoot()
    {
        GameObject hud = GameObject.Find(HudName);
        if (hud != null && hud.GetComponent<Canvas>() == null && hud.transform.childCount == 0)
        {
            Object.DestroyImmediate(hud);
            hud = null;
        }

        return hud != null ? hud : new GameObject(HudName, typeof(RectTransform));
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        RectTransform rectTransform = target.GetComponent<RectTransform>();
        return rectTransform != null ? rectTransform : target.AddComponent<RectTransform>();
    }

    private static CreatureInventorySlotUI GetOrCreateCreatureSlotPrefab()
    {
        CreatureInventorySlotUI prefab = AssetDatabase.LoadAssetAtPath<CreatureInventorySlotUI>(CreatureSlotPrefabPath);
        if (prefab != null) return prefab;

        Directory.CreateDirectory(Path.GetDirectoryName(CreatureSlotPrefabPath));

        GameObject slotObject = new("CreatureInventorySlot", typeof(RectTransform));
        CreatureInventorySlotUI slot = EnsureComponent<CreatureInventorySlotUI>(slotObject);
        slot.SetEmpty();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(slotObject, CreatureSlotPrefabPath);
        Object.DestroyImmediate(slotObject);
        return savedPrefab.GetComponent<CreatureInventorySlotUI>();
    }

    private static GameObject InstantiateSlotPrefab(CreatureInventorySlotUI slotPrefab, Transform parent, string slotName)
    {
        GameObject slotObject = slotPrefab != null
            ? PrefabUtility.InstantiatePrefab(slotPrefab.gameObject, parent) as GameObject
            : null;

        if (slotObject == null)
        {
            slotObject = new GameObject(slotName, typeof(RectTransform));
            slotObject.transform.SetParent(parent, false);
        }

        slotObject.name = slotName;
        return slotObject;
    }
}
#endif
