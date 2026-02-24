using UnityEditor;
using UnityEngine;

public static class PlungerVisualSetup
{
    const string PrefabPath = "Assets/Prefabs/Plunger.prefab";
    const string SpritePath = "Assets/Resources/UI/Sprites/PSG-Ball/plunger.png";

    [MenuItem("Tools/PSG-Ball/Apply Plunger PNG Visual")]
    public static void ApplyFromMenu() => Apply();

    public static void ApplyFromCli() => Apply();

    static void Apply()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (prefabRoot == null)
            throw new System.Exception($"Failed to load prefab: {PrefabPath}");

        try
        {
            Plunger plunger = prefabRoot.GetComponent<Plunger>();
            if (plunger == null)
                throw new System.Exception("Plunger component not found on prefab root.");

            Transform springTransform = FindSpringTransform(prefabRoot.transform);
            if (springTransform == null)
                throw new System.Exception("Spring child transform was not found.");

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
                throw new System.Exception($"Sprite was not found at path: {SpritePath}");

            // Remove old mesh visuals.
            MeshRenderer meshRenderer = springTransform.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                Object.DestroyImmediate(meshRenderer);

            MeshFilter meshFilter = springTransform.GetComponent<MeshFilter>();
            if (meshFilter != null)
                Object.DestroyImmediate(meshFilter);

            // Add sprite visual.
            SpriteRenderer spriteRenderer = springTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = springTransform.gameObject.AddComponent<SpriteRenderer>();

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingLayerID = 0;
            spriteRenderer.sortingOrder = 5;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;

            // Pivot is at bottom, so keep the base at local origin.
            springTransform.localPosition = Vector3.zero;
            springTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            springTransform.localScale = new Vector3(0.013f, 0.0204f, 1f);

            // Configure compression to operate on the sprite Y axis.
            SerializedObject plungerSerialized = new SerializedObject(plunger);
            plungerSerialized.FindProperty("CompressionVisual").objectReferenceValue = springTransform;
            plungerSerialized.FindProperty("MinCompression").floatValue = 0.3f;
            plungerSerialized.FindProperty("Axis").enumValueIndex = 1; // Y
            plungerSerialized.FindProperty("UseLegacySpringAnimation").boolValue = false;
            plungerSerialized.ApplyModifiedPropertiesWithoutUndo();

            Animator animator = springTransform.GetComponent<Animator>();
            if (animator != null)
                animator.enabled = false;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            Debug.Log("PlungerVisualSetup: Plunger prefab updated to use plunger.png sprite visual.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    static Transform FindSpringTransform(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root) continue;
            string trimmed = child.name.Trim();
            if (trimmed == "Spring")
                return child;
        }

        return null;
    }
}
