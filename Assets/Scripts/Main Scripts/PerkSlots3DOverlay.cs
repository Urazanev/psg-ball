using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-800)]
[DisallowMultipleComponent]
public class PerkSlots3DOverlay : MonoBehaviour
{
    const string DefaultPrefabResourcePath = "UI3D/PerkCell3D";
    const int DefaultSlotCount = 3;
    const string IconSpriteRoot = "UI/Sprites/PSG-Ball/";

    [Header("Source")]
    [SerializeField] bool usePrefabVisual = true;
    [SerializeField] Object slotPrefab;
    [SerializeField] string slotPrefabResourcePath = DefaultPrefabResourcePath;

    [Header("Layout")]
    [SerializeField] float slotDepth = 6.8f;
    [SerializeField] float slotScale = 2.8f;
    [SerializeField] Vector3 slotEulerOffset = new Vector3(-90f, 0f, 0f);
    [SerializeField] bool followInventoryUiSlots = false;
    [SerializeField] string inventoryRootName = "Inventory";
    [SerializeField] float slotSpacingMultiplier = 1f;
    [SerializeField] Vector3 slotGroupWorldOffset = Vector3.zero;
    [SerializeField] bool hideSlotsWhenInventoryHidden = false;
    [SerializeField] bool projectSlotsToSurface = false;
    [SerializeField] float surfaceRaycastDistance = 80f;
    [SerializeField] LayerMask surfaceLayerMask = ~0;
    [SerializeField] float surfaceOffset = 0.012f;
    [SerializeField] bool alignSlotsToSurfaceNormal = false;
    [SerializeField] bool useAutoLayout = true;
    [SerializeField] int autoSlotCount = DefaultSlotCount;
    [SerializeField] Vector2 autoStartViewportPosition = new Vector2(0.435f, 0.92f);
    [SerializeField] Vector2 autoViewportSpacing = new Vector2(0.065f, 0f);
    [SerializeField] Vector2[] viewportPositions =
    {
        new Vector2(0.365f, 0.815f),
        new Vector2(0.465f, 0.815f),
        new Vector2(0.565f, 0.815f)
    };
    [SerializeField] bool hideLegacySquares = true;
    [SerializeField] bool disableLegacyRaycast = false;
    [SerializeField] bool showOnlyDuringGameplay = true;
    [Header("Emergency Profile")]
    [SerializeField] bool enforceLowBackWallAtRuntime = true;
    [SerializeField] bool enforceTightCameraAtRuntime = true;
    [SerializeField] Vector3 runtimeCameraLocalPosition = new Vector3(-6.45f, 13.55f, -8.2f);
    [SerializeField] Vector3 runtimeCameraLocalEuler = new Vector3(45f, 0f, 0f);
    [Header("Backboard Mount")]
    [SerializeField] bool mountSlotsToBackboard = false;
    [SerializeField] bool hideBackboardForExperiment = false;
    [SerializeField] string backboardRootName = "Arena Static";
    [SerializeField] string backboardNameContains = "Outer Edge";
    [SerializeField] float backboardSurfaceOffset = 0.02f;
    [SerializeField] float backboardHorizontalOffset = 0f;
    [SerializeField] float backboardVerticalOffset = 0f;
    [SerializeField] float backboardGapFactor = 0.22f;
    [Header("Shape")]
    [SerializeField] bool applyRuntimeCornerRounding = true;
    [SerializeField] float runtimeOuterCornerRadius = 0.008f;
    [Header("Perk Visuals")]
    [SerializeField] bool renderPerkIcons = true;
    [SerializeField] float perkIconFill = 0.70f;
    [SerializeField] float perkIconDepthBias = 0.014f;
    [SerializeField] int perkIconSortingOrder = 25;

    readonly List<GameObject> spawnedSlots = new List<GameObject>();
    readonly List<GameObject> spawnedPerkIcons = new List<GameObject>();
    readonly List<SpriteRenderer> spawnedPerkRenderers = new List<SpriteRenderer>();
    readonly Dictionary<Mesh, Mesh> roundedMeshCache = new Dictionary<Mesh, Mesh>();
    readonly Vector3[] uiSlotWorldCache = new Vector3[DefaultSlotCount];
    readonly Vector3[] uiSlotSurfaceNormalCache = new Vector3[DefaultSlotCount];
    Camera cachedCamera;
    RectTransform cachedInventoryRoot;
    Renderer cachedBackboardRenderer;
    bool runtimeProfileApplied;
    bool loggedMissingPrefab;
    bool loggedUiFallback;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AttachToMainCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
            if (cameras != null && cameras.Length > 0)
                camera = cameras[0];
        }
        if (camera == null) return;

        PerkSlots3DOverlay overlay = camera.GetComponent<PerkSlots3DOverlay>();
        if (overlay == null)
            overlay = camera.gameObject.AddComponent<PerkSlots3DOverlay>();

        overlay.ForceRefresh();
    }

    void Awake()
    {
        ForceRefresh();
    }

    void OnEnable()
    {
        ForceRefresh();
    }

    void LateUpdate()
    {
        if (spawnedSlots.Count == 0)
            EnsureSlots();
        UpdateSlots();
        UpdatePerkIcons();
    }

    void OnDisable()
    {
        ClearSpawnedSlots();
        ClearPerkIconSlots();
        ClearRoundedMeshCache();
    }

    public void ForceRefresh()
    {
        cachedCamera = GetComponent<Camera>();
        ApplyRuntimeViewportFallback();
        ApplyRuntimeEmergencyProfile();
        loggedUiFallback = false;
        ClearSpawnedSlots();
        ClearPerkIconSlots();
        TryResolvePrefab();
        HideLegacySquares();
        EnsureSlots();
        UpdateSlots();
        UpdatePerkIcons();
    }

    void TryResolvePrefab()
    {
        if (ResolveSlotPrefabRoot() != null)
            return;

        if (!usePrefabVisual)
        {
            slotPrefab = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(slotPrefabResourcePath))
            slotPrefabResourcePath = DefaultPrefabResourcePath;

        slotPrefab = Resources.Load<GameObject>(slotPrefabResourcePath);
    }

    GameObject ResolveSlotPrefabRoot()
    {
        if (slotPrefab is GameObject slotGo)
            return slotGo;

        if (slotPrefab is Component component)
            return component.gameObject;

        return null;
    }

    void ClearSpawnedSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] == null) continue;
            Destroy(spawnedSlots[i]);
        }

        spawnedSlots.Clear();
    }

    void ClearPerkIconSlots()
    {
        for (int i = 0; i < spawnedPerkIcons.Count; i++)
        {
            if (spawnedPerkIcons[i] == null) continue;
            Destroy(spawnedPerkIcons[i]);
        }

        spawnedPerkIcons.Clear();
        spawnedPerkRenderers.Clear();
    }

    void ClearRoundedMeshCache()
    {
        foreach (Mesh mesh in roundedMeshCache.Values)
        {
            if (mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(mesh);
                else
                    DestroyImmediate(mesh);
            }
        }

        roundedMeshCache.Clear();
    }

    void EnsureSlots()
    {
        TryResolvePrefab();
        GameObject slotPrefabRoot = ResolveSlotPrefabRoot();
        int targetCount = GetTargetSlotCount();

        while (spawnedSlots.Count < targetCount)
        {
            GameObject slot = null;
            if (slotPrefabRoot != null)
            {
                try
                {
                    Object instance = Instantiate((Object)slotPrefabRoot, transform);
                    slot = instance as GameObject;
                    if (slot == null && instance is Component instanceComponent)
                        slot = instanceComponent.gameObject;
                }
                catch (System.Exception ex)
                {
                    slotPrefab = null;
                    slotPrefabRoot = null;
                    if (!loggedMissingPrefab)
                    {
                        loggedMissingPrefab = true;
                        Debug.LogWarning($"PerkSlots3DOverlay: prefab instantiate failed, switching to fallback. {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }

            if (slot == null)
            {
                slot = CreateFallbackSlotVisual();
                slot.transform.SetParent(transform, false);
            }

            slot.name = $"PerkSlot3D_{spawnedSlots.Count + 1}";
            ApplyRuntimeCornerRounding(slot);

            MeshCollider meshCollider = slot.GetComponent<MeshCollider>();
            if (meshCollider != null)
                meshCollider.enabled = false;

            spawnedSlots.Add(slot);
        }

        for (int i = spawnedSlots.Count - 1; i >= targetCount; i--)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i]);
            spawnedSlots.RemoveAt(i);
        }

        EnsurePerkIconSlots(targetCount);
    }

    void EnsurePerkIconSlots(int targetCount)
    {
        while (spawnedPerkIcons.Count < targetCount)
        {
            GameObject iconRoot = new GameObject($"PerkSlotIcon3D_{spawnedPerkIcons.Count + 1}");
            iconRoot.transform.SetParent(transform, false);

            SpriteRenderer spriteRenderer = iconRoot.AddComponent<SpriteRenderer>();
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.sortingOrder = perkIconSortingOrder;
            spriteRenderer.maskInteraction = SpriteMaskInteraction.None;
            spriteRenderer.color = Color.white;
            spriteRenderer.enabled = false;

            spawnedPerkIcons.Add(iconRoot);
            spawnedPerkRenderers.Add(spriteRenderer);
        }

        for (int i = spawnedPerkIcons.Count - 1; i >= targetCount; i--)
        {
            if (spawnedPerkIcons[i] != null)
                Destroy(spawnedPerkIcons[i]);
            spawnedPerkIcons.RemoveAt(i);
            spawnedPerkRenderers.RemoveAt(i);
        }
    }

    GameObject CreateFallbackSlotVisual()
    {
        if (!loggedMissingPrefab)
        {
            loggedMissingPrefab = true;
            Debug.LogWarning("PerkSlots3DOverlay: slot prefab not found, using fallback 3D slot.");
        }

        GameObject root = new GameObject("PerkSlot_Fallback");

        GameObject baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseCube.name = "Base";
        baseCube.transform.SetParent(root.transform, false);
        baseCube.transform.localScale = new Vector3(0.18f, 0.022f, 0.18f);

        GameObject cavityCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cavityCube.name = "Cavity";
        cavityCube.transform.SetParent(root.transform, false);
        cavityCube.transform.localPosition = new Vector3(0f, 0.006f, 0f);
        cavityCube.transform.localScale = new Vector3(0.145f, 0.012f, 0.145f);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material baseMat = new Material(shader);
        baseMat.color = new Color(0.82f, 0.84f, 0.87f, 1f);
        if (baseMat.HasProperty("_Glossiness"))
            baseMat.SetFloat("_Glossiness", 0.45f);
        if (baseMat.HasProperty("_Smoothness"))
            baseMat.SetFloat("_Smoothness", 0.45f);

        Material cavityMat = new Material(shader);
        cavityMat.color = new Color(0.72f, 0.75f, 0.79f, 1f);
        if (cavityMat.HasProperty("_Glossiness"))
            cavityMat.SetFloat("_Glossiness", 0.35f);
        if (cavityMat.HasProperty("_Smoothness"))
            cavityMat.SetFloat("_Smoothness", 0.35f);

        MeshRenderer baseRenderer = baseCube.GetComponent<MeshRenderer>();
        if (baseRenderer != null)
        {
            baseRenderer.sharedMaterial = baseMat;
            baseRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            baseRenderer.receiveShadows = false;
        }

        Collider baseCollider = baseCube.GetComponent<Collider>();
        if (baseCollider != null)
            Destroy(baseCollider);

        MeshRenderer cavityRenderer = cavityCube.GetComponent<MeshRenderer>();
        if (cavityRenderer != null)
        {
            cavityRenderer.sharedMaterial = cavityMat;
            cavityRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cavityRenderer.receiveShadows = false;
        }

        Collider cavityCollider = cavityCube.GetComponent<Collider>();
        if (cavityCollider != null)
            Destroy(cavityCollider);

        return root;
    }

    void UpdateSlots()
    {
        if (cachedCamera == null)
            cachedCamera = GetComponent<Camera>();
        if (cachedCamera == null) return;

        if (spawnedSlots.Count == 0) return;
        if (showOnlyDuringGameplay && !IsGameplayActive())
        {
            SetSpawnedSlotsActive(false);
            SetPerkIconsActive(false);
            RestoreBackboardVisibility();
            return;
        }

        bool hasUiSlotTargets = followInventoryUiSlots && TryGetInventorySlotWorldPositions(uiSlotWorldCache);
        if (followInventoryUiSlots && !hasUiSlotTargets && hideSlotsWhenInventoryHidden)
        {
            SetSpawnedSlotsActive(false);
            SetPerkIconsActive(false);
            return;
        }

        SetSpawnedSlotsActive(true);
        if (!mountSlotsToBackboard)
            RestoreBackboardVisibility();
        if (TryMountSlotsToBackboard())
            return;

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            GameObject slot = spawnedSlots[i];
            if (slot == null) continue;

            Vector3 worldPos;
            if (hasUiSlotTargets && i < uiSlotWorldCache.Length)
                worldPos = uiSlotWorldCache[i];
            else
            {
                Vector2 viewport = GetViewportPosition(i);
                worldPos = cachedCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, slotDepth));
            }

            slot.transform.position = worldPos;
            if (hasUiSlotTargets && alignSlotsToSurfaceNormal && i < uiSlotSurfaceNormalCache.Length)
                slot.transform.rotation = BuildSurfaceAlignedRotation(uiSlotSurfaceNormalCache[i]);
            else
                slot.transform.rotation = cachedCamera.transform.rotation * Quaternion.Euler(slotEulerOffset);
            slot.transform.localScale = Vector3.one * slotScale;
        }

    }

    bool TryMountSlotsToBackboard()
    {
        if (!mountSlotsToBackboard)
            return false;
        if (spawnedSlots.Count == 0)
            return false;
        if (cachedCamera == null)
            cachedCamera = GetComponent<Camera>();
        if (cachedCamera == null)
            return false;

        if (!TryResolveBackboardFrame(out Renderer backboardRenderer, out Vector3 center, out Vector3 normal, out Vector3 widthAxis, out Vector3 heightAxis, out float boardWidth, out float boardHeight, out float boardThickness))
            return false;

        ApplyBackboardVisibility(backboardRenderer);

        float faceSize = EstimateSlotFaceSizeWorld();
        if (faceSize <= 0.001f)
            faceSize = 0.18f * Mathf.Max(0.01f, slotScale);

        int count = spawnedSlots.Count;
        float spacing = faceSize * (1f + Mathf.Max(0f, backboardGapFactor));
        if (count > 1)
        {
            float maxSpan = Mathf.Max(0f, boardWidth - faceSize * 0.92f);
            float desiredSpan = spacing * (count - 1);
            if (desiredSpan > maxSpan && maxSpan > 0f)
                spacing = maxSpan / (count - 1);
        }

        Vector3 rowCenter = center + normal * (boardThickness * 0.5f + backboardSurfaceOffset);
        rowCenter += widthAxis * backboardHorizontalOffset;
        rowCenter += heightAxis * backboardVerticalOffset;

        Quaternion rotation = Quaternion.LookRotation(-normal, heightAxis) * Quaternion.Euler(slotEulerOffset);
        float start = 0.5f * (count - 1);
        for (int i = 0; i < count; i++)
        {
            GameObject slot = spawnedSlots[i];
            if (slot == null) continue;

            float offset = i - start;
            slot.transform.position = rowCenter + widthAxis * (offset * spacing);
            slot.transform.rotation = rotation;
            slot.transform.localScale = Vector3.one * slotScale;
        }

        return true;
    }

    bool TryResolveBackboardFrame(out Renderer backboardRenderer, out Vector3 center, out Vector3 normal, out Vector3 widthAxis, out Vector3 heightAxis, out float boardWidth, out float boardHeight, out float boardThickness)
    {
        backboardRenderer = ResolveBackboardRenderer();
        center = Vector3.zero;
        normal = Vector3.forward;
        widthAxis = Vector3.right;
        heightAxis = Vector3.up;
        boardWidth = 0f;
        boardHeight = 0f;
        boardThickness = 0f;
        if (backboardRenderer == null)
            return false;

        Transform t = backboardRenderer.transform;
        MeshFilter meshFilter = backboardRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return false;

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
        Vector3 scale = t.lossyScale;
        float sx = Mathf.Abs(meshSize.x * scale.x);
        float sy = Mathf.Abs(meshSize.y * scale.y);
        float sz = Mathf.Abs(meshSize.z * scale.z);

        Vector3 ax = t.right.normalized;
        Vector3 ay = t.up.normalized;
        Vector3 az = t.forward.normalized;

        int thicknessIndex = 0;
        int widthIndex = 2;
        int heightIndex = 1;
        SortAxesBySize(sx, sy, sz, ref thicknessIndex, ref heightIndex, ref widthIndex);

        Vector3[] axes = { ax, ay, az };
        float[] sizes = { sx, sy, sz };
        normal = axes[thicknessIndex];
        heightAxis = axes[heightIndex];
        widthAxis = axes[widthIndex];
        boardThickness = sizes[thicknessIndex];
        boardHeight = sizes[heightIndex];
        boardWidth = sizes[widthIndex];

        center = backboardRenderer.bounds.center;

        Vector3 toCamera = cachedCamera.transform.position - center;
        if (Vector3.Dot(normal, toCamera) < 0f)
            normal = -normal;
        if (Vector3.Dot(heightAxis, Vector3.up) < 0f)
            heightAxis = -heightAxis;
        if (Vector3.Dot(widthAxis, cachedCamera.transform.right) < 0f)
            widthAxis = -widthAxis;
        if (Vector3.Dot(Vector3.Cross(widthAxis, heightAxis), normal) < 0f)
            widthAxis = -widthAxis;

        return true;
    }

    Renderer ResolveBackboardRenderer()
    {
        if (cachedBackboardRenderer != null && cachedBackboardRenderer.gameObject.scene.IsValid())
            return cachedBackboardRenderer;

        GameObject root = string.IsNullOrWhiteSpace(backboardRootName) ? null : GameObject.Find(backboardRootName);
        if (root == null)
            root = GameObject.Find("Arena Static");
        if (root == null)
            return null;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Renderer best = null;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;
            if (renderer.GetComponent<MeshFilter>() == null) continue;

            Vector3 size = renderer.bounds.size;
            float major = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            float minor = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            float middle = size.x + size.y + size.z - major - minor;
            if (major < 3f || middle < 2f) continue;

            float score = renderer.bounds.center.z * 10f + major * 2f + middle;
            if (!string.IsNullOrWhiteSpace(backboardNameContains) &&
                renderer.name.IndexOf(backboardNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                score += 50f;

            if (best == null || score > bestScore)
            {
                best = renderer;
                bestScore = score;
            }
        }

        cachedBackboardRenderer = best;
        return cachedBackboardRenderer;
    }

    void ApplyBackboardVisibility(Renderer backboardRenderer)
    {
        if (backboardRenderer == null)
            return;

        bool targetVisible = !hideBackboardForExperiment;
        if (backboardRenderer.enabled != targetVisible)
            backboardRenderer.enabled = targetVisible;

        Collider collider = backboardRenderer.GetComponent<Collider>();
        if (collider != null && collider.enabled != targetVisible)
            collider.enabled = targetVisible;
    }

    void RestoreBackboardVisibility()
    {
        Renderer backboard = ResolveBackboardRenderer();
        if (backboard == null)
            return;

        if (!backboard.enabled)
            backboard.enabled = true;

        Collider collider = backboard.GetComponent<Collider>();
        if (collider != null && !collider.enabled)
            collider.enabled = true;
    }

    float EstimateSlotFaceSizeWorld()
    {
        GameObject prefabRoot = ResolveSlotPrefabRoot();
        if (prefabRoot == null)
            return 0.18f * Mathf.Max(0.01f, slotScale);

        if (!TryGetCombinedLocalMeshBounds(prefabRoot, out Bounds localBounds))
            return 0.18f * Mathf.Max(0.01f, slotScale);

        float x = Mathf.Abs(localBounds.size.x);
        float y = Mathf.Abs(localBounds.size.y);
        float z = Mathf.Abs(localBounds.size.z);
        int minIndex = 0;
        int midIndex = 1;
        int maxIndex = 2;
        SortAxesBySize(x, y, z, ref minIndex, ref midIndex, ref maxIndex);
        float[] values = { x, y, z };
        return values[midIndex] * Mathf.Max(0.01f, slotScale);
    }

    static bool TryGetCombinedLocalMeshBounds(GameObject root, out Bounds combinedBounds)
    {
        combinedBounds = default;
        if (root == null)
            return false;

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        if (meshFilters == null || meshFilters.Length == 0)
            return false;

        Matrix4x4 rootWorldToLocal = root.transform.worldToLocalMatrix;
        bool hasBounds = false;
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter mf = meshFilters[i];
            if (mf == null || mf.sharedMesh == null) continue;

            Matrix4x4 meshToRoot = rootWorldToLocal * mf.transform.localToWorldMatrix;
            Vector3[] corners = GetBoundsCorners(mf.sharedMesh.bounds);
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 p = meshToRoot.MultiplyPoint3x4(corners[c]);
                if (!hasBounds)
                {
                    combinedBounds = new Bounds(p, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(p);
                }
            }
        }

        return hasBounds;
    }

    static Vector3[] GetBoundsCorners(Bounds bounds)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        return new[]
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, max.y, max.z)
        };
    }

    static void SortAxesBySize(float a, float b, float c, ref int minIndex, ref int midIndex, ref int maxIndex)
    {
        float[] values = { a, b, c };
        int[] idx = { 0, 1, 2 };
        if (values[idx[0]] > values[idx[1]]) SwapIndices(idx, 0, 1);
        if (values[idx[1]] > values[idx[2]]) SwapIndices(idx, 1, 2);
        if (values[idx[0]] > values[idx[1]]) SwapIndices(idx, 0, 1);
        minIndex = idx[0];
        midIndex = idx[1];
        maxIndex = idx[2];
    }

    static void SwapIndices(int[] idx, int a, int b)
    {
        int t = idx[a];
        idx[a] = idx[b];
        idx[b] = t;
    }

    void UpdatePerkIcons()
    {
        if (cachedCamera == null)
            cachedCamera = GetComponent<Camera>();
        if (cachedCamera == null) return;
        if (!renderPerkIcons)
        {
            SetPerkIconsActive(false);
            return;
        }

        if (spawnedPerkRenderers.Count == 0 || spawnedSlots.Count == 0)
        {
            SetPerkIconsActive(false);
            return;
        }

        bool showIcons = !showOnlyDuringGameplay || IsGameplayActive();
        if (!showIcons)
        {
            SetPerkIconsActive(false);
            return;
        }

        EnsurePerkIconSlots(spawnedSlots.Count);

        for (int i = 0; i < spawnedSlots.Count && i < spawnedPerkRenderers.Count; i++)
        {
            GameObject slot = spawnedSlots[i];
            SpriteRenderer iconRenderer = spawnedPerkRenderers[i];
            GameObject iconRoot = spawnedPerkIcons[i];
            if (slot == null || iconRenderer == null || iconRoot == null)
                continue;

            Item item = GetInventorySlotItem(i);
            Sprite icon = ResolvePerkIcon(item);
            bool hasIcon = icon != null;
            bool slotVisible = slot.activeInHierarchy;
            bool showSlotIcon = hasIcon && slotVisible;
            iconRenderer.enabled = showSlotIcon;
            iconRoot.SetActive(showSlotIcon);

            if (!showSlotIcon)
                continue;

            if (iconRenderer.sprite != icon)
                iconRenderer.sprite = icon;

            if (TryGetSlotBounds(slot, out Bounds slotBounds))
            {
                float face = GetSlotFaceSize(slotBounds);
                float thickness = GetSlotThickness(slotBounds);
                float iconSize = Mathf.Max(0.01f, face * perkIconFill);
                Vector3 toCamera = (cachedCamera.transform.position - slotBounds.center).normalized;
                iconRoot.transform.position = slotBounds.center + toCamera * (thickness * 0.55f + perkIconDepthBias);
                iconRoot.transform.rotation = Quaternion.LookRotation(-toCamera, cachedCamera.transform.up);
                iconRoot.transform.localScale = Vector3.one;
                iconRenderer.size = new Vector2(iconSize, iconSize);
            }
            else
            {
                Vector3 toCamera = (cachedCamera.transform.position - slot.transform.position).normalized;
                iconRoot.transform.position = slot.transform.position + toCamera * perkIconDepthBias;
                iconRoot.transform.rotation = Quaternion.LookRotation(-toCamera, cachedCamera.transform.up);
                iconRoot.transform.localScale = Vector3.one;
                iconRenderer.size = new Vector2(0.9f, 0.9f);
            }
        }
    }

    void SetPerkIconsActive(bool isActive)
    {
        for (int i = 0; i < spawnedPerkIcons.Count; i++)
        {
            GameObject iconRoot = spawnedPerkIcons[i];
            if (iconRoot == null) continue;
            if (iconRoot.activeSelf != isActive)
                iconRoot.SetActive(isActive);

            if (i < spawnedPerkRenderers.Count && spawnedPerkRenderers[i] != null && !isActive)
                spawnedPerkRenderers[i].enabled = false;
        }
    }

    bool IsGameplayActive()
    {
        PlayerGUI playerGui = PlayerGUI.instance;
        if (playerGui == null) return true;
        return !playerGui.IsMainMenuVisible;
    }

    static Item GetInventorySlotItem(int index)
    {
        if (Inventory.Slots == null) return null;
        if (index < 0 || index >= Inventory.Slots.Length) return null;
        return Inventory.Slots[index];
    }

    static Sprite ResolvePerkIcon(Item item)
    {
        if (item == null || item is NoItem)
            return null;
        if (item.Icon != null)
            return item.Icon;

        string resourceName = ResolvePerkIconResourceName(item);
        if (string.IsNullOrEmpty(resourceName))
            return null;

        Sprite icon = Resources.Load<Sprite>($"{IconSpriteRoot}{resourceName}");
        if (icon != null)
            item.Icon = icon;
        return icon;
    }

    static string ResolvePerkIconResourceName(Item item)
    {
        if (item == null) return null;

        if (Items.instance != null)
        {
            if (item == Items.AngelWings) return "icon_angelwings";
            if (item == Items.ExtraBall) return "icon_extraball";
            if (item == Items.HealthBonus) return "icon_healthbonus";
            if (item == Items.PingPong) return "icon_pingpong";
            if (item == Items.TicketPrize) return "icon_ticketprize";
            if (item == Items.Fireball) return "icon_fireball";
            if (item == Items.WaterDroplet) return "icon_waterdroplet";
            if (item == Items.LuckyCharm) return "icon_luckycharm";
            if (item == Items.CurseOfAnubis) return "icon_curseofanubis";
            if (item == Items.CameraFlip) return "icon_cameraflip";
            if (item == Items.Rock) return "icon_rock";
            if (item == Items.TennisBall) return "icon_tennisball";
        }

        string typeName = item.GetType().Name;
        switch (typeName)
        {
            case nameof(AngelWings): return "icon_angelwings";
            case nameof(ExtraBall): return "icon_extraball";
            case nameof(HealthBonus): return "icon_healthbonus";
            case nameof(PingPong): return "icon_pingpong";
            case nameof(TicketPrize): return "icon_ticketprize";
            case nameof(Fireball): return "icon_fireball";
            case nameof(WaterDroplet): return "icon_waterdroplet";
            case nameof(LuckyCharm): return "icon_luckycharm";
            case nameof(CurseOfAnubis): return "icon_curseofanubis";
            case nameof(CameraFlip): return "icon_cameraflip";
            case nameof(Rock): return "icon_rock";
            case nameof(TennisBall): return "icon_tennisball";
            default: return null;
        }
    }

    static bool TryGetSlotBounds(GameObject slot, out Bounds bounds)
    {
        bounds = default;
        if (slot == null) return false;

        Renderer[] renderers = slot.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled) continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    static float GetSlotFaceSize(Bounds bounds)
    {
        float x = bounds.size.x;
        float y = bounds.size.y;
        float z = bounds.size.z;

        if (x < y) Swap(ref x, ref y);
        if (y < z) Swap(ref y, ref z);
        if (x < y) Swap(ref x, ref y);
        return y;
    }

    static float GetSlotThickness(Bounds bounds)
    {
        float x = bounds.size.x;
        float y = bounds.size.y;
        float z = bounds.size.z;

        if (x > y) Swap(ref x, ref y);
        if (y > z) Swap(ref y, ref z);
        if (x > y) Swap(ref x, ref y);
        return x;
    }

    static void Swap(ref float a, ref float b)
    {
        float tmp = a;
        a = b;
        b = tmp;
    }

    Quaternion BuildSurfaceAlignedRotation(Vector3 surfaceNormal)
    {
        Vector3 forward = -surfaceNormal;
        if (forward.sqrMagnitude < 0.0001f)
            forward = cachedCamera.transform.forward;

        forward.Normalize();
        Vector3 up = Vector3.ProjectOnPlane(cachedCamera.transform.up, forward);
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.ProjectOnPlane(Vector3.up, forward);
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;

        Quaternion surfaceRotation = Quaternion.LookRotation(forward, up.normalized);
        return surfaceRotation * Quaternion.Euler(slotEulerOffset);
    }

    void SetSpawnedSlotsActive(bool isActive)
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            GameObject slot = spawnedSlots[i];
            if (slot == null) continue;
            if (slot.activeSelf != isActive)
                slot.SetActive(isActive);
        }
    }

    bool TryGetInventorySlotWorldPositions(Vector3[] result)
    {
        if (cachedCamera == null || result == null || result.Length < DefaultSlotCount)
            return false;

        RectTransform inventoryRoot = GetInventoryRoot();
        if (inventoryRoot == null || !inventoryRoot.gameObject.activeInHierarchy)
            return false;

        RectTransform first = FindSlotRect(inventoryRoot, "First", "Main");
        RectTransform second = FindSlotRect(inventoryRoot, "Second");
        RectTransform third = FindSlotRect(inventoryRoot, "Third");
        if (first == null || second == null || third == null)
            return false;

        Camera uiCamera = null;
        Canvas canvas = inventoryRoot.GetComponentInParent<Canvas>(true);
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        if (!TryGetRectScreenCenter(first, uiCamera, out Vector2 p0)) return false;
        if (!TryGetRectScreenCenter(second, uiCamera, out Vector2 p1)) return false;
        if (!TryGetRectScreenCenter(third, uiCamera, out Vector2 p2)) return false;
        if (!AreScreenPointsUsable(p0, p1, p2))
        {
            if (!loggedUiFallback)
            {
                loggedUiFallback = true;
                Debug.LogWarning("PerkSlots3DOverlay: UI slot points are out of view, using viewport fallback.");
            }

            return false;
        }

        Vector3 n0 = Vector3.zero;
        Vector3 n1 = Vector3.zero;
        Vector3 n2 = Vector3.zero;
        Vector3 w0 = Vector3.zero;
        Vector3 w1 = Vector3.zero;
        Vector3 w2 = Vector3.zero;

        bool projected = false;
        if (projectSlotsToSurface &&
            TryBuildSurfacePlane((p0 + p1 + p2) / 3f, out Plane plane, out Vector3 planeNormal))
        {
            projected = TryProjectScreenPointToPlane(p0, plane, planeNormal, out w0, out n0) &&
                        TryProjectScreenPointToPlane(p1, plane, planeNormal, out w1, out n1) &&
                        TryProjectScreenPointToPlane(p2, plane, planeNormal, out w2, out n2);
        }

        if (!projected)
        {
            w0 = cachedCamera.ScreenToWorldPoint(new Vector3(p0.x, p0.y, slotDepth));
            w1 = cachedCamera.ScreenToWorldPoint(new Vector3(p1.x, p1.y, slotDepth));
            w2 = cachedCamera.ScreenToWorldPoint(new Vector3(p2.x, p2.y, slotDepth));
            Vector3 fallbackNormal = -cachedCamera.transform.forward;
            n0 = fallbackNormal;
            n1 = fallbackNormal;
            n2 = fallbackNormal;
        }

        Vector3 center = (w0 + w1 + w2) / 3f;
        result[0] = center + (w0 - center) * slotSpacingMultiplier + slotGroupWorldOffset;
        result[1] = center + (w1 - center) * slotSpacingMultiplier + slotGroupWorldOffset;
        result[2] = center + (w2 - center) * slotSpacingMultiplier + slotGroupWorldOffset;
        uiSlotSurfaceNormalCache[0] = n0;
        uiSlotSurfaceNormalCache[1] = n1;
        uiSlotSurfaceNormalCache[2] = n2;
        return true;
    }

    void ApplyRuntimeViewportFallback()
    {
        if (!Application.isPlaying) return;

        // Force a deterministic visible layout while we stabilize UI binding.
        mountSlotsToBackboard = false;
        hideBackboardForExperiment = false;
        followInventoryUiSlots = false;
        hideSlotsWhenInventoryHidden = false;
        useAutoLayout = true;
        autoSlotCount = DefaultSlotCount;
        autoStartViewportPosition = new Vector2(0.435f, 0.92f);
        autoViewportSpacing = new Vector2(0.065f, 0f);
        slotDepth = 6.8f;
        slotScale = 2.8f;
    }

    void ApplyRuntimeEmergencyProfile()
    {
        if (!Application.isPlaying) return;
        if (runtimeProfileApplied) return;

        ApplyRuntimeLowBackWall();
        ApplyRuntimeTightCamera();
        runtimeProfileApplied = true;
    }

    void ApplyRuntimeLowBackWall()
    {
        if (!enforceLowBackWallAtRuntime) return;

        GameObject arenaStatic = GameObject.Find("Arena Static");
        if (arenaStatic == null) return;

        Transform sideReference = null;
        Transform backWall = null;
        for (int i = 0; i < arenaStatic.transform.childCount; i++)
        {
            Transform child = arenaStatic.transform.GetChild(i);
            if (child == null || child.name != "Outer Edge") continue;

            Vector3 lp = child.localPosition;
            if (lp.z > 10f)
            {
                backWall = child;
            }
            else if (Mathf.Abs(lp.x) > 3f)
            {
                sideReference = child;
            }
        }

        if (backWall == null) return;

        Vector3 targetScale = sideReference != null ? sideReference.localScale : new Vector3(0.1571f, 1f, 10.195885f);
        float targetY = sideReference != null ? sideReference.localPosition.y : -6.6f;

        Vector3 backScale = backWall.localScale;
        backScale.x = targetScale.x;
        backScale.y = targetScale.y;
        backWall.localScale = backScale;

        Vector3 backPos = backWall.localPosition;
        backPos.y = targetY;
        backWall.localPosition = backPos;
    }

    void ApplyRuntimeTightCamera()
    {
        if (!enforceTightCameraAtRuntime) return;
        if (cachedCamera == null) return;

        Transform cameraTransform = cachedCamera.transform;
        cameraTransform.localPosition = runtimeCameraLocalPosition;
        cameraTransform.localRotation = Quaternion.Euler(runtimeCameraLocalEuler);
    }

    bool TryBuildSurfacePlane(Vector2 centerScreen, out Plane plane, out Vector3 normal)
    {
        plane = default;
        normal = Vector3.zero;

        if (cachedCamera == null) return false;

        Ray ray = cachedCamera.ScreenPointToRay(centerScreen);
        int mask = GetSurfaceRaycastMask();
        RaycastHit[] hits = Physics.RaycastAll(ray, surfaceRaycastDistance, mask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;

        bool found = false;
        float bestScore = float.NegativeInfinity;
        RaycastHit bestHit = default;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider collider = hit.collider;
            if (collider == null) continue;
            if (collider.attachedRigidbody != null) continue;
            if (collider.transform.IsChildOf(transform)) continue;

            float verticalPenalty = Mathf.Abs(Vector3.Dot(hit.normal.normalized, Vector3.up));
            float facingScore = Vector3.Dot(hit.normal.normalized, -ray.direction.normalized);
            float score = facingScore * 2f + (1f - verticalPenalty) + hit.point.y * 0.02f;

            if (!found || score > bestScore)
            {
                found = true;
                bestScore = score;
                bestHit = hit;
            }
        }

        if (!found) return false;

        normal = bestHit.normal.normalized;
        plane = new Plane(normal, bestHit.point);
        return true;
    }

    bool TryProjectScreenPointToPlane(Vector2 screenPoint, Plane plane, Vector3 planeNormal, out Vector3 worldPoint, out Vector3 worldNormal)
    {
        worldPoint = default;
        worldNormal = planeNormal;

        if (cachedCamera == null) return false;
        Ray ray = cachedCamera.ScreenPointToRay(screenPoint);
        if (!plane.Raycast(ray, out float distance)) return false;
        if (distance <= 0f) return false;

        worldPoint = ray.GetPoint(distance) + planeNormal * surfaceOffset;
        return true;
    }

    int GetSurfaceRaycastMask()
    {
        int mask = surfaceLayerMask.value;
        return mask == 0 ? Physics.DefaultRaycastLayers : mask;
    }

    RectTransform GetInventoryRoot()
    {
        if (cachedInventoryRoot != null &&
            cachedInventoryRoot.gameObject.scene.IsValid() &&
            cachedInventoryRoot.gameObject.activeInHierarchy &&
            IsPreferredInventoryRoot(cachedInventoryRoot))
            return cachedInventoryRoot;

        RectTransform[] rects = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        RectTransform bestActive = null;
        float bestActiveScore = float.NegativeInfinity;
        RectTransform fallback = null;
        float fallbackScore = float.NegativeInfinity;

        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (!IsInventoryRootCandidate(rect)) continue;

            float score = ScoreInventoryRoot(rect);
            if (rect.gameObject.activeInHierarchy)
            {
                if (bestActive == null || score > bestActiveScore)
                {
                    bestActive = rect;
                    bestActiveScore = score;
                }
            }
            else if (fallback == null || score > fallbackScore)
            {
                fallback = rect;
                fallbackScore = score;
            }
        }

        cachedInventoryRoot = bestActive != null ? bestActive : fallback;
        return cachedInventoryRoot;
    }

    bool IsInventoryRootCandidate(RectTransform rect)
    {
        if (rect == null || rect.name != inventoryRootName)
            return false;

        RectTransform first = FindSlotRect(rect, "First");
        RectTransform second = FindSlotRect(rect, "Second");
        RectTransform third = FindSlotRect(rect, "Third");
        return first != null && second != null && third != null;
    }

    bool IsPreferredInventoryRoot(RectTransform rect)
    {
        if (rect == null) return false;
        if (HasAncestorNameContaining(rect.transform, "Play Panel")) return true;
        if (rect.Find("My powerups") != null) return true;
        return rect.Find("First") != null;
    }

    float ScoreInventoryRoot(RectTransform rect)
    {
        float score = Mathf.Abs(rect.rect.width * rect.rect.height) * 0.001f;
        if (rect.Find("First") != null) score += 50f;
        if (rect.Find("My powerups") != null) score += 80f;
        if (HasAncestorNameContaining(rect.transform, "Play Panel")) score += 100f;
        return score;
    }

    bool AreScreenPointsUsable(Vector2 p0, Vector2 p1, Vector2 p2)
    {
        return IsScreenPointUsable(p0) && IsScreenPointUsable(p1) && IsScreenPointUsable(p2);
    }

    bool IsScreenPointUsable(Vector2 screenPoint)
    {
        if (float.IsNaN(screenPoint.x) || float.IsNaN(screenPoint.y)) return false;
        if (float.IsInfinity(screenPoint.x) || float.IsInfinity(screenPoint.y)) return false;

        Vector3 viewport = cachedCamera.ScreenToViewportPoint(new Vector3(screenPoint.x, screenPoint.y, slotDepth));
        return viewport.x >= -0.1f && viewport.x <= 1.1f && viewport.y >= -0.1f && viewport.y <= 1.1f;
    }

    static bool HasAncestorNameContaining(Transform transformNode, string token)
    {
        if (transformNode == null || string.IsNullOrEmpty(token))
            return false;

        for (Transform current = transformNode; current != null; current = current.parent)
        {
            if (current.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    static RectTransform FindSlotRect(RectTransform parent, params string[] names)
    {
        if (parent == null || names == null) return null;

        for (int i = 0; i < names.Length; i++)
        {
            Transform child = parent.Find(names[i]);
            if (child is RectTransform rect)
                return rect;
        }

        return null;
    }

    static bool TryGetRectScreenCenter(RectTransform rect, Camera uiCamera, out Vector2 screenPoint)
    {
        screenPoint = default;
        if (rect == null) return false;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
        return true;
    }

    void ApplyRuntimeCornerRounding(GameObject slotRoot)
    {
        if (!applyRuntimeCornerRounding) return;
        if (slotRoot == null) return;
        if (runtimeOuterCornerRadius <= 0.0001f) return;

        MeshFilter[] meshFilters = slotRoot.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter == null || meshFilter.sharedMesh == null) continue;

            Mesh sourceMesh = meshFilter.sharedMesh;
            if (!roundedMeshCache.TryGetValue(sourceMesh, out Mesh roundedMesh) || roundedMesh == null)
            {
                roundedMesh = CreateRoundedCornerMesh(sourceMesh, runtimeOuterCornerRadius);
                roundedMeshCache[sourceMesh] = roundedMesh;
            }

            meshFilter.sharedMesh = roundedMesh;

            MeshCollider collider = meshFilter.GetComponent<MeshCollider>();
            if (collider != null)
                collider.sharedMesh = roundedMesh;
        }
    }

    static Mesh CreateRoundedCornerMesh(Mesh sourceMesh, float cornerRadius)
    {
        Mesh rounded = Instantiate(sourceMesh);
        rounded.name = $"{sourceMesh.name}_RoundedRuntime";

        Vector3[] vertices = rounded.vertices;
        Bounds bounds = rounded.bounds;
        float halfX = bounds.extents.x;
        float halfZ = bounds.extents.z;

        if (halfX <= 0.0001f || halfZ <= 0.0001f)
            return rounded;

        float radius = Mathf.Min(cornerRadius, halfX * 0.45f, halfZ * 0.45f);
        if (radius <= 0.0001f)
            return rounded;

        float startX = halfX - radius;
        float startZ = halfZ - radius;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            float absX = Mathf.Abs(v.x);
            float absZ = Mathf.Abs(v.z);

            if (absX <= startX || absZ <= startZ)
                continue;

            float localX = absX - startX;
            float localZ = absZ - startZ;
            float length = Mathf.Sqrt(localX * localX + localZ * localZ);

            if (length <= radius || length <= 0.0001f)
                continue;

            float invLength = 1f / length;
            localX *= radius * invLength;
            localZ *= radius * invLength;

            absX = startX + localX;
            absZ = startZ + localZ;

            v.x = Mathf.Sign(v.x) * absX;
            v.z = Mathf.Sign(v.z) * absZ;
            vertices[i] = v;
        }

        rounded.vertices = vertices;
        rounded.RecalculateNormals();
        rounded.RecalculateTangents();
        rounded.RecalculateBounds();
        return rounded;
    }

    int GetTargetSlotCount()
    {
        if (useAutoLayout)
            return Mathf.Max(DefaultSlotCount, autoSlotCount);

        if (viewportPositions == null || viewportPositions.Length == 0)
            return DefaultSlotCount;

        return viewportPositions.Length;
    }

    Vector2 GetViewportPosition(int index)
    {
        if (useAutoLayout)
        {
            return new Vector2(
                autoStartViewportPosition.x + autoViewportSpacing.x * index,
                autoStartViewportPosition.y + autoViewportSpacing.y * index);
        }

        if (viewportPositions != null && index >= 0 && index < viewportPositions.Length)
            return viewportPositions[index];

        return new Vector2(0.365f + 0.10f * index, 0.815f);
    }

    void HideLegacySquares()
    {
        if (!hideLegacySquares) return;

        RectTransform[] rects = Object.FindObjectsOfType<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null) continue;
            if (rect.name != "Main" && rect.name != "First" && rect.name != "Second" && rect.name != "Third") continue;

            Image image = rect.GetComponent<Image>();
            if (image == null) continue;

            Color color = image.color;
            if (color.a > 0f)
            {
                color.a = 0f;
                image.color = color;
            }

            if (disableLegacyRaycast)
                image.raycastTarget = false;
        }
    }
}
