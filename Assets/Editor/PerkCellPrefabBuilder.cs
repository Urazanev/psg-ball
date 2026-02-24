using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PerkCellPrefabBuilder
{
    const string ModelsFolder = "Assets/Models/UI3D";
    const string MaterialsFolder = "Assets/Materials/UI3D";
    const string PrefabsFolder = "Assets/Prefabs/UI3D";

    const string MeshPath = ModelsFolder + "/PerkCellMesh.asset";
    const string MaterialPath = MaterialsFolder + "/PerkCell_Grey.mat";
    const string PrefabPath = PrefabsFolder + "/PerkCell3D.prefab";

    [MenuItem("Tools/PSG-Ball/Build 3D Perk Cell")]
    public static void BuildFromMenu() => BuildSingleCell();

    public static void BuildSingleCell()
    {
        EnsureFolder("Assets", "Models");
        EnsureFolder("Assets/Models", "UI3D");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "UI3D");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "UI3D");

        CellSettings settings = CellSettings.Default();

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = "PerkCellMesh" };
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }

        BuildCellMesh(mesh, settings);
        EditorUtility.SetDirty(mesh);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        ConfigureMaterial(material);
        EditorUtility.SetDirty(material);

        GameObject root = new GameObject("PerkCell3D");
        try
        {
            MeshFilter meshFilter = root.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = root.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;

            MeshCollider meshCollider = root.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            float halfHeight = settings.OuterHeight * 0.5f;
            float anchorY = halfHeight - settings.CavityInsetDepth - (settings.CenterDimpleDepth * 0.4f) + 0.001f;
            GameObject anchor = new GameObject("PerkAnchor");
            anchor.transform.SetParent(root.transform, false);
            anchor.transform.localPosition = new Vector3(0f, anchorY, 0f);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"PerkCellPrefabBuilder: generated {PrefabPath}");
    }

    static void ConfigureMaterial(Material material)
    {
        material.name = "PerkCell_Grey";
        material.color = new Color(0.84f, 0.86f, 0.89f, 1f);

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.84f, 0.86f, 0.89f, 1f));
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.73f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_SpecColor"))
            material.SetColor("_SpecColor", new Color(0.19f, 0.19f, 0.19f, 1f));
    }

    static void BuildCellMesh(Mesh mesh, CellSettings settings)
    {
        const int xSegments = 60;
        const int zSegments = 60;

        int topVertCount = (xSegments + 1) * (zSegments + 1);
        var vertices = new List<Vector3>(topVertCount * 2);
        var uvs = new List<Vector2>(topVertCount * 2);
        var triangles = new List<int>(xSegments * zSegments * 12);

        float halfWidth = settings.OuterWidth * 0.5f;
        float halfDepth = settings.OuterDepth * 0.5f;
        float halfHeight = settings.OuterHeight * 0.5f;
        float invXSegments = 1f / xSegments;
        float invZSegments = 1f / zSegments;

        for (int z = 0; z <= zSegments; z++)
        {
            float v = z * invZSegments;
            float pz = Mathf.Lerp(-halfDepth, halfDepth, v);
            for (int x = 0; x <= xSegments; x++)
            {
                float u = x * invXSegments;
                float px = Mathf.Lerp(-halfWidth, halfWidth, u);
                float py = EvaluateTopHeight(px, pz, settings);
                vertices.Add(new Vector3(px, py, pz));
                uvs.Add(new Vector2(u, v));
            }
        }

        for (int z = 0; z <= zSegments; z++)
        {
            float v = z * invZSegments;
            float pz = Mathf.Lerp(-halfDepth, halfDepth, v);
            for (int x = 0; x <= xSegments; x++)
            {
                float u = x * invXSegments;
                float px = Mathf.Lerp(-halfWidth, halfWidth, u);
                vertices.Add(new Vector3(px, -halfHeight, pz));
                uvs.Add(new Vector2(u, v));
            }
        }

        int IndexTop(int x, int z) => z * (xSegments + 1) + x;
        int IndexBottom(int x, int z) => topVertCount + IndexTop(x, z);

        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int a = IndexTop(x, z);
                int b = IndexTop(x + 1, z);
                int c = IndexTop(x + 1, z + 1);
                int d = IndexTop(x, z + 1);
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(c);
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
            }
        }

        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int a = IndexBottom(x, z);
                int b = IndexBottom(x + 1, z);
                int c = IndexBottom(x + 1, z + 1);
                int d = IndexBottom(x, z + 1);
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(d);
            }
        }

        var perimeter = new List<(int x, int z)>((xSegments + zSegments) * 2);
        for (int x = 0; x <= xSegments; x++) perimeter.Add((x, 0));
        for (int z = 1; z <= zSegments; z++) perimeter.Add((xSegments, z));
        for (int x = xSegments - 1; x >= 0; x--) perimeter.Add((x, zSegments));
        for (int z = zSegments - 1; z >= 1; z--) perimeter.Add((0, z));

        for (int i = 0; i < perimeter.Count; i++)
        {
            (int x, int z) current = perimeter[i];
            (int x, int z) next = perimeter[(i + 1) % perimeter.Count];

            int topA = IndexTop(current.x, current.z);
            int topB = IndexTop(next.x, next.z);
            int bottomA = IndexBottom(current.x, current.z);
            int bottomB = IndexBottom(next.x, next.z);

            triangles.Add(topA);
            triangles.Add(topB);
            triangles.Add(bottomB);
            triangles.Add(topA);
            triangles.Add(bottomB);
            triangles.Add(bottomA);
        }

        mesh.Clear();
        mesh.name = "PerkCellMesh";
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
    }

    static float EvaluateTopHeight(float x, float z, CellSettings settings)
    {
        float halfWidth = settings.OuterWidth * 0.5f;
        float halfDepth = settings.OuterDepth * 0.5f;
        float halfHeight = settings.OuterHeight * 0.5f;

        float y = halfHeight;
        float distanceToOuterEdge = Mathf.Min(halfWidth - Mathf.Abs(x), halfDepth - Mathf.Abs(z));
        if (distanceToOuterEdge < settings.OuterEdgeBevelWidth)
        {
            float t = 1f - Mathf.Clamp01(distanceToOuterEdge / settings.OuterEdgeBevelWidth);
            y -= settings.OuterEdgeBevelDepth * Smooth01(t);
        }

        float cavitySd = SignedDistanceRoundedRect(
            new Vector2(x, z),
            settings.CavityWidth * 0.5f,
            settings.CavityDepth * 0.5f,
            settings.CavityCornerRadius);

        if (cavitySd < 0f)
        {
            float blend = Mathf.Clamp01((-cavitySd) / Mathf.Max(0.0001f, settings.CavityBlendWidth));
            float profile = Smooth01(blend);
            float centerFactor = 1f - Mathf.Clamp01(Mathf.Sqrt(
                (x * x) / Mathf.Max(0.0001f, settings.CavityWidth * settings.CavityWidth * 0.25f) +
                (z * z) / Mathf.Max(0.0001f, settings.CavityDepth * settings.CavityDepth * 0.25f)));

            y -= settings.CavityInsetDepth * profile;
            y -= settings.CenterDimpleDepth * centerFactor * profile;
        }

        float minTop = -halfHeight + settings.BottomThickness;
        return Mathf.Max(y, minTop);
    }

    static float SignedDistanceRoundedRect(Vector2 p, float width, float depth, float radius)
    {
        float rx = Mathf.Max(0.0001f, width - radius);
        float rz = Mathf.Max(0.0001f, depth - radius);
        Vector2 q = new Vector2(Mathf.Abs(p.x) - rx, Mathf.Abs(p.y) - rz);
        Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        float outsideLen = outside.magnitude;
        float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        return outsideLen + inside - radius;
    }

    static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    struct CellSettings
    {
        public float OuterWidth;
        public float OuterDepth;
        public float OuterHeight;
        public float CavityWidth;
        public float CavityDepth;
        public float CavityCornerRadius;
        public float CavityInsetDepth;
        public float CavityBlendWidth;
        public float CenterDimpleDepth;
        public float OuterEdgeBevelWidth;
        public float OuterEdgeBevelDepth;
        public float BottomThickness;

        public static CellSettings Default()
        {
            return new CellSettings
            {
                // 160 mm inner area to comfortably fit a 150x150 perk tile.
                OuterWidth = 0.182f,
                OuterDepth = 0.182f,
                OuterHeight = 0.026f,
                CavityWidth = 0.160f,
                CavityDepth = 0.160f,
                CavityCornerRadius = 0.034f,
                CavityInsetDepth = 0.010f,
                CavityBlendWidth = 0.034f,
                CenterDimpleDepth = 0.0015f,
                OuterEdgeBevelWidth = 0.022f,
                OuterEdgeBevelDepth = 0.0028f,
                BottomThickness = 0.006f
            };
        }
    }
}
