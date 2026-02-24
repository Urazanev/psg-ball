using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class BumperUpgradeCLI
{
    public static void Run()
    {
        string scenePath = "Assets/Scenes/Game.unity";
        EditorSceneManager.OpenScene(scenePath);

        Bumper[] bumpers = Object.FindObjectsOfType<Bumper>();
        if (bumpers.Length == 0)
        {
            Debug.LogWarning("BUMPER_LOG: No Bumper objects found in the scene.");
            EditorApplication.Exit(1);
            return;
        }

        Color yellow = new Color32(255, 215, 0, 255);
        Color ringBlue = new Color32(64, 120, 170, 255);

        Material baseMat = BumperUpgradeToolHelper.GetOrCreateMaterial("BumperBase", yellow, 0.9f, true, 0.07f);
        Material ringMat = BumperUpgradeToolHelper.GetOrCreateMaterial("BumperTopRing", ringBlue, 0.98f, true, 0.04f);
        Material capMat = BumperUpgradeToolHelper.GetOrCreateMaterial("BumperTop", yellow, 0.98f, true, 0.12f);

        Mesh capMesh = BumperUpgradeToolHelper.GetMeshFromModelOrPrimitive("Assets/Models/BumperTop.obj", PrimitiveType.Sphere);
        Mesh baseMesh = BumperUpgradeToolHelper.GetMeshFromModelOrPrimitive("Assets/Models/BumperBase.obj", PrimitiveType.Cylinder);
        Mesh ringMesh = BumperUpgradeToolHelper.GetMeshFromModelOrPrimitive("Assets/Models/BumperTopRing.obj", PrimitiveType.Cylinder);

        int sceneCount = 0;
        foreach (Bumper bumper in bumpers)
        {
            if (BumperUpgradeToolHelper.ApplyToyCoreUpgrade(bumper, baseMat, ringMat, capMat, capMesh, baseMesh, ringMesh))
            {
                sceneCount++;
            }
        }

        int prefabCount = UpgradeBumperPrefab(baseMat, ringMat, capMat, capMesh, baseMesh, ringMesh);

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("BUMPER_LOG: Successfully upgraded " + sceneCount + " bumpers in scene and " + prefabCount + " in prefab.");
        EditorApplication.Exit(0);
    }

    private static int UpgradeBumperPrefab(
        Material baseMat,
        Material ringMat,
        Material capMat,
        Mesh capMesh,
        Mesh baseMesh,
        Mesh ringMesh)
    {
        const string prefabPath = "Assets/Prefabs/Bumper.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            Debug.LogWarning("BUMPER_LOG: Bumper prefab not found at " + prefabPath);
            return 0;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        int upgraded = 0;
        try
        {
            Bumper[] prefabBumpers = prefabRoot.GetComponentsInChildren<Bumper>(true);
            foreach (Bumper bumper in prefabBumpers)
            {
                if (BumperUpgradeToolHelper.ApplyToyCoreUpgrade(bumper, baseMat, ringMat, capMat, capMesh, baseMesh, ringMesh))
                {
                    upgraded++;
                }
            }

            if (upgraded > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return upgraded;
    }
}

public static class BumperUpgradeToolHelper
{
    private const float VisualLift = 0.08f;
    private const float BaseRadiusScale = 0.62f;
    private const float BaseHeightScale = 1.18f;
    private const float TopRootYOffset = 0.88f;
    private const float TopOverhangScale = 1f;
    private const float RingRadiusScale = 0.9f;
    private const float RingHeightScale = 0.045f;
    private const float RingYOffset = -0.02f;
    private const float CapRadiusScale = 0.56f;
    private const float CapHeightScale = 0.34f;
    private const float CapYOffset = 0.08f;

    public static Material GetOrCreateMaterial(string matName, Color color, float smoothness, bool emissionEnabled, float emissionIntensity)
    {
        string path = "Assets/Materials/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader == null)
            {
                mat = new Material(Shader.Find("Standard"));
            }

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            AssetDatabase.CreateAsset(mat, path);
        }

        ConfigureMaterial(mat, color, smoothness, emissionEnabled, emissionIntensity);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Mesh GetMeshFromModelOrPrimitive(string modelPath, PrimitiveType fallbackType)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(modelPath);
        if (mesh != null)
        {
            return mesh;
        }

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model != null)
        {
            MeshFilter filter = model.GetComponentInChildren<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                return filter.sharedMesh;
            }
        }

        GameObject primitive = GameObject.CreatePrimitive(fallbackType);
        Mesh fallbackMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(primitive);
        Debug.LogWarning("BUMPER_LOG: Missing model at " + modelPath + ". Using primitive fallback: " + fallbackType);
        return fallbackMesh;
    }

    public static bool ApplyToyCoreUpgrade(
        Bumper bumper,
        Material baseMat,
        Material ringMat,
        Material capMat,
        Mesh capMesh,
        Mesh baseMesh,
        Mesh ringMesh)
    {
        if (bumper == null)
        {
            return false;
        }

        Undo.RecordObject(bumper.gameObject, "Upgrade Bumper");

        Transform baseTransform = bumper.transform.Find("Base");
        if (baseTransform == null)
        {
            Debug.LogWarning("BUMPER_LOG: Bumper " + bumper.name + " does not have a Base child. Skipped.");
            return false;
        }

        Undo.RecordObject(baseTransform.gameObject, "Upgrade Bumper Base");
        MeshRenderer baseRenderer = ConfigureVisualPart(
            baseTransform,
            baseMesh,
            baseMat,
            true,
            new Vector3(BaseRadiusScale, BaseHeightScale, BaseRadiusScale),
            new Vector3(0f, VisualLift, 0f));

        Transform topRoot = ResolveTopRoot(bumper.transform);
        Undo.RecordObject(topRoot.gameObject, "Upgrade Bumper Top");
        topRoot.localPosition = new Vector3(0f, VisualLift + TopRootYOffset, 0f);
        topRoot.localRotation = Quaternion.identity;
        topRoot.localScale = Vector3.one;

        // Keep a deterministic split setup (Ring + Cap) to match reference even when meshes are fallbacks.
        RemoveComponentIfExists<MeshFilter>(topRoot.gameObject);
        RemoveComponentIfExists<MeshRenderer>(topRoot.gameObject);
        topRoot.localScale = Vector3.one;

        Transform ringTransform = EnsureChild(topRoot, "BumperTopRing");
        MeshRenderer ringRenderer = ConfigureVisualPart(
            ringTransform,
            ringMesh,
            ringMat,
            false,
            new Vector3(TopOverhangScale * RingRadiusScale, RingHeightScale, TopOverhangScale * RingRadiusScale),
            new Vector3(0f, RingYOffset, 0f));

        Transform capTransform = EnsureChild(topRoot, "BumperTopCap");
        MeshRenderer capRenderer = ConfigureVisualPart(
            capTransform,
            capMesh,
            capMat,
            true,
            new Vector3(TopOverhangScale * CapRadiusScale, CapHeightScale, TopOverhangScale * CapRadiusScale),
            new Vector3(0f, CapYOffset, 0f));

        Renderer flashTargetRenderer = capRenderer;

        BumperVisuals visuals = EnsureSingleBumperVisuals(bumper);

        visuals.flashRenderer = flashTargetRenderer;
        visuals.flashDuration = 0.1f;
        visuals.maxEmissionIntensity = 4.8f;
        visuals.punchScale = 1.12f;
        visuals.punchDuration = 0.12f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(visuals);

        MeshRenderer parentRenderer = bumper.GetComponent<MeshRenderer>();
        if (parentRenderer != null)
        {
            parentRenderer.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(parentRenderer);
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(baseTransform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(topRoot);
        EditorUtility.SetDirty(baseRenderer);
        EditorUtility.SetDirty(visuals);
        EditorUtility.SetDirty(bumper.gameObject);
        return true;
    }

    private static void ConfigureMaterial(Material mat, Color color, float smoothness, bool emissionEnabled, float emissionIntensity)
    {
        mat.color = color;
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0.0f);

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }

        if (emissionEnabled)
        {
            Color emissionColor = color * emissionIntensity;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", emissionColor);
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            mat.SetColor("_EmissionColor", Color.black);
        }
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(childObject, "Create " + childName);
        }
        return child;
    }

    private static Transform ResolveTopRoot(Transform bumperTransform)
    {
        List<Transform> topCandidates = new List<Transform>();
        for (int i = 0; i < bumperTransform.childCount; i++)
        {
            Transform child = bumperTransform.GetChild(i);
            if (child.name == "BumperTop")
            {
                topCandidates.Add(child);
            }
        }

        Transform prefabLinkedTop = null;
        for (int i = 0; i < topCandidates.Count; i++)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(topCandidates[i].gameObject) != null)
            {
                prefabLinkedTop = topCandidates[i];
                break;
            }
        }

        if (prefabLinkedTop != null)
        {
            for (int i = 0; i < topCandidates.Count; i++)
            {
                if (topCandidates[i] != prefabLinkedTop)
                {
                    Undo.DestroyObjectImmediate(topCandidates[i].gameObject);
                }
            }
            return prefabLinkedTop;
        }

        if (topCandidates.Count > 0)
        {
            return topCandidates[0];
        }

        return EnsureChild(bumperTransform, "BumperTop");
    }

    private static void RemoveChildIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void RemoveComponentIfExists<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component);
        }
    }

    private static MeshRenderer ConfigureTopMultiMaterial(Transform topRoot, Mesh topMesh, Material ringMat, Material capMat)
    {
        MeshFilter filter = topRoot.GetComponent<MeshFilter>();
        if (filter == null)
        {
            filter = topRoot.gameObject.AddComponent<MeshFilter>();
        }
        filter.sharedMesh = topMesh;

        MeshRenderer renderer = topRoot.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            renderer = topRoot.gameObject.AddComponent<MeshRenderer>();
        }
        renderer.sharedMaterials = new[] { ringMat, capMat };
        SetReceiveShadows(renderer, true);
        EditorUtility.SetDirty(renderer);
        return renderer;
    }

    private static MeshRenderer ConfigureVisualPart(
        Transform part,
        Mesh mesh,
        Material material,
        bool receiveShadows,
        Vector3 localScale,
        Vector3 localPosition)
    {
        Undo.RecordObject(part, "Configure Bumper Visual Part");
        part.localPosition = localPosition;
        part.localRotation = Quaternion.identity;
        part.localScale = localScale;
        PrefabUtility.RecordPrefabInstancePropertyModifications(part);
        EditorUtility.SetDirty(part);

        MeshFilter meshFilter = part.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = part.gameObject.AddComponent<MeshFilter>();
        }
        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = part.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = part.gameObject.AddComponent<MeshRenderer>();
        }
        meshRenderer.sharedMaterial = material;
        SetReceiveShadows(meshRenderer, receiveShadows);
        PrefabUtility.RecordPrefabInstancePropertyModifications(meshRenderer);
        EditorUtility.SetDirty(meshRenderer);
        return meshRenderer;
    }

    private static void SetReceiveShadows(MeshRenderer renderer, bool receiveShadows)
    {
        renderer.receiveShadows = receiveShadows;
        SerializedObject serializedRenderer = new SerializedObject(renderer);
        SerializedProperty receiveShadowsProp = serializedRenderer.FindProperty("m_ReceiveShadows");
        if (receiveShadowsProp != null)
        {
            receiveShadowsProp.intValue = receiveShadows ? 1 : 0;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static BumperVisuals EnsureSingleBumperVisuals(Bumper bumper)
    {
        BumperVisuals[] allVisuals = bumper.GetComponents<BumperVisuals>();
        BumperVisuals chosen = null;

        for (int i = 0; i < allVisuals.Length; i++)
        {
            if (!PrefabUtility.IsAddedComponentOverride(allVisuals[i]))
            {
                chosen = allVisuals[i];
                break;
            }
        }

        if (chosen == null)
        {
            if (allVisuals.Length > 0)
            {
                chosen = allVisuals[0];
            }
            else
            {
                chosen = bumper.gameObject.AddComponent<BumperVisuals>();
            }
        }

        for (int i = 0; i < allVisuals.Length; i++)
        {
            if (allVisuals[i] != chosen)
            {
                Undo.DestroyObjectImmediate(allVisuals[i]);
            }
        }

        return chosen;
    }
}
