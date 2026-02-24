using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PerkSlots3DOverlaySetup
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string SlotPrefabPath = "Assets/Prefabs/UI3D/PerkCell3D.prefab";

    [MenuItem("Tools/PSG-Ball/Apply 3D Perk Slots To Game Scene")]
    public static void ApplyFromMenu() => ApplyToGameScene();

    public static void ApplyToGameScene() => ApplyInternal();
    public static void ApplyToGameSceneCli() => ApplyInternal();

    static void ApplyInternal()
    {
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
                throw new System.Exception($"Failed to open scene: {ScenePath}");

            GameObject cameraGo = GameObject.FindWithTag("MainCamera");
            if (cameraGo == null)
                throw new System.Exception("MainCamera was not found in Game scene.");

            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlotPrefabPath);
            if (slotPrefab == null)
                throw new System.Exception($"Slot prefab was not found at: {SlotPrefabPath}");

            PerkSlots3DOverlay overlay = cameraGo.GetComponent<PerkSlots3DOverlay>();
            if (overlay == null)
                overlay = cameraGo.AddComponent<PerkSlots3DOverlay>();

            var serializedOverlay = new SerializedObject(overlay);
            serializedOverlay.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            serializedOverlay.FindProperty("slotDepth").floatValue = 6.4f;
            serializedOverlay.FindProperty("slotScale").floatValue = 6.5f;
            SetIfExists(serializedOverlay, "followInventoryUiSlots", false);
            SetIfExists(serializedOverlay, "inventoryRootName", "Inventory");
            SetIfExists(serializedOverlay, "slotSpacingMultiplier", 1f);
            SetIfExists(serializedOverlay, "slotGroupWorldOffset", Vector3.zero);
            SetIfExists(serializedOverlay, "hideSlotsWhenInventoryHidden", false);
            SetIfExists(serializedOverlay, "projectSlotsToSurface", false);
            SetIfExists(serializedOverlay, "surfaceRaycastDistance", 80f);
            SetIfExists(serializedOverlay, "surfaceOffset", 0.012f);
            SetIfExists(serializedOverlay, "alignSlotsToSurfaceNormal", false);

            SerializedProperty surfaceMask = serializedOverlay.FindProperty("surfaceLayerMask");
            if (surfaceMask != null)
                surfaceMask.intValue = ~0;

            SerializedProperty positionsProp = serializedOverlay.FindProperty("viewportPositions");
            positionsProp.arraySize = 3;
            positionsProp.GetArrayElementAtIndex(0).vector2Value = new Vector2(0.365f, 0.815f);
            positionsProp.GetArrayElementAtIndex(1).vector2Value = new Vector2(0.465f, 0.815f);
            positionsProp.GetArrayElementAtIndex(2).vector2Value = new Vector2(0.565f, 0.815f);
            serializedOverlay.ApplyModifiedPropertiesWithoutUndo();

            HideLegacyInventorySquares();

            EditorUtility.SetDirty(cameraGo);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("PerkSlots3DOverlaySetup: Game scene updated with 3D perk slots.");
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
        }
    }

    static void HideLegacyInventorySquares()
    {
        RectTransform[] rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null || rect.name != "Inventory") continue;
            if (!rect.gameObject.scene.IsValid()) continue;

            Transform main = rect.Find("Main");
            Transform second = rect.Find("Second");
            Transform third = rect.Find("Third");
            if (main == null || second == null || third == null) continue;

            SetImageTransparent(main.gameObject);
            SetImageTransparent(second.gameObject);
            SetImageTransparent(third.gameObject);
        }
    }

    static void SetImageTransparent(GameObject go)
    {
        if (go == null) return;
        Image image = go.GetComponent<Image>();
        if (image == null) return;

        Color color = image.color;
        color.a = 0f;
        image.color = color;
        image.raycastTarget = true;
        EditorUtility.SetDirty(image);
    }

    static void SetIfExists(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
    }

    static void SetIfExists(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    static void SetIfExists(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.stringValue = value;
    }

    static void SetIfExists(SerializedObject serializedObject, string propertyName, Vector3 value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.vector3Value = value;
    }
}
