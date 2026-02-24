using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plunger : MonoBehaviour
{
    static readonly Vector3 RuntimeSpriteScale = new Vector3(0.029f, 0.03f, 1f);
    static readonly Vector3 RuntimeSpriteOffset = new Vector3(0f, -0.0065f, 0.001f);
    static readonly Vector3 RuntimeFacingOffsetEuler = new Vector3(-13f, 3f, 12f);

    enum CompressionAxis
    {
        X,
        Y,
        Z
    }

    // Same-scene "singleton" pattern 
    private static Plunger _instance;
    public static Plunger instance
    {
        get
        {
            if (!_instance)
                _instance = FindObjectOfType<Plunger>();
            return _instance;
        }
    }

    [SerializeField]
    Animator Spring;

    [Header("Visual Compression")]
    [SerializeField]
    Transform CompressionVisual;

    [SerializeField, Range(0.1f, 1f)]
    float MinCompression = 0.3f;

    [SerializeField]
    CompressionAxis Axis = CompressionAxis.Z;

    [SerializeField]
    string SpriteResourcePath = "UI/Sprites/PSG-Ball/plunger";

    [SerializeField]
    Vector3 SpriteLocalScale = new Vector3(0.022f, 0.028f, 1f);

    [SerializeField]
    Vector3 SpriteLocalOffset = new Vector3(0f, -0.004f, 0.001f);

    [SerializeField]
    Vector3 SpriteFacingOffsetEuler = new Vector3(-12f, 0f, 0f);

    [Header("Debug Reference")]
    [SerializeField]
    bool ShowLegacySpringReference = false;

    [SerializeField]
    Vector2 LegacyReferenceViewport = new Vector2(0.13f, 0.3f);

    [SerializeField]
    float LegacyReferenceViewportDepth = 1.2f;

    [SerializeField]
    float LegacyReferenceScaleFactor = 0.28f;

    [SerializeField]
    bool LegacyReferenceKeepOriginalAngle = true;

    [SerializeField]
    bool ShowStyledSteelReference = true;

    [SerializeField]
    Vector2 StyledReferenceViewportOffset = new Vector2(0.11f, 0f);

    [SerializeField]
    float StyledReferenceScaleFactor = 0.28f;

    [SerializeField]
    Color StyledSteelColor = new Color(0.76f, 0.79f, 0.84f, 1f);

    [SerializeField, Range(0f, 1f)]
    float StyledSteelMetallic = 0.95f;

    [SerializeField, Range(0f, 1f)]
    float StyledSteelSmoothness = 0.92f;

    [SerializeField]
    bool StyledAddTopKnob = true;

    [SerializeField, Range(0.1f, 2f)]
    float StyledTopKnobScale = 0.7f;

    [SerializeField]
    bool UseLegacySpringAnimation;

    [Header("Sound Effects")]
    [SerializeField]
    AudioClip stress;

    [SerializeField]
    AudioClip fail;

    [SerializeField]
    AudioClip launch;

    AudioSource speaker;
    Vector3 compressionBaseScale = Vector3.one;
    SpriteRenderer plungerSpriteRenderer;
    Transform legacyReferenceTransform;
    Transform styledReferenceTransform;
    Transform styledTopKnobTransform;
    Transform gameplayTopKnobTransform;
    Material styledSteelMaterial;
    Material styledTopKnobMaterial;
    Mesh styledSmoothMesh;
    Vector3 legacyReferenceBaseWorldScale = Vector3.zero;
    Quaternion legacyReferenceBaseWorldRotation = Quaternion.identity;

    [HideInInspector]
    public List<Rigidbody> ObjectsInSpring = new List<Rigidbody>();

    void Awake()
    {
        // Keep only the gameplay plunger styled mesh; no debug references.
        ShowLegacySpringReference = false;
        ShowStyledSteelReference = false;

        speaker = GetComponent<AudioSource>();
        OverrideClipIfFound(ref stress, "plunger_pull_tension_loop");
        OverrideClipIfFound(ref fail, "ui_purchase_fail");
        OverrideClipIfFound(ref launch, "plunger_release_thunk");

        if (CompressionVisual == null && Spring != null)
            CompressionVisual = Spring.transform;

        SetupGameplayStyledSpring();

        if (CompressionVisual != null)
            compressionBaseScale = CompressionVisual.localScale;

        if (!UseLegacySpringAnimation && Spring != null)
            Spring.enabled = false;

        ResetCompression();
    }

    void SetupGameplayStyledSpring()
    {
        Transform springTransform = Spring != null ? Spring.transform : CompressionVisual;
        if (springTransform == null) return;

        Transform parent = springTransform.parent;
        if (parent != null)
        {
            Transform legacyRef = parent.Find("LegacySpringReference");
            if (legacyRef != null) legacyRef.gameObject.SetActive(false);

            Transform styledRef = parent.Find("LegacySpringStyledReference");
            if (styledRef != null) styledRef.gameObject.SetActive(false);
        }

        Transform spriteTransform = springTransform.Find("PlungerSpriteVisual");
        if (spriteTransform != null)
            spriteTransform.gameObject.SetActive(false);
        plungerSpriteRenderer = null;

        MeshFilter meshFilter = springTransform.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = springTransform.GetComponent<MeshRenderer>();
        if (meshFilter == null || meshRenderer == null) return;

        meshFilter.sharedMesh = GetSmoothedStyledMesh(meshFilter.sharedMesh);

        if (styledSteelMaterial == null)
            styledSteelMaterial = CreateStyledSteelMaterial(meshRenderer);

        int materialCount = Mathf.Max(1, meshRenderer.sharedMaterials.Length);
        Material[] materials = new Material[materialCount];
        for (int i = 0; i < materialCount; i++)
            materials[i] = styledSteelMaterial;
        meshRenderer.sharedMaterials = materials;
        meshRenderer.enabled = true;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        SetupGameplayTopKnob(springTransform, meshFilter.sharedMesh);

        CompressionVisual = springTransform;
        Axis = CompressionAxis.Z;
        UseLegacySpringAnimation = false;
    }

    void SetupGameplayTopKnob(Transform springTransform, Mesh mesh)
    {
        if (!StyledAddTopKnob || springTransform == null || mesh == null)
        {
            if (gameplayTopKnobTransform != null)
                gameplayTopKnobTransform.gameObject.SetActive(false);
            return;
        }

        if (gameplayTopKnobTransform == null)
        {
            Transform found = springTransform.Find("GameplayTopKnob");
            if (found != null)
            {
                gameplayTopKnobTransform = found;
            }
            else
            {
                GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                knob.name = "GameplayTopKnob";
                gameplayTopKnobTransform = knob.transform;
                gameplayTopKnobTransform.SetParent(springTransform, false);
                Collider knobCollider = knob.GetComponent<Collider>();
                if (knobCollider != null) Destroy(knobCollider);
            }
        }

        MeshRenderer knobRenderer = gameplayTopKnobTransform.GetComponent<MeshRenderer>();
        if (knobRenderer != null)
        {
            if (styledTopKnobMaterial == null)
            {
                styledTopKnobMaterial = styledSteelMaterial != null
                    ? new Material(styledSteelMaterial)
                    : CreateFallbackLitMaterial();
                styledTopKnobMaterial.name = "GameplayTopKnobSteel_Runtime";
                if (styledTopKnobMaterial.HasProperty("_Color"))
                    styledTopKnobMaterial.SetColor("_Color", new Color(0.16f, 0.16f, 0.18f, 1f));
                if (styledTopKnobMaterial.HasProperty("_BaseColor"))
                    styledTopKnobMaterial.SetColor("_BaseColor", new Color(0.16f, 0.16f, 0.18f, 1f));
                if (styledTopKnobMaterial.HasProperty("_Metallic"))
                    styledTopKnobMaterial.SetFloat("_Metallic", 0.85f);
                if (styledTopKnobMaterial.HasProperty("_Glossiness"))
                    styledTopKnobMaterial.SetFloat("_Glossiness", 0.96f);
                if (styledTopKnobMaterial.HasProperty("_Smoothness"))
                    styledTopKnobMaterial.SetFloat("_Smoothness", 0.96f);
            }

            knobRenderer.sharedMaterial = styledTopKnobMaterial;
            knobRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            knobRenderer.receiveShadows = false;
        }

        Bounds b = mesh.bounds;
        float knobDiameter = Mathf.Max(b.size.x, b.size.y) * 0.55f * StyledTopKnobScale;
        float knobRadius = knobDiameter * 0.5f;
        gameplayTopKnobTransform.localScale = new Vector3(knobDiameter, knobDiameter, knobDiameter);
        gameplayTopKnobTransform.localPosition = new Vector3(0f, 0f, b.max.z + knobRadius * 0.85f);
        gameplayTopKnobTransform.localRotation = Quaternion.identity;
        gameplayTopKnobTransform.gameObject.SetActive(true);
    }

    void SetupSpriteVisual()
    {
        if (CompressionVisual == null) return;

        Transform springTransform = CompressionVisual;
        Sprite sprite = Resources.Load<Sprite>(SpriteResourcePath);
        if (sprite == null) return;

        MeshRenderer oldMeshRenderer = springTransform.GetComponent<MeshRenderer>();
        MeshFilter oldMeshFilter = springTransform.GetComponent<MeshFilter>();

        SetupLegacyReferenceIfNeeded(springTransform, oldMeshFilter, oldMeshRenderer);

        if (oldMeshRenderer != null)
            oldMeshRenderer.enabled = false;

        Transform spriteTransform = springTransform.Find("PlungerSpriteVisual");
        if (spriteTransform == null)
        {
            GameObject spriteObject = new GameObject("PlungerSpriteVisual");
            spriteTransform = spriteObject.transform;
            spriteTransform.SetParent(springTransform, false);
        }

        SpriteRenderer spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = spriteTransform.gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 5;
        plungerSpriteRenderer = spriteRenderer;

        // Force runtime tuning values so scene/prefab overrides do not keep an old large size.
        spriteTransform.localPosition = RuntimeSpriteOffset;
        spriteTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spriteTransform.localScale = RuntimeSpriteScale;

        CompressionVisual = spriteTransform;
        Axis = CompressionAxis.Y;
        UseLegacySpringAnimation = false;
    }

    void SetupLegacyReferenceIfNeeded(Transform springTransform, MeshFilter oldMeshFilter, MeshRenderer oldMeshRenderer)
    {
        // Hide stale legacy reference object from previous runs.
        if (!ShowLegacySpringReference)
        {
            if (legacyReferenceTransform == null)
                legacyReferenceTransform = springTransform.parent != null
                    ? springTransform.parent.Find("LegacySpringReference")
                    : null;

            if (legacyReferenceTransform != null)
                legacyReferenceTransform.gameObject.SetActive(false);
        }

        if (oldMeshFilter == null || oldMeshRenderer == null)
            return;

        if (!ShowLegacySpringReference)
        {
            if (legacyReferenceTransform != null)
                legacyReferenceTransform.gameObject.SetActive(false);
        }
        else
        {
            if (legacyReferenceTransform == null)
            {
                Transform found = springTransform.parent != null
                    ? springTransform.parent.Find("LegacySpringReference")
                    : null;

                if (found != null)
                {
                    legacyReferenceTransform = found;
                }
                else
                {
                    GameObject go = new GameObject("LegacySpringReference");
                    Transform parent = springTransform.parent != null ? springTransform.parent : springTransform;
                    go.transform.SetParent(parent, false);
                    legacyReferenceTransform = go.transform;
                }
            }

            MeshFilter refFilter = legacyReferenceTransform.GetComponent<MeshFilter>();
            if (refFilter == null)
                refFilter = legacyReferenceTransform.gameObject.AddComponent<MeshFilter>();

            MeshRenderer refRenderer = legacyReferenceTransform.GetComponent<MeshRenderer>();
            if (refRenderer == null)
                refRenderer = legacyReferenceTransform.gameObject.AddComponent<MeshRenderer>();

            refFilter.sharedMesh = oldMeshFilter.sharedMesh;
            refRenderer.sharedMaterials = oldMeshRenderer.sharedMaterials;
            refRenderer.enabled = true;
            refRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            refRenderer.receiveShadows = false;
            legacyReferenceTransform.gameObject.SetActive(true);
        }

        legacyReferenceBaseWorldScale = springTransform.lossyScale;
        legacyReferenceBaseWorldRotation = springTransform.rotation;

        SetupStyledSteelReferenceIfNeeded(oldMeshFilter, oldMeshRenderer);
        PositionLegacyReferenceStandalone();
    }

    void SetupStyledSteelReferenceIfNeeded(MeshFilter oldMeshFilter, MeshRenderer oldMeshRenderer)
    {
        if (!ShowStyledSteelReference || oldMeshFilter == null)
        {
            if (styledReferenceTransform != null)
                styledReferenceTransform.gameObject.SetActive(false);
            return;
        }

        if (styledReferenceTransform == null)
        {
            GameObject go = new GameObject("LegacySpringStyledReference");
            styledReferenceTransform = go.transform;
        }

        MeshFilter styledFilter = styledReferenceTransform.GetComponent<MeshFilter>();
        if (styledFilter == null)
            styledFilter = styledReferenceTransform.gameObject.AddComponent<MeshFilter>();

        MeshRenderer styledRenderer = styledReferenceTransform.GetComponent<MeshRenderer>();
        if (styledRenderer == null)
            styledRenderer = styledReferenceTransform.gameObject.AddComponent<MeshRenderer>();

        styledFilter.sharedMesh = GetSmoothedStyledMesh(oldMeshFilter.sharedMesh);

        if (styledSteelMaterial == null)
            styledSteelMaterial = CreateStyledSteelMaterial(oldMeshRenderer);

        int materialCount = Mathf.Max(1, oldMeshRenderer != null ? oldMeshRenderer.sharedMaterials.Length : 1);
        Material[] materials = new Material[materialCount];
        for (int i = 0; i < materialCount; i++)
            materials[i] = styledSteelMaterial;
        styledRenderer.sharedMaterials = materials;
        styledRenderer.enabled = true;
        styledRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        styledRenderer.receiveShadows = false;
        styledReferenceTransform.gameObject.SetActive(true);

        SetupStyledTopKnob(styledFilter.sharedMesh);
    }

    Material CreateStyledSteelMaterial(MeshRenderer oldMeshRenderer)
    {
        Material source = null;
        if (oldMeshRenderer != null && oldMeshRenderer.sharedMaterials != null && oldMeshRenderer.sharedMaterials.Length > 0)
            source = oldMeshRenderer.sharedMaterials[0];

        Material material = source != null
            ? new Material(source)
            : CreateFallbackLitMaterial();
        material.name = "LegacySpringStyledSteel_Runtime";

        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", null);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", new Color(0.93f, 0.95f, 0.98f, 1f));
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.93f, 0.95f, 0.98f, 1f));
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 1f);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0.97f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.97f);
        if (material.HasProperty("_SpecularHighlights"))
            material.SetFloat("_SpecularHighlights", 1f);
        if (material.HasProperty("_EnvironmentReflections"))
            material.SetFloat("_EnvironmentReflections", 1f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.06f, 0.065f, 0.07f, 1f));
        }

        return material;
    }

    Material CreateFallbackLitMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null)
            shader = Shader.Find("Lit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        return new Material(shader);
    }

    Mesh GetSmoothedStyledMesh(Mesh source)
    {
        if (source == null) return null;

        if (styledSmoothMesh == null)
        {
            styledSmoothMesh = Instantiate(source);
            styledSmoothMesh.name = $"{source.name}_StyledSmooth_Runtime";
            styledSmoothMesh.RecalculateNormals();
            styledSmoothMesh.RecalculateTangents();
        }

        return styledSmoothMesh;
    }

    void SetupStyledTopKnob(Mesh mesh)
    {
        if (!StyledAddTopKnob)
        {
            if (styledTopKnobTransform != null)
                styledTopKnobTransform.gameObject.SetActive(false);
            return;
        }

        if (styledReferenceTransform == null || mesh == null) return;

        if (styledTopKnobTransform == null)
        {
            GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "StyledTopKnob";
            styledTopKnobTransform = knob.transform;
            styledTopKnobTransform.SetParent(styledReferenceTransform, false);

            Collider knobCollider = knob.GetComponent<Collider>();
            if (knobCollider != null)
                Destroy(knobCollider);
        }

        MeshRenderer knobRenderer = styledTopKnobTransform.GetComponent<MeshRenderer>();
        if (knobRenderer != null)
        {
            if (styledTopKnobMaterial == null)
            {
                styledTopKnobMaterial = styledSteelMaterial != null
                    ? new Material(styledSteelMaterial)
                    : CreateFallbackLitMaterial();
                styledTopKnobMaterial.name = "LegacySpringStyledKnob_Runtime";
                styledTopKnobMaterial.SetColor("_Color", new Color(0.16f, 0.16f, 0.18f, 1f));
                if (styledTopKnobMaterial.HasProperty("_BaseColor"))
                    styledTopKnobMaterial.SetColor("_BaseColor", new Color(0.16f, 0.16f, 0.18f, 1f));
                if (styledTopKnobMaterial.HasProperty("_Metallic"))
                    styledTopKnobMaterial.SetFloat("_Metallic", 0.85f);
                if (styledTopKnobMaterial.HasProperty("_Glossiness"))
                    styledTopKnobMaterial.SetFloat("_Glossiness", 0.96f);
                if (styledTopKnobMaterial.HasProperty("_Smoothness"))
                    styledTopKnobMaterial.SetFloat("_Smoothness", 0.96f);
            }
            knobRenderer.sharedMaterial = styledTopKnobMaterial;
            knobRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            knobRenderer.receiveShadows = false;
        }

        Bounds b = mesh.bounds;
        float knobDiameter = Mathf.Max(b.size.x, b.size.y) * 0.55f * StyledTopKnobScale;
        float knobRadius = knobDiameter * 0.5f;
        styledTopKnobTransform.localScale = new Vector3(knobDiameter, knobDiameter, knobDiameter);
        styledTopKnobTransform.localPosition = new Vector3(0f, 0f, b.max.z + knobRadius * 0.85f);
        styledTopKnobTransform.localRotation = Quaternion.identity;
        styledTopKnobTransform.gameObject.SetActive(true);
    }

    void PositionLegacyReferenceStandalone()
    {
        if (Camera.main != null)
        {
            Vector3 baseScale = legacyReferenceBaseWorldScale == Vector3.zero
                ? new Vector3(35f, 35f, 35f)
                : legacyReferenceBaseWorldScale;

            if (ShowLegacySpringReference && legacyReferenceTransform != null)
            {
                if (legacyReferenceTransform.parent != null)
                    legacyReferenceTransform.SetParent(null, true);

                Vector3 viewportPoint = new Vector3(
                    Mathf.Clamp01(LegacyReferenceViewport.x),
                    Mathf.Clamp01(LegacyReferenceViewport.y),
                    Mathf.Max(0.2f, LegacyReferenceViewportDepth));
                legacyReferenceTransform.position = Camera.main.ViewportToWorldPoint(viewportPoint);
                legacyReferenceTransform.rotation = LegacyReferenceKeepOriginalAngle
                    ? legacyReferenceBaseWorldRotation
                    : Camera.main.transform.rotation;
                legacyReferenceTransform.localScale = baseScale * Mathf.Max(0.05f, LegacyReferenceScaleFactor);
            }

            if (ShowStyledSteelReference && styledReferenceTransform != null)
            {
                Vector2 styledViewport = LegacyReferenceViewport + StyledReferenceViewportOffset;
                Vector3 styledPoint = new Vector3(
                    Mathf.Clamp01(styledViewport.x),
                    Mathf.Clamp01(styledViewport.y),
                    Mathf.Max(0.2f, LegacyReferenceViewportDepth));
                styledReferenceTransform.position = Camera.main.ViewportToWorldPoint(styledPoint);
                styledReferenceTransform.rotation = LegacyReferenceKeepOriginalAngle
                    ? legacyReferenceBaseWorldRotation
                    : Camera.main.transform.rotation;
                styledReferenceTransform.localScale = baseScale * Mathf.Max(0.05f, StyledReferenceScaleFactor);
                styledReferenceTransform.gameObject.SetActive(true);
            }
            return;
        }

        // Fallback when camera is not yet available.
        if (ShowLegacySpringReference && legacyReferenceTransform != null)
        {
            legacyReferenceTransform.localPosition = new Vector3(-0.03f, 0.012f, 0f);
            legacyReferenceTransform.localRotation = Quaternion.identity;
            legacyReferenceTransform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        }

        if (styledReferenceTransform != null)
        {
            styledReferenceTransform.localPosition = new Vector3(-0.01f, 0.012f, 0f);
            styledReferenceTransform.localRotation = Quaternion.identity;
            styledReferenceTransform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        }
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        if (plungerSpriteRenderer != null && CompressionVisual != null)
        {
            // Face the gameplay camera with a slight tilt to match table perspective.
            CompressionVisual.rotation = Camera.main.transform.rotation * Quaternion.Euler(RuntimeFacingOffsetEuler);
        }

        if ((ShowLegacySpringReference && legacyReferenceTransform != null) ||
            (ShowStyledSteelReference && styledReferenceTransform != null))
            PositionLegacyReferenceStandalone();
    }

    void OverrideClipIfFound(ref AudioClip slot, string clipName)
    {
        AudioClip clip = SoundCatalog.Get(clipName);
        if (clip)
            slot = clip;
    }

    void OnTriggerEnter(Collider c)
    {
        if (c.tag == "Ball")
            ObjectsInSpring.Add(c.GetComponent<Rigidbody>());
    }

    void OnTriggerExit(Collider c)
    {
        if (c.tag == "Ball")
            ObjectsInSpring.Remove(c.GetComponent<Rigidbody>());
    }

    public void Retract()
    {
        PlaySound("STRESS");

        if (UseLegacySpringAnimation && Spring != null)
            Spring.Play("Stress");
    }

    public void Fail() => PlaySound("FAIL");

    public void Release()
    {
        PlaySound("LAUNCH");

        if (UseLegacySpringAnimation && Spring != null)
            Spring.Play("Release");

        ResetCompression();
    }

    public void SetCompression(float currentCompression)
    {
        if (CompressionVisual == null) return;

        float clamped = Mathf.Clamp(currentCompression, MinCompression, 1f);
        Vector3 scale = compressionBaseScale;
        switch (Axis)
        {
            case CompressionAxis.X:
                scale.x = compressionBaseScale.x * clamped;
                break;
            case CompressionAxis.Y:
                scale.y = compressionBaseScale.y * clamped;
                break;
            case CompressionAxis.Z:
                scale.z = compressionBaseScale.z * clamped;
                break;
        }

        CompressionVisual.localScale = scale;
    }

    public void ResetCompression()
    {
        if (CompressionVisual == null) return;
        CompressionVisual.localScale = compressionBaseScale;
    }

    void PlaySound(string soundKey)
    {
        if (!speaker) return;

        speaker.Stop();
        
        switch(soundKey)
        {
            case "LAUNCH":
                speaker.PlayOneShot(launch);
                break;
            case "FAIL":
                speaker.PlayOneShot(fail);
                break;
            case "STRESS":
                speaker.PlayOneShot(stress);
                break;
        }
    } 

}
