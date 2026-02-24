using System;
using System.Reflection;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerGUI : MonoBehaviour
{
    const string RuntimePlayPanelRootName = "PSG1_PlayPanel";

    // Same-scene "singleton" pattern 
    private static PlayerGUI _instance;
    public static PlayerGUI instance
    {
        get
        {
            if (!_instance)
                _instance = FindObjectOfType<PlayerGUI>();
            return _instance;
        }
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField]
    TMP_Text FPS;
#endif

    [Header("Important Buttons")]
    [SerializeField]
    Button Play;

    [SerializeField]
    Button PlayAgain;

    [SerializeField]
    Button Reset;

    [SerializeField]
    Button DeleteProgress;

    [Header("Debug UI")]
    [SerializeField]
    bool ShowDangerButtons = false;

    [SerializeField]
    bool DisableLegacyPopups = true;

    [Header("Panels")]
    [SerializeField]
    GameObject PlayPanel;

    [SerializeField]
    GameObject GameOverPanel;

    public bool IsMainMenuVisible
    {
        get => PlayPanel != null && PlayPanel.activeSelf;
    }

    public GameObject PlayPanelRoot
    {
        get => PlayPanel;
    }

    [Header("Panel Labels")]
    [SerializeField]
    TMP_Text GameOverLabel;

    [Header("Player labels")]
    public TMP_Text Score;

    public TMP_Text Multiplier;

    public TMP_Text Tickets;

    public TMP_Text PlayMenuTickets;

    public TMP_Text Lives;

    [Header("Info Panel")]
    public TMP_Text InfoName;
    
    public TMP_Text InfoDescription;

    [Header("Other Labels/Fields")]
    [SerializeField]
    TMP_InputField HighScore;

    const string HudRetroTmpResource = "UI/Fonts/RetroArcade SDF";
    const string HudRetroFontResource = "UI/Fonts/RetroArcade";
    const string HudNunitoFontResource = "UI/Fonts/Nunito-ExtraBold";
    const string HudNunitoTmpResource = "UI/Fonts/Nunito-ExtraBold SDF";
    const string HudFallbackTmpResource = "Fonts & Materials/LiberationSans SDF";
    static readonly Color GameplayPaperColor = new Color32(0xE5, 0xE5, 0xE5, 0xFF);
    static readonly Color FlipperGoldColor = new Color32(0xFF, 0xD7, 0x00, 0xFF);
    static readonly Color SlingshotTerracottaColor = new Color32(0xBF, 0x68, 0x44, 0xFF);
    GameObject gameplayBackgroundPlane;
    Material gameplayBackgroundMaterial;
    Material flipperMaterial;
    Material slingshotBaseMaterial;
    Material slingshotLitMaterial;
    bool popupTriggersDisabled;
    GameObject cachedInfoPanel;
    GameObject cachedTutorialPanel;
    GameObject cachedAchievementToast;
    GameObject cachedAchievementGui;
    TMP_FontAsset hudFontAsset;
    TMP_Text perkActivationHintText;
    bool gameOverReturnInProgress;

    float deltaTime = 0, refreshRate = 0;

    void Awake()
    {
#if UNITY_EDITOR
        FPS.gameObject.SetActive(true);
#endif
        EnsurePerkSlotsOverlay();
        EnsureHudTypography();
        HideLegacyPlayPanelChildren();
        OpenPlayPanel();

        if (Reset != null) Reset.gameObject.SetActive(true);
        if (DeleteProgress != null) DeleteProgress.gameObject.SetActive(ShowDangerButtons);

        DeleteProgress.onClick.AddListener(RestartProgress);
        Play.onClick.AddListener(PlayGame);
        PlayAgain.onClick.AddListener(ReturnToMainMenuFromGameOver);
        Reset.onClick.AddListener(ResetGame);

        ConfigureGameOverButtonLabel();
    }

    static void EnsurePerkSlotsOverlay()
    {
        Camera camera = Camera.main;
        if (camera == null)
            camera = FindObjectOfType<Camera>();
        if (camera == null) return;

        PerkSlots3DOverlay overlay = camera.GetComponent<PerkSlots3DOverlay>();
        if (overlay == null)
            overlay = camera.gameObject.AddComponent<PerkSlots3DOverlay>();

        overlay.ForceRefresh();
    }

    void ResetGame()
    {
        Player.instance.Lives = 0;
        Field.instance.EliminateBalls();
    }

    void OpenPlayPanel()
    {
        HideLegacyPlayPanelChildren();
        SetGameplayBackgroundActive(false);
        SuppressLegacyPopups(PlayPanel != null ? PlayPanel.transform.parent as RectTransform : null);
        RestoreMenuBackground();
        PlayPanel.SetActive(true);
        GameOverPanel.SetActive(false);
    }

    void HideLegacyPlayPanelChildren()
    {
        if (PlayPanel == null) return;

        foreach (Transform child in PlayPanel.transform)
        {
            if (child == null) continue;

            bool keepVisible = string.Equals(child.name, RuntimePlayPanelRootName, StringComparison.Ordinal);
            if (child.gameObject.activeSelf != keepVisible)
                child.gameObject.SetActive(keepVisible);
        }
    }

    void LateUpdate()
    {
        AlignInventoryRowToPerkSlots();
        UpdatePerkActivationVisuals();
        if (!DisableLegacyPopups) return;
        ForceHidePopupObjects();
    }

    void PlayGame()
    {
        ForceWhiteGameplayBackground();
        ConfigureGameplayHud();
        Player.instance.GameStart();
        CloseAll();
        ConfigureGameplayHud();
        if (ItemGUI.instance != null)
            ItemGUI.instance.LoadItems();
        ForceWhiteGameplayBackground();
    }

    public void PlayFromMenu()
    {
        PlayGame();
    }

    public void SetPlayMenuTicketsLabel(TMP_Text label)
    {
        if (label == null) return;

        PlayMenuTickets = label;
        int cachedTickets = PlayerPrefs.GetInt("ticketCount", 0);
        PlayMenuTickets.text = cachedTickets.ToString();
    }

    public void StopGame(int score)
    {
        GameOverPanel.SetActive(true);
        GameOverLabel.text = $"<b>Game Over!</b>\nScore: {score}";

        Leaderboard.Games.Add(new Game() { Score = score, Time = System.DateTime.Now });
        GenerateGameList();
    }

    void CloseAll()
    {
        PlayPanel.SetActive(false);
        GameOverPanel.SetActive(false);
    }

    void ForceWhiteGameplayBackground()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = GameplayPaperColor;

        if (!EnsureGameplayBackground(mainCamera))
        {
            SetGameplayBackgroundActive(false);
            return;
        }

        ConfigureRealtimeShadows();
        AlignGameplayBackground(mainCamera);
        ApplyGameplayColorHarmony();
        SetGameplayBackgroundActive(true);
    }

    void RestoreMenuBackground()
    {
        if (PlayPanel == null) return;

        Transform runtimePanel = PlayPanel.transform.Find("PSG1_PlayPanel");
        if (runtimePanel == null) return;

        Image panelImage = runtimePanel.GetComponent<Image>();
        if (panelImage == null) return;

        if (panelImage.sprite == null)
        {
            Sprite backgroundSprite = Resources.Load<Sprite>("UI/Sprites/PSG-Ball/bkg");
            if (backgroundSprite == null)
                backgroundSprite = Resources.Load<Sprite>("UI/Sprites/PSG-Ball/pleth_background");
            if (backgroundSprite == null)
                backgroundSprite = Resources.Load<Sprite>("UI/Sprites/PSG-Ball/panel_background");

            if (backgroundSprite != null)
                panelImage.sprite = backgroundSprite;
        }

        panelImage.color = Color.white;
    }

    bool EnsureGameplayBackground(Camera mainCamera)
    {
        if (mainCamera == null) return false;

        GameObject legacyQuad = FindSceneObjectByName("Gameplay Background");
        if (legacyQuad != null)
            Destroy(legacyQuad);

        if (gameplayBackgroundPlane == null)
        {
            gameplayBackgroundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            gameplayBackgroundPlane.name = "Gameplay Background Plane";

            GameObject arena = FindSceneObjectByName("Arena Special");
            if (arena != null)
                gameplayBackgroundPlane.transform.SetParent(arena.transform, true);

            Collider collider = gameplayBackgroundPlane.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            MeshRenderer renderer = gameplayBackgroundPlane.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
        }

        if (gameplayBackgroundMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Diffuse");
            if (shader == null) return false;

            gameplayBackgroundMaterial = new Material(shader);
            gameplayBackgroundMaterial.name = "GameplayBackgroundMaterial";
        }

        ApplyMaterialColor(gameplayBackgroundMaterial, GameplayPaperColor);
        ApplyMaterialSmoothness(gameplayBackgroundMaterial, 0f, 0f);

        MeshRenderer planeRenderer = gameplayBackgroundPlane.GetComponent<MeshRenderer>();
        if (planeRenderer == null) return false;

        planeRenderer.sharedMaterial = gameplayBackgroundMaterial;
        return true;
    }

    void AlignGameplayBackground(Camera mainCamera)
    {
        if (mainCamera == null || gameplayBackgroundPlane == null) return;

        if (!TryGetArenaBounds(out Bounds arenaBounds))
        {
            gameplayBackgroundPlane.transform.position = new Vector3(0f, -1f, 0f);
            gameplayBackgroundPlane.transform.rotation = Quaternion.identity;
            gameplayBackgroundPlane.transform.localScale = new Vector3(3f, 1f, 3f);
            return;
        }

        float targetWidth = Mathf.Max(arenaBounds.size.x + 16f, 26f);
        float targetLength = Mathf.Max(arenaBounds.size.z + 20f, 32f);
        gameplayBackgroundPlane.transform.position = new Vector3(
            arenaBounds.center.x,
            arenaBounds.min.y - 0.08f,
            arenaBounds.center.z);
        gameplayBackgroundPlane.transform.rotation = Quaternion.identity;
        gameplayBackgroundPlane.transform.localScale = new Vector3(targetWidth / 10f, 1f, targetLength / 10f);
    }

    void SetGameplayBackgroundActive(bool isActive)
    {
        if (gameplayBackgroundPlane == null) return;
        gameplayBackgroundPlane.SetActive(isActive);
    }

    void ConfigureRealtimeShadows()
    {
        Light mainLight = SceneObjects.instance != null ? SceneObjects.instance.MainLight : null;
        if (mainLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    mainLight = lights[i];
                    break;
                }
            }
        }

        if (mainLight != null)
        {
            mainLight.type = LightType.Directional;
            mainLight.shadows = LightShadows.Soft;
            mainLight.shadowStrength = 0.62f;
            mainLight.shadowBias = 0.035f;
            mainLight.shadowNormalBias = 0.45f;
            if (mainLight.intensity < 1.05f)
                mainLight.intensity = 1.05f;
        }

        if (QualitySettings.shadowDistance < 90f)
            QualitySettings.shadowDistance = 90f;

        if (!TryGetArenaRoot(out Transform arenaRoot)) return;
        Renderer[] renderers = arenaRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.gameObject.scene.IsValid()) continue;
            if (renderer.gameObject.layer == 5) continue;
            if (renderer.gameObject == gameplayBackgroundPlane) continue;

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    void ApplyGameplayColorHarmony()
    {
        if (flipperMaterial == null)
            flipperMaterial = CreateRuntimeLitMaterial("FlipperGoldRuntime", FlipperGoldColor, 0.9f, 0.12f);

        if (slingshotBaseMaterial == null)
            slingshotBaseMaterial = CreateRuntimeLitMaterial("SlingshotBaseRuntime", SlingshotTerracottaColor, 0.38f, 0.05f);
        if (slingshotLitMaterial == null)
            slingshotLitMaterial = CreateRuntimeLitMaterial("SlingshotLitRuntime", Color.Lerp(SlingshotTerracottaColor, Color.white, 0.2f), 0.56f, 0.06f);

        ApplyFlipperMaterials();
        ApplySlingshotMaterials();
    }

    void ApplyFlipperMaterials()
    {
        if (flipperMaterial == null) return;

        HingeJoint left = null;
        HingeJoint right = null;
        if (Movement.instance != null)
        {
            left = Movement.instance.LeftFlipperJoint;
            right = Movement.instance.RightFlipperJoint;
        }

        ApplyMaterialToJointRenderers(left, flipperMaterial);
        ApplyMaterialToJointRenderers(right, flipperMaterial);

        if (left != null || right != null) return;

        HingeJoint[] joints = FindObjectsOfType<HingeJoint>(true);
        for (int i = 0; i < joints.Length; i++)
        {
            HingeJoint joint = joints[i];
            if (joint == null) continue;

            string n = joint.name.ToLowerInvariant();
            if (!n.Contains("flipper")) continue;
            ApplyMaterialToJointRenderers(joint, flipperMaterial);
        }
    }

    static void ApplyMaterialToJointRenderers(HingeJoint joint, Material material)
    {
        if (joint == null || material == null) return;

        Renderer[] renderers = joint.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            if (renderer.gameObject.layer == 5) continue;

            Material[] mats = renderer.sharedMaterials;
            if (mats == null || mats.Length == 0) continue;
            for (int m = 0; m < mats.Length; m++)
                mats[m] = material;
            renderer.sharedMaterials = mats;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    void ApplySlingshotMaterials()
    {
        if (slingshotBaseMaterial == null || slingshotLitMaterial == null) return;

        Slingshot[] slingshots = FindObjectsOfType<Slingshot>(true);
        for (int i = 0; i < slingshots.Length; i++)
        {
            Slingshot slingshot = slingshots[i];
            if (slingshot == null) continue;
            slingshot.ApplyMaterials(slingshotBaseMaterial, slingshotLitMaterial);
        }
    }

    static Material CreateRuntimeLitMaterial(string materialName, Color color, float smoothness, float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null) return null;

        Material material = new Material(shader);
        material.name = materialName;
        ApplyMaterialColor(material, color);
        ApplyMaterialSmoothness(material, smoothness, metallic);
        return material;
    }

    static void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    static void ApplyMaterialSmoothness(Material material, float smoothness, float metallic)
    {
        if (material == null) return;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic);
    }

    bool TryGetArenaRoot(out Transform root)
    {
        root = null;

        GameObject arena = FindSceneObjectByName("Arena Special");
        if (arena != null)
        {
            root = arena.transform;
            return true;
        }

        if (Field.instance != null)
        {
            root = Field.instance.transform.root;
            return root != null;
        }

        return false;
    }

    bool TryGetArenaBounds(out Bounds bounds)
    {
        bounds = default;
        if (!TryGetArenaRoot(out Transform root) || root == null)
            return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled) continue;
            if (!renderer.gameObject.scene.IsValid()) continue;
            if (renderer.gameObject.layer == 5) continue;
            if (renderer.gameObject == gameplayBackgroundPlane) continue;

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

        if (found) return true;
        Collider trigger = Field.instance != null ? Field.instance.GetComponent<Collider>() : null;
        if (trigger == null) return false;
        bounds = trigger.bounds;
        return true;
    }

    void ConfigureGameplayHud()
    {
        if (PlayPanel == null) return;

        RectTransform canvasRoot = PlayPanel.transform.parent as RectTransform;
        if (canvasRoot == null) return;

        AlignInventoryRowToPerkSlots(canvasRoot);

        if (!SyncTicketCounterFromMenu(canvasRoot))
            ApplyTicketCounterFallback(canvasRoot);

        ApplyGameplayContrast(canvasRoot);
        ApplyRightHudPadding(canvasRoot);
        SuppressLegacyPopups(canvasRoot);

        RectTransform ticketIconRect = canvasRoot.Find("Ticket Sprite") as RectTransform;
        RectTransform ticketsValueRect = canvasRoot.Find("Tickets") as RectTransform;
        if (ticketIconRect != null)
        {
            ticketIconRect.gameObject.SetActive(true);
            ticketIconRect.SetAsLastSibling();
        }
        if (ticketsValueRect != null)
        {
            ticketsValueRect.gameObject.SetActive(true);
            ticketsValueRect.SetAsLastSibling();
        }

        if (Reset != null)
        {
            Reset.gameObject.SetActive(true);
            Reset.transform.SetAsLastSibling();
            TMP_Text resetText = Reset.GetComponentInChildren<TMP_Text>(true);
            if (resetText != null)
                resetText.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        }

        HideLegacyTicketRow(PlayPanel.transform);
        EnsureHudTypography();
        ApplyGameplayTextShadows(canvasRoot);
        UpdatePerkActivationVisuals();
    }

    bool SyncTicketCounterFromMenu(RectTransform canvasRoot)
    {
        if (canvasRoot == null || PlayPanel == null) return false;
        if (!PlayPanel.activeInHierarchy) return false;

        Transform counterRoot = PlayPanel.transform.Find("PSG1_PlayPanel/Header/Ball_Counter");
        if (counterRoot == null) return false;
        if (!counterRoot.gameObject.activeInHierarchy) return false;

        RectTransform sourceIcon = counterRoot.Find("Currency_Ball") as RectTransform;
        RectTransform sourceValue = counterRoot.Find("TicketCount") as RectTransform;
        RectTransform targetIcon = canvasRoot.Find("Ticket Sprite") as RectTransform;
        RectTransform targetValue = canvasRoot.Find("Tickets") as RectTransform;
        if (sourceIcon == null || sourceValue == null || targetIcon == null || targetValue == null) return false;

        CopyRectToCanvasSpace(canvasRoot, sourceIcon, targetIcon);
        CopyRectToCanvasSpace(canvasRoot, sourceValue, targetValue);
        if (targetIcon.sizeDelta.x < 1f || targetIcon.sizeDelta.y < 1f) return false;
        if (targetValue.sizeDelta.x < 1f || targetValue.sizeDelta.y < 1f) return false;

        Image sourceIconImage = sourceIcon.GetComponent<Image>();
        Image targetIconImage = targetIcon.GetComponent<Image>();
        if (sourceIconImage != null && targetIconImage != null)
        {
            Sprite sourceSprite = sourceIconImage.sprite;
            if (sourceSprite == null)
                sourceSprite = Resources.Load<Sprite>("UI/Sprites/PSG-Ball/currency_ball");
            if (sourceSprite != null)
                targetIconImage.sprite = sourceSprite;
            targetIconImage.preserveAspect = sourceIconImage.preserveAspect;
            targetIconImage.color = sourceIconImage.color;
        }

        TMP_Text sourceValueText = sourceValue.GetComponent<TMP_Text>();
        TMP_Text targetValueText = targetValue.GetComponent<TMP_Text>();
        if (sourceValueText != null && targetValueText != null)
        {
            targetValueText.font = sourceValueText.font;
            targetValueText.fontSharedMaterial = sourceValueText.fontSharedMaterial;
            targetValueText.fontSize = sourceValueText.fontSize;
            targetValueText.enableAutoSizing = sourceValueText.enableAutoSizing;
            targetValueText.fontSizeMin = sourceValueText.fontSizeMin;
            targetValueText.fontSizeMax = sourceValueText.fontSizeMax;
            targetValueText.fontStyle = sourceValueText.fontStyle;
            targetValueText.alignment = sourceValueText.alignment;
            targetValueText.color = sourceValueText.color;
            targetValueText.characterSpacing = sourceValueText.characterSpacing;
        }

        return true;
    }

    void ApplyGameplayContrast(RectTransform canvasRoot)
    {
        EnsureHudTypography();
        ApplyTextColor(Score, new Color(0.1f, 0.1f, 0.1f, 1f));
        ApplyTextColor(Multiplier, new Color(1f, 0.55f, 0.16f, 1f));
        ApplyTextColor(Lives, new Color(0.2f, 0.2f, 0.2f, 1f));

        if (canvasRoot != null)
        {
            TMP_Text ballsLabel = (canvasRoot.Find("Balls") as RectTransform)?.GetComponent<TMP_Text>();
            if (ballsLabel != null)
            {
                ballsLabel.text = "LIVES";
                ballsLabel.fontSize = 18f;
                ballsLabel.fontStyle = FontStyles.Bold;
                ballsLabel.characterSpacing = 1.6f;
                ApplyTextColor(ballsLabel, new Color(0.2f, 0.2f, 0.2f, 1f));
                if (hudFontAsset != null)
                    ApplyHudFont(ballsLabel, hudFontAsset);
            }
        }
    }

    void ApplyRightHudPadding(RectTransform canvasRoot)
    {
        if (Score != null)
        {
            SetTopRightRect(Score.rectTransform, 52f, 26f, 340f, 84f);
            Score.alignment = TextAlignmentOptions.TopRight;
        }
        if (Multiplier != null)
        {
            SetTopRightRect(Multiplier.rectTransform, 52f, 106f, 340f, 40f);
            Multiplier.alignment = TextAlignmentOptions.TopRight;
        }
        if (Lives != null)
        {
            SetTopRightRect(Lives.rectTransform, 52f, 144f, 86f, 38f);
            Lives.alignment = TextAlignmentOptions.MidlineRight;
        }

        if (canvasRoot != null)
        {
            TMP_Text ballsLabel = (canvasRoot.Find("Balls") as RectTransform)?.GetComponent<TMP_Text>();
            if (ballsLabel != null && ballsLabel != Lives)
            {
                SetTopRightRect(ballsLabel.rectTransform, 142f, 148f, 140f, 30f);
                ballsLabel.alignment = TextAlignmentOptions.MidlineRight;
            }
        }
    }

    void SuppressLegacyPopups(RectTransform canvasRoot)
    {
        if (!DisableLegacyPopups) return;

        if (cachedInfoPanel == null) cachedInfoPanel = FindSceneObjectByName("InfoPanel");
        if (cachedTutorialPanel == null) cachedTutorialPanel = FindSceneObjectByName("Tutorial");
        if (cachedAchievementToast == null) cachedAchievementToast = FindSceneObjectByName("Achievement get");
        if (cachedAchievementGui == null) cachedAchievementGui = FindSceneObjectByName("Achievement GUI");

        ForceHidePopupObjects();

        if (canvasRoot == null || popupTriggersDisabled) return;
        DisablePopupEventTriggers(canvasRoot);
        popupTriggersDisabled = true;
    }

    void ForceHidePopupObjects()
    {
        if (cachedInfoPanel != null) cachedInfoPanel.SetActive(false);
        if (cachedTutorialPanel != null) cachedTutorialPanel.SetActive(false);
        if (cachedAchievementToast != null) cachedAchievementToast.SetActive(false);
        if (cachedAchievementGui != null) cachedAchievementGui.SetActive(false);
    }

    static void DisablePopupEventTriggers(RectTransform canvasRoot)
    {
        if (canvasRoot == null) return;

        EventTrigger[] triggers = canvasRoot.GetComponentsInChildren<EventTrigger>(true);
        foreach (EventTrigger trigger in triggers)
        {
            if (trigger == null || trigger.triggers == null) continue;

            bool shouldDisable = false;
            for (int e = 0; e < trigger.triggers.Count && !shouldDisable; e++)
            {
                EventTrigger.Entry entry = trigger.triggers[e];
                if (entry == null || entry.callback == null) continue;

                int listeners = entry.callback.GetPersistentEventCount();
                for (int i = 0; i < listeners; i++)
                {
                    UnityEngine.Object target = entry.callback.GetPersistentTarget(i);
                    string method = entry.callback.GetPersistentMethodName(i);
                    if (target == null) continue;

                    string typeName = target.GetType().Name;
                    if ((typeName == "ItemGUI" || typeName == "AchievementGUI") &&
                        (method == "GetItemInfo" || method == "GetSlotInfo" || method == "GetAchievementInfo"))
                    {
                        shouldDisable = true;
                        break;
                    }

                    if (target is GameObject targetGameObject &&
                        targetGameObject.name == "InfoPanel" &&
                        method == "SetActive")
                    {
                        shouldDisable = true;
                        break;
                    }
                }
            }

            if (shouldDisable)
                trigger.enabled = false;
        }
    }

    static void CopyRectToCanvasSpace(RectTransform canvasRoot, RectTransform source, RectTransform target)
    {
        if (canvasRoot == null || source == null || target == null) return;

        Vector3[] worldCorners = new Vector3[4];
        source.GetWorldCorners(worldCorners);

        Vector2 bottomLeft;
        Vector2 topRight;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot,
            RectTransformUtility.WorldToScreenPoint(null, worldCorners[0]),
            null,
            out bottomLeft);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot,
            RectTransformUtility.WorldToScreenPoint(null, worldCorners[2]),
            null,
            out topRight);

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.zero;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = (bottomLeft + topRight) * 0.5f;
        target.sizeDelta = new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y));
    }

    static void ApplyTicketCounterFallback(RectTransform canvasRoot)
    {
        if (canvasRoot == null) return;

        RectTransform ticketIconRect = canvasRoot.Find("Ticket Sprite") as RectTransform;
        if (ticketIconRect != null)
        {
            // Match PSG1 menu header icon: left/top = 44px, size = 72px at 1240x1080 reference.
            SetTopLeftRect(ticketIconRect, 44f, 44f, 72f, 72f);

            Image ticketIcon = ticketIconRect.GetComponent<Image>();
            if (ticketIcon != null)
            {
                Sprite currencySprite = Resources.Load<Sprite>("UI/Sprites/PSG-Ball/currency_ball");
                if (currencySprite != null)
                    ticketIcon.sprite = currencySprite;
                ticketIcon.preserveAspect = true;
                ticketIcon.color = Color.white;
            }
        }

        RectTransform ticketsValueRect = canvasRoot.Find("Tickets") as RectTransform;
        if (ticketsValueRect != null)
        {
            // Match PSG1 menu counter text block:
            // ball counter area starts at x=44,y=24 with size 360x112,
            // and text starts with 92px left inset => x=136.
            SetTopLeftRect(ticketsValueRect, 136f, 24f, 268f, 112f);

            TMP_Text ticketsValue = ticketsValueRect.GetComponent<TMP_Text>();
            if (ticketsValue != null)
            {
                ticketsValue.color = new Color(0.12f, 0.12f, 0.12f, 1f);
                ticketsValue.fontStyle = FontStyles.Normal;
                ticketsValue.alignment = TextAlignmentOptions.Left;
            }
        }
    }

    static void HideLegacyTicketRow(Transform menuRoot)
    {
        if (menuRoot == null) return;

        DisableIfFound(menuRoot, "Ticket Sprite");
        DisableIfFound(menuRoot, "Tickets");
        DisableIfFound(menuRoot, "Ticket count label");
    }

    static void DisableIfFound(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
            child.gameObject.SetActive(false);
    }

    static void SetAnchorsY(RectTransform rect, float minY, float maxY)
    {
        if (rect == null) return;

        Vector2 min = rect.anchorMin;
        Vector2 max = rect.anchorMax;
        min.y = minY;
        max.y = maxY;
        rect.anchorMin = min;
        rect.anchorMax = max;
    }

    static void SetAnchorsXY(RectTransform rect, float minX, float maxX, float minY, float maxY)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
    }

    static void SetTopLeftRect(RectTransform rect, float left, float top, float width, float height)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void SetTopRightRect(RectTransform rect, float right, float top, float width, float height)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-right, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    static void ApplyTextColor(TMP_Text text, Color color)
    {
        if (text == null) return;
        text.color = color;
    }

    static void SetRightMargin(RectTransform rect, float margin)
    {
        if (rect == null) return;

        float width = rect.rect.width;
        if (width <= 0.001f)
            width = rect.sizeDelta.x;

        Vector2 pos = rect.anchoredPosition;
        pos.x = -margin - (1f - rect.pivot.x) * width;
        rect.anchoredPosition = pos;
    }

    static GameObject FindSceneObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return null;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject sceneObject = allObjects[i];
            if (sceneObject == null) continue;
            if (sceneObject.name != objectName) continue;
            if (sceneObject.hideFlags != HideFlags.None) continue;
            if (!sceneObject.scene.IsValid()) continue;
            return sceneObject;
        }

        return null;
    }

    void AlignInventoryRowToPerkSlots()
    {
        if (PlayPanel == null) return;
        RectTransform canvasRoot = PlayPanel.transform.parent as RectTransform;
        if (canvasRoot == null) return;
        AlignInventoryRowToPerkSlots(canvasRoot);
    }

    void AlignInventoryRowToPerkSlots(RectTransform canvasRoot)
    {
        if (canvasRoot == null) return;

        RectTransform gameplayInventoryRoot = FindGameplayInventoryRoot(canvasRoot);
        RectTransform menuInventoryRoot = canvasRoot.Find("Inventory") as RectTransform;
        RectTransform targetInventoryRoot = gameplayInventoryRoot != null ? gameplayInventoryRoot : menuInventoryRoot;

        bool gameplayVisible = !PlayPanel.activeInHierarchy && (GameOverPanel == null || !GameOverPanel.activeInHierarchy);
        bool hasSeparateGameplayInventory = gameplayInventoryRoot != null && gameplayInventoryRoot != menuInventoryRoot;
        if (menuInventoryRoot != null && hasSeparateGameplayInventory)
        {
            bool showMenuInventory = !gameplayVisible;
            if (menuInventoryRoot.gameObject.activeSelf != showMenuInventory)
                menuInventoryRoot.gameObject.SetActive(showMenuInventory);
        }

        if (targetInventoryRoot == null) return;

        if (!gameplayVisible)
        {
            if (targetInventoryRoot.gameObject.activeSelf)
                targetInventoryRoot.gameObject.SetActive(false);
            return;
        }

        if (!targetInventoryRoot.gameObject.activeSelf)
            targetInventoryRoot.gameObject.SetActive(true);

        // Match the gameplay inventory icon size to the menu inventory row (150x150 per slot).
        SetAnchorsXY(targetInventoryRoot, 0.5f, 0.5f, 0.9f, 0.9f);
        targetInventoryRoot.pivot = new Vector2(0.5f, 0.5f);
        targetInventoryRoot.anchoredPosition = Vector2.zero;
        targetInventoryRoot.sizeDelta = new Vector2(560f, 132f);
        ApplyGameplayInventorySlotSizes(targetInventoryRoot);
        targetInventoryRoot.SetAsLastSibling();
    }

    static RectTransform FindGameplayInventoryRoot(RectTransform canvasRoot)
    {
        if (canvasRoot == null) return null;

        RectTransform direct = canvasRoot.Find("Play Panel Outline/Inventory") as RectTransform;
        if (IsGameplayInventoryRoot(direct))
            return direct;

        RectTransform directInventory = canvasRoot.Find("Inventory") as RectTransform;
        if (IsGameplayInventoryRoot(directInventory))
            return directInventory;

        RectTransform[] localRects = canvasRoot.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < localRects.Length; i++)
        {
            RectTransform rect = localRects[i];
            if (!IsGameplayInventoryRoot(rect)) continue;
            return rect;
        }

        RectTransform[] rects = Resources.FindObjectsOfTypeAll<RectTransform>();
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (!IsGameplayInventoryRoot(rect)) continue;
            if (!rect.gameObject.scene.IsValid()) continue;
            return rect;
        }

        return null;
    }

    static bool IsGameplayInventoryRoot(RectTransform rect)
    {
        if (rect == null || rect.name != "Inventory") return false;
        if (FindSlotRect(rect, "First") == null) return false;
        return HasAncestorNameContaining(rect.transform, "Play Panel");
    }

    static bool HasAncestorNameContaining(Transform node, string token)
    {
        if (node == null || string.IsNullOrEmpty(token)) return false;
        for (Transform current = node; current != null; current = current.parent)
        {
            if (current.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    static void ApplyGameplayInventorySlotSizes(RectTransform inventoryRoot)
    {
        if (inventoryRoot == null) return;

        const float centerX = 6f;
        const float spacing = 184f;
        ApplyGameplaySlotLayout(FindSlotRect(inventoryRoot, "Main", "First"), centerX - spacing);
        ApplyGameplaySlotLayout(FindSlotRect(inventoryRoot, "Second"), centerX);
        ApplyGameplaySlotLayout(FindSlotRect(inventoryRoot, "Third"), centerX + spacing);
    }

    static RectTransform FindSlotRect(RectTransform parent, params string[] names)
    {
        if (parent == null || names == null) return null;

        for (int i = 0; i < names.Length; i++)
        {
            Transform t = parent.Find(names[i]);
            if (t is RectTransform rect)
                return rect;
        }

        return null;
    }

    static void ApplyGameplaySlotLayout(RectTransform slot, float posX)
    {
        if (slot == null) return;

        slot.anchorMin = new Vector2(0.5f, 0.5f);
        slot.anchorMax = new Vector2(0.5f, 0.5f);
        slot.pivot = new Vector2(0.5f, 0.5f);
        slot.anchoredPosition = new Vector2(posX, 20f);
        slot.sizeDelta = new Vector2(150f, 150f);

        if (slot.childCount <= 0) return;
        RectTransform icon = slot.GetChild(0) as RectTransform;
        if (icon == null) return;

        icon.anchorMin = Vector2.zero;
        icon.anchorMax = Vector2.one;
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = Vector2.zero;
        icon.sizeDelta = Vector2.zero;
        icon.offsetMin = new Vector2(1f, 1f);
        icon.offsetMax = new Vector2(-1f, -1f);
    }

    void EnsureHudTypography()
    {
        if (Score == null || Multiplier == null || Lives == null) return;

        if (hudFontAsset == null)
            hudFontAsset = EnsureHudFontAsset();

        if (hudFontAsset != null)
        {
            ApplyHudFont(Score, hudFontAsset);
            ApplyHudFont(Multiplier, hudFontAsset);
            ApplyHudFont(Lives, hudFontAsset);
        }

        Score.fontSize = 70f;
        Score.fontStyle = FontStyles.Bold;
        Score.characterSpacing = 3.2f;
        Score.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        Score.alignment = TextAlignmentOptions.TopRight;

        Multiplier.fontSize = 35f;
        Multiplier.fontStyle = FontStyles.Bold;
        Multiplier.characterSpacing = 2.4f;
        Multiplier.color = new Color(1f, 0.55f, 0.16f, 1f);
        Multiplier.alignment = TextAlignmentOptions.TopRight;

        Lives.fontSize = 40f;
        Lives.fontStyle = FontStyles.Bold;
        Lives.characterSpacing = 1.8f;
        Lives.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        Lives.alignment = TextAlignmentOptions.MidlineRight;
    }

    void ApplyGameplayTextShadows(RectTransform canvasRoot)
    {
        ApplyTextShadow(Score);
        ApplyTextShadow(Multiplier);
        ApplyTextShadow(Lives);
        ApplyTextShadow(Tickets);

        if (canvasRoot != null)
        {
            TMP_Text ballsLabel = (canvasRoot.Find("Balls") as RectTransform)?.GetComponent<TMP_Text>();
            ApplyTextShadow(ballsLabel);
        }

        ApplyTextShadow(perkActivationHintText);
    }

    static void ApplyTextShadow(TMP_Text text)
    {
        if (text == null) return;

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        shadow.effectDistance = new Vector2(1.6f, -1.6f);
        shadow.useGraphicAlpha = true;
    }

    void UpdatePerkActivationVisuals()
    {
        if (PlayPanel == null) return;
        RectTransform canvasRoot = PlayPanel.transform.parent as RectTransform;
        if (canvasRoot == null) return;

        RectTransform gameplayInventoryRoot = FindGameplayInventoryRoot(canvasRoot);
        RectTransform menuInventoryRoot = canvasRoot.Find("Inventory") as RectTransform;
        RectTransform inventoryRoot = gameplayInventoryRoot != null ? gameplayInventoryRoot : menuInventoryRoot;
        if (inventoryRoot == null)
        {
            SetPerkActivationHintVisible(false);
            return;
        }

        bool gameplayVisible = !PlayPanel.activeInHierarchy && (GameOverPanel == null || !GameOverPanel.activeInHierarchy);
        if (!gameplayVisible)
        {
            SetPerkActivationHintVisible(false);
            ApplyPerkSlotPulse(inventoryRoot, -1, 0f);
            return;
        }

        EnsurePerkActivationHint(inventoryRoot);
        SetPerkActivationHintVisible(true);

        int activePerkIndex = GetActivePerkQueueIndex();
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.8f);
        UpdatePerkActivationHintText(activePerkIndex, pulse);
        ApplyPerkSlotPulse(inventoryRoot, activePerkIndex, pulse);
    }

    void EnsurePerkActivationHint(RectTransform inventoryRoot)
    {
        if (inventoryRoot == null) return;

        if (perkActivationHintText == null || perkActivationHintText.transform.parent != inventoryRoot)
        {
            Transform existing = inventoryRoot.Find("PerkActivateHint");
            if (existing is RectTransform existingRect)
                perkActivationHintText = existingRect.GetComponent<TMP_Text>();
            else
            {
                GameObject hintGo = new GameObject("PerkActivateHint", typeof(RectTransform));
                hintGo.transform.SetParent(inventoryRoot, false);
                perkActivationHintText = hintGo.AddComponent<TextMeshProUGUI>();
            }
        }

        if (perkActivationHintText == null) return;

        RectTransform hintRect = perkActivationHintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -8f);
        hintRect.sizeDelta = new Vector2(620f, 36f);

        if (hudFontAsset != null)
            ApplyHudFont(perkActivationHintText, hudFontAsset);
        perkActivationHintText.fontSize = 23f;
        perkActivationHintText.fontStyle = FontStyles.Bold;
        perkActivationHintText.alignment = TextAlignmentOptions.Center;
        perkActivationHintText.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        perkActivationHintText.raycastTarget = false;
        ApplyTextShadow(perkActivationHintText);
    }

    void SetPerkActivationHintVisible(bool isVisible)
    {
        if (perkActivationHintText == null) return;
        GameObject hintObject = perkActivationHintText.gameObject;
        if (hintObject.activeSelf != isVisible)
            hintObject.SetActive(isVisible);
    }

    void UpdatePerkActivationHintText(int activePerkIndex, float pulse)
    {
        if (perkActivationHintText == null) return;

        if (activePerkIndex >= 0)
        {
            Color activePressColor = Color.Lerp(
                new Color(0.34f, 0.34f, 0.34f, 1f),
                new Color(1f, 0.56f, 0.16f, 1f),
                0.4f + 0.6f * pulse);
            string pressHex = ColorUtility.ToHtmlStringRGB(activePressColor);
            perkActivationHintText.text = $"<color=#{pressHex}>PRESS [X]</color> TO ACTIVATE";
        }
        else
        {
            perkActivationHintText.text = "<color=#555555>PRESS [X]</color> TO ACTIVATE";
        }
    }

    void ApplyPerkSlotPulse(RectTransform inventoryRoot, int activePerkIndex, float pulse)
    {
        if (inventoryRoot == null) return;

        RectTransform first = FindSlotRect(inventoryRoot, "Main", "First");
        RectTransform second = FindSlotRect(inventoryRoot, "Second");
        RectTransform third = FindSlotRect(inventoryRoot, "Third");
        RectTransform[] slots = { first, second, third };

        for (int i = 0; i < slots.Length; i++)
        {
            RectTransform slot = slots[i];
            if (slot == null) continue;

            bool hasPerk = HasPerkInSlot(i);
            bool isActive = hasPerk && i == activePerkIndex;
            Image frame = slot.GetComponent<Image>();
            if (frame != null)
            {
                if (isActive)
                {
                    frame.color = Color.Lerp(
                        new Color(0.86f, 0.5f, 0.18f, 0.22f),
                        new Color(1f, 0.64f, 0.2f, 0.62f),
                        pulse);
                }
                else if (hasPerk)
                {
                    frame.color = new Color(0.2f, 0.2f, 0.2f, 0.24f);
                }
                else
                {
                    frame.color = new Color(0.2f, 0.2f, 0.2f, 0.08f);
                }
            }

            Image icon = slot.childCount > 0 ? slot.GetChild(0).GetComponent<Image>() : null;
            if (icon != null && icon.enabled)
            {
                if (isActive)
                    icon.color = Color.Lerp(new Color(0.82f, 0.82f, 0.82f, 1f), Color.white, pulse);
                else if (hasPerk)
                    icon.color = new Color(0.93f, 0.93f, 0.93f, 1f);
            }
        }
    }

    static bool HasPerkInSlot(int index)
    {
        if (Inventory.Slots == null) return false;
        if (index < 0 || index >= Inventory.Slots.Length) return false;

        Item item = Inventory.Slots[index];
        return item != null && !(item is NoItem);
    }

    static int GetActivePerkQueueIndex()
    {
        if (Inventory.Slots == null) return -1;

        for (int i = 0; i < Inventory.Slots.Length; i++)
        {
            if (HasPerkInSlot(i))
                return i;
        }

        return -1;
    }

    static void ApplyHudFont(TMP_Text text, TMP_FontAsset fontAsset)
    {
        if (text == null || fontAsset == null) return;
        text.font = fontAsset;
        text.fontSharedMaterial = fontAsset.material;
    }

    static TMP_FontAsset EnsureHudFontAsset()
    {
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>(HudRetroTmpResource);
        if (fontAsset != null) return fontAsset;

        Font retroRawFont = Resources.Load<Font>(HudRetroFontResource);
        if (retroRawFont != null)
        {
            TMP_FontAsset retroCreated = TryCreateRuntimeFontAsset(retroRawFont);
            if (retroCreated != null)
                return retroCreated;
        }

        fontAsset = TryCreateHudFontFromOs();
        if (fontAsset != null) return fontAsset;

        fontAsset = Resources.Load<TMP_FontAsset>(HudFallbackTmpResource);
        if (fontAsset != null) return fontAsset;

        fontAsset = Resources.Load<TMP_FontAsset>(HudNunitoTmpResource);
        if (fontAsset != null) return fontAsset;

        Font rawFont = Resources.Load<Font>(HudNunitoFontResource);
        if (rawFont == null) return null;

        return TryCreateRuntimeFontAsset(rawFont);
    }

    static TMP_FontAsset TryCreateHudFontFromOs()
    {
        string[] preferredFonts = new string[]
        {
            "Press Start 2P",
            "ArcadeClassic",
            "Joystix",
            "Andale Mono",
            "SF Mono",
            "Menlo"
        };

        for (int i = 0; i < preferredFonts.Length; i++)
        {
            Font osFont = Font.CreateDynamicFontFromOSFont(preferredFonts[i], 64);
            if (osFont == null) continue;

            TMP_FontAsset tmpFont = TryCreateRuntimeFontAsset(osFont);
            if (tmpFont != null)
                return tmpFont;
        }

        return null;
    }

    static TMP_FontAsset TryCreateRuntimeFontAsset(Font sourceFont)
    {
        if (sourceFont == null) return null;

        try
        {
            MethodInfo[] methods = typeof(TMP_FontAsset).GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (!string.Equals(method.Name, "CreateFontAsset", StringComparison.Ordinal))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 0 || parameters[0].ParameterType != typeof(Font))
                    continue;

                object[] args = new object[parameters.Length];
                args[0] = sourceFont;
                for (int p = 1; p < parameters.Length; p++)
                {
                    ParameterInfo parameter = parameters[p];
                    args[p] = parameter.HasDefaultValue
                        ? parameter.DefaultValue
                        : (parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null);
                }

                object result = method.Invoke(null, args);
                if (result is TMP_FontAsset createdFontAsset)
                    return createdFontAsset;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"PlayerGUI: Could not create runtime TMP font asset: {exception.Message}");
        }

        return null;
    }

    void RestartProgress()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene("Game");
    }

    void GenerateGameList()
    {
        Leaderboard.Order();

        string text = "";

        for (int i = 0; i < Leaderboard.Games.Count; i++)
            text += $"<i>#</i>{(i+1).ToString("00")} | Score: <b>{Leaderboard.Games[i].Score}</b> | <color=#aaa>{Leaderboard.Games[i].Time}</color>\n";

        HighScore.text = text;
    }

    void Update()
    {
        HandleGameOverInput();

#if UNITY_EDITOR
        if (FPS == null) return;

        refreshRate += Time.deltaTime;
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if(refreshRate > 0.5f)
        {
            FPS.text = $"FPS: {((int) (1f / deltaTime))}";
            refreshRate = 0;
        }
#endif
    }

    void HandleGameOverInput()
    {
        if (GameOverPanel == null || !GameOverPanel.activeInHierarchy) return;
        if (PlayAgain == null || !PlayAgain.isActiveAndEnabled) return;
        if (gameOverReturnInProgress) return;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject != PlayAgain.gameObject)
            eventSystem.SetSelectedGameObject(PlayAgain.gameObject);

        if (InputAdapter.MenuSubmitPressedThisFrame() ||
            InputAdapter.MenuDailyDropPressedThisFrame() ||
            InputAdapter.MenuBackPressedThisFrame())
        {
            ReturnToMainMenuFromGameOver();
        }
    }

    void ConfigureGameOverButtonLabel()
    {
        if (PlayAgain == null) return;

        TMP_Text label = PlayAgain.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;

        label.text = "GO TO MAIN MENU";
    }

    void ReturnToMainMenuFromGameOver()
    {
        if (gameOverReturnInProgress) return;
        StartCoroutine(ReturnToMainMenuNextFrame());
    }

    IEnumerator ReturnToMainMenuNextFrame()
    {
        gameOverReturnInProgress = true;
        yield return null;
        OpenPlayPanel();
        gameOverReturnInProgress = false;
    }
}
