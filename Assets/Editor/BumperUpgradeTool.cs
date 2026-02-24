using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class BumperUpgradeTool : EditorWindow
{
    [MenuItem("Pinball/Upgrade Bumpers to Toy-Core")]
    public static void UpgradeBumpers()
    {
        // Find all Bumper components in the scene
        Bumper[] bumpers = FindObjectsOfType<Bumper>();
        if (bumpers.Length == 0)
        {
            Debug.LogWarning("No Bumper objects found in the scene.");
            return;
        }

        // Load or create materials to match reference: yellow core + lighter glossy blue rim
        Color yellow = new Color32(255, 215, 0, 255);
        Color ringBlue = new Color32(64, 120, 170, 255);
        Material baseMat = BumperUpgradeToolHelper.GetOrCreateMaterial("BumperBase", yellow, 0.9f, true, 0.07f);
        Material ringMat = BumperUpgradeToolHelper.GetOrCreateMaterial("BumperTopRing", ringBlue, 0.98f, true, 0.04f);
        Material capMat = BumperUpgradeToolHelper.GetOrCreateMaterial("BumperTop", yellow, 0.98f, true, 0.12f);

        // Load models, fallback to Unity primitives if obj files are unavailable.
        Mesh topMesh = BumperUpgradeToolHelper.GetMeshFromModelOrPrimitive("Assets/Models/BumperTop.obj", PrimitiveType.Sphere);
        Mesh baseMesh = BumperUpgradeToolHelper.GetMeshFromModelOrPrimitive("Assets/Models/BumperBase.obj", PrimitiveType.Cylinder);
        Mesh ringMesh = BumperUpgradeToolHelper.GetMeshFromModelOrPrimitive("Assets/Models/BumperTopRing.obj", PrimitiveType.Cylinder);

        int count = 0;
        foreach (Bumper bumper in bumpers)
        {
            if (BumperUpgradeToolHelper.ApplyToyCoreUpgrade(bumper, baseMat, ringMat, capMat, topMesh, baseMesh, ringMesh))
            {
                count++;
            }
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Success", "Successfully upgraded " + count + " bumpers to Toy-Core style!", "OK");
        Debug.Log("Successfully upgraded " + count + " bumpers to Toy-Core style.");
    }
}
