using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PSG1PlayPanelController : MonoBehaviour
{
    const string RootName = "PSG1_PlayPanel";
    const string DailyClaimKey = "psg1_daily_drop_claim_utc";
    const int DailyRewardAmount = 10;
    const int ShopCrateCount = 2;
    const int InventorySlotCount = 3;
    const float SlotSelectedScale = 1.1f;
    const float SlotScaleLerpSpeed = 12f;
    const float PurchaseExtractDuration = 0.14f;
    const float PurchaseTransitDuration = 0.28f;
    const float SlotFlashDuration = 0.16f;
    const float WalletButtonMinWidth = 260f;
    const float MainBackgroundLightenAlpha = 0.12f;
    const float ShopBackgroundLightenAlpha = 0.08f;
    const int ShopMainMenuFocusIndex = ShopCrateCount;

    readonly RectTransform[] shopCrateRects = new RectTransform[ShopCrateCount];
    readonly Button[] shopCrateButtons = new Button[ShopCrateCount];
    readonly CanvasGroup[] shopCrateCanvasGroups = new CanvasGroup[ShopCrateCount];
    readonly Image[] shopCrateFrameImages = new Image[ShopCrateCount];
    readonly Image[] shopCrateBuyButtonImages = new Image[ShopCrateCount];
    readonly Image[] shopCrateIconImages = new Image[ShopCrateCount];
    readonly TMP_Text[] shopCratePriceLabels = new TMP_Text[ShopCrateCount];
    readonly Image[] inventorySlotFrameImages = new Image[InventorySlotCount];
    readonly Image[] inventorySlotGlowImages = new Image[InventorySlotCount];
    readonly Image[] inventorySlotIconImages = new Image[InventorySlotCount];
    readonly Coroutine[] inventorySlotPulseCoroutines = new Coroutine[InventorySlotCount];
    readonly bool[] inventorySlotAnimating = new bool[InventorySlotCount];
    readonly int[] cratePrices = { 3, 7 };
    readonly Crates[] crateTypes = { Crates.Rusty, Crates.Brass };
    readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    PlayerGUI playerGui;
    WalletConnectUI walletUi;
    RectTransform root;
    RectTransform shopPanel;
    GameObject actionZoneRoot;
    TMP_Text ballCounterText;
    Button walletButton;
    Image walletButtonBackgroundImage;
    TMP_Text walletText;
    Button dailyDropButton;
    Image shopButtonBackgroundImage;
    TMP_Text dailyDropText;
    Button shopCloseButton;
    TMP_Text shopCloseText;
    Image shopCloseButtonBackgroundImage;
    GameObject shopCloseButtonRoot;
    RectTransform shopCloseButtonRect;
    Button dailyClaimButton;
    Image dailyClaimButtonBackgroundImage;
    TMP_Text dailyClaimText;
    TMP_Text queueFullText;
    GameObject queueFullBadgeRoot;
    Button launchButton;
    Image launchButtonBackgroundImage;
    GameObject launchButtonRoot;
    GameObject utilityButtonsRoot;
    Sprite currencyBallSprite;
    Sprite panelBackgroundSprite;
    Sprite plethBackgroundSprite;
    Sprite slotFrameEmptySprite;
    Sprite slotFrameSelectedSprite;
    Sprite launchSprite;
    Sprite launchButtonCleanSprite;
    Sprite dailyDropBadgeSprite;
    Sprite crateRustySprite;
    Sprite crateBrassSprite;
    Sprite crateGoldenSprite;
    Sprite orangeButtonSprite;
    Sprite blueButtonSprite;
    Sprite whiteButtonSprite;
    TMP_FontAsset toyCoreButtonFontAsset;
    Material toyCoreButtonMaterialPreset;
    bool initialized;
    bool toyCoreIconsApplied;
    int selectedCrate;
    int selectedShopFocus;
    readonly Color disabledIconColor = new Color(1f, 1f, 1f, 0.35f);
    readonly Color queueFullColor = new Color(0.98f, 0.8f, 0.24f, 1f);
    readonly Color queueFullFrameColor = new Color(0.23f, 0.2f, 0.12f, 0.55f);
    readonly Color slotFlashColor = new Color(1f, 0.93f, 0.62f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<PSG1PlayPanelController>() != null) return;

        GameObject rootObject = new GameObject(nameof(PSG1PlayPanelController));
        rootObject.AddComponent<PSG1PlayPanelController>();
    }

    void Start()
    {
        TryInitialize();
    }

    void Update()
    {
        if (!TryInitialize()) return;

        if (!toyCoreIconsApplied)
            toyCoreIconsApplied = TryApplyToyCoreItemIcons();

        if (playerGui == null || !playerGui.IsMainMenuVisible) return;

        HandleMenuInput();
        AnimateSelectedCrate();
        RefreshShopRow();
        RefreshInventoryRow();
        RefreshWalletAndDailyState();
        RefreshLaunchButtonState();
    }

    bool TryInitialize()
    {
        if (initialized)
        {
            bool hasMainReferences =
                root != null &&
                ballCounterText != null &&
                walletButton != null &&
                dailyDropButton != null &&
                shopCloseButton != null &&
                dailyClaimButton != null;
            if (hasMainReferences)
                return true;

            initialized = false;
        }

        playerGui = PlayerGUI.instance;
        if (playerGui == null) return false;
        if (playerGui.PlayPanelRoot == null) return false;

        walletUi = WalletConnectUI.Instance;
        LoadSprites();
        EnsureToyCoreButtonTextStyleAssets();
        ConfigureCanvas(playerGui.PlayPanelRoot);
        BuildMenu(playerGui.PlayPanelRoot.transform as RectTransform);
        HideLegacyMenu(playerGui.PlayPanelRoot);

        if (ballCounterText != null)
            playerGui.SetPlayMenuTicketsLabel(ballCounterText);

        SanitizeInventorySlots();
        selectedCrate = 0;
        selectedShopFocus = 0;
        SetShopPanelVisible(false);
        if (launchButtonRoot != null) launchButtonRoot.SetActive(true);
        if (utilityButtonsRoot != null) utilityButtonsRoot.SetActive(true);
        if (actionZoneRoot != null) actionZoneRoot.SetActive(true);
        RefreshShopRow();
        RefreshInventoryRow();
        RefreshWalletAndDailyState();
        RefreshLaunchButtonState();
        initialized = true;
        return true;
    }

    void ConfigureCanvas(GameObject playPanel)
    {
        if (playPanel == null) return;

        CanvasScaler scaler = playPanel.GetComponentInParent<CanvasScaler>();
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1240f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    void BuildMenu(RectTransform playPanel)
    {
        if (playPanel == null) return;

        Transform existingRoot = playPanel.Find(RootName);
        if (existingRoot != null)
            Destroy(existingRoot.gameObject);

        root = CreateRect(RootName, playPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        Image rootBackground = root.gameObject.AddComponent<Image>();
        rootBackground.sprite = plethBackgroundSprite;
        rootBackground.type = Image.Type.Simple;
        rootBackground.preserveAspect = false;
        rootBackground.color = Color.white;

        RectTransform rootLightOverlay = CreateRect("Root_Background_Lighten", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image rootLightOverlayImage = rootLightOverlay.gameObject.AddComponent<Image>();
        rootLightOverlayImage.type = Image.Type.Simple;
        rootLightOverlayImage.preserveAspect = false;
        rootLightOverlayImage.color = new Color(1f, 1f, 1f, MainBackgroundLightenAlpha);

        BuildMainDisplay(root);
        BuildInventoryRow(root);
        BuildDailyRewardZone(root);
        BuildFooter(root);
        BuildHeader(root);
    }

    void BuildHeader(RectTransform parent)
    {
        RectTransform header = CreateRect("Header", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-40f, 120f));

        RectTransform ballCounter = CreateRect("Ball_Counter", header, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(360f, -8f));
        Image ballIcon = CreateImage("Currency_Ball", ballCounter, currencyBallSprite);
        RectTransform ballIconRect = ballIcon.rectTransform;
        ballIconRect.anchorMin = new Vector2(0f, 0.5f);
        ballIconRect.anchorMax = new Vector2(0f, 0.5f);
        ballIconRect.pivot = new Vector2(0f, 0.5f);
        ballIconRect.sizeDelta = new Vector2(72f, 72f);
        ballIconRect.anchoredPosition = Vector2.zero;

        ballCounterText = CreateLabel("TicketCount", ballCounter, "0", 48f, TextAlignmentOptions.Left);
        RectTransform counterTextRect = ballCounterText.rectTransform;
        counterTextRect.anchorMin = Vector2.zero;
        counterTextRect.anchorMax = Vector2.one;
        counterTextRect.offsetMin = new Vector2(92f, 0f);
        counterTextRect.offsetMax = Vector2.zero;

        RectTransform walletRect = CreateRect("Wallet_Button", header, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(WalletButtonMinWidth, 94f));
        LayoutElement walletLayout = walletRect.gameObject.AddComponent<LayoutElement>();
        walletLayout.minWidth = WalletButtonMinWidth;
        walletLayout.preferredWidth = WalletButtonMinWidth;
        walletLayout.minHeight = 94f;
        walletLayout.preferredHeight = 94f;
        walletButton = walletRect.gameObject.AddComponent<Button>();
        Image walletBg = walletRect.gameObject.AddComponent<Image>();
        walletButtonBackgroundImage = walletBg;
        if (blueButtonSprite != null)
        {
            walletBg.sprite = blueButtonSprite;
            walletBg.type = Image.Type.Sliced;
            walletBg.preserveAspect = false;
            walletBg.color = Color.white;
        }
        else
        {
            walletBg.color = new Color(0.93f, 0.94f, 0.96f, 1f);
        }
        Outline walletOutline = walletRect.gameObject.AddComponent<Outline>();
        walletOutline.effectColor = blueButtonSprite != null ? new Color(0f, 0f, 0f, 0f) : new Color(0.77f, 0.8f, 0.86f, 1f);
        walletOutline.effectDistance = new Vector2(2f, -2f);

        walletText = CreateLabel("Wallet_Label", walletRect, "CONNECT", 28f, TextAlignmentOptions.Center);
        ApplyToyCoreButtonTextStyle(walletText, autoSize: true, autoSizeMin: 10f, autoSizeMax: 20f);
        walletText.rectTransform.anchorMin = Vector2.zero;
        walletText.rectTransform.anchorMax = Vector2.one;
        walletText.rectTransform.offsetMin = new Vector2(14f, 8f);
        walletText.rectTransform.offsetMax = new Vector2(-14f, -8f);

        walletButton.transition = Selectable.Transition.None;
        walletButton.onClick.AddListener(OnWalletPressed);
    }

    void BuildMainDisplay(RectTransform parent)
    {
        shopPanel = CreateRect("Shop_Overlay_Panel", parent, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.76f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image panelImage = shopPanel.gameObject.AddComponent<Image>();
        panelImage.sprite = plethBackgroundSprite != null ? plethBackgroundSprite : panelBackgroundSprite;
        panelImage.type = Image.Type.Simple;
        panelImage.preserveAspect = false;
        panelImage.color = new Color(1f, 1f, 1f, 0.98f);

        RectTransform panelLightOverlay = CreateRect("Shop_Background_Lighten", shopPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image panelLightOverlayImage = panelLightOverlay.gameObject.AddComponent<Image>();
        panelLightOverlayImage.type = Image.Type.Simple;
        panelLightOverlayImage.preserveAspect = false;
        panelLightOverlayImage.color = new Color(1f, 1f, 1f, ShopBackgroundLightenAlpha);
        Outline panelOutline = shopPanel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0f, 0f, 0f, 0f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform content = CreateRect("Main_Content", shopPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, -24f));
        VerticalLayoutGroup vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = 10f;
        vertical.padding = new RectOffset(16, 16, 16, 12);
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = false;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        RectTransform spacer = CreateRect("Queue_State_Row", content, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 18f));
        LayoutElement spacerLayout = spacer.gameObject.AddComponent<LayoutElement>();
        spacerLayout.preferredHeight = 18f;

        RectTransform shopRow = CreateRect("ShopRow", content, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 366f));
        LayoutElement shopRowLayout = shopRow.gameObject.AddComponent<LayoutElement>();
        shopRowLayout.preferredHeight = 366f;

        HorizontalLayoutGroup shopLayout = shopRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        shopLayout.spacing = 30f;
        shopLayout.padding = new RectOffset(4, 4, 0, 0);
        shopLayout.childAlignment = TextAnchor.MiddleCenter;
        shopLayout.childControlWidth = false;
        shopLayout.childControlHeight = false;
        shopLayout.childForceExpandWidth = false;
        shopLayout.childForceExpandHeight = false;

        for (int i = 0; i < ShopCrateCount; i++)
        {
            int capturedIndex = i;
            RectTransform crate = CreateRect($"Shop_Crate_{i + 1}", shopRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 352f));
            LayoutElement crateLayout = crate.gameObject.AddComponent<LayoutElement>();
            crateLayout.preferredWidth = 430f;
            crateLayout.preferredHeight = 352f;

            Image frame = crate.gameObject.AddComponent<Image>();
            frame.color = new Color(1f, 1f, 1f, 0f);
            Outline frameOutline = crate.gameObject.AddComponent<Outline>();
            frameOutline.effectColor = new Color(0f, 0f, 0f, 0f);
            frameOutline.effectDistance = new Vector2(2f, -2f);

            CanvasGroup crateCanvasGroup = crate.gameObject.AddComponent<CanvasGroup>();

            VerticalLayoutGroup cardLayout = crate.gameObject.AddComponent<VerticalLayoutGroup>();
            cardLayout.spacing = 10f;
            cardLayout.padding = new RectOffset(14, 14, 14, 14);
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = false;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            RectTransform iconZone = CreateRect("Card_Icon_Zone", crate, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 142f));
            LayoutElement iconZoneLayout = iconZone.gameObject.AddComponent<LayoutElement>();
            iconZoneLayout.preferredHeight = 142f;

            Image icon = CreateImage("Crate_Icon", iconZone, GetCrateSpriteForIndex(i));
            icon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.rectTransform.sizeDelta = new Vector2(150f, 150f);

            RectTransform infoPlate = CreateRect("Card_Info", crate, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(402f, 120f));
            LayoutElement infoLayout = infoPlate.gameObject.AddComponent<LayoutElement>();
            infoLayout.preferredHeight = 120f;
            Image infoBg = infoPlate.gameObject.AddComponent<Image>();
            infoBg.color = new Color(1f, 1f, 1f, 0.88f);

            VerticalLayoutGroup infoGroup = infoPlate.gameObject.AddComponent<VerticalLayoutGroup>();
            infoGroup.spacing = 4f;
            infoGroup.padding = new RectOffset(12, 12, 8, 8);
            infoGroup.childAlignment = TextAnchor.UpperCenter;
            infoGroup.childControlWidth = true;
            infoGroup.childControlHeight = false;
            infoGroup.childForceExpandWidth = true;
            infoGroup.childForceExpandHeight = false;

            TMP_Text titleLabel = CreateLabel("Card_Title", infoPlate, GetCrateTitle(i), 25f, TextAlignmentOptions.Center);
            LayoutElement titleLayout = titleLabel.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 30f;
            titleLabel.enableWordWrapping = false;

            RectTransform perkRows = CreateRect("Card_Perk_Rows", infoPlate, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            LayoutElement perkRowsLayout = perkRows.gameObject.AddComponent<LayoutElement>();
            perkRowsLayout.preferredHeight = 74f;
            VerticalLayoutGroup perkLayout = perkRows.gameObject.AddComponent<VerticalLayoutGroup>();
            perkLayout.spacing = 2f;
            perkLayout.padding = new RectOffset(0, 0, 0, 0);
            perkLayout.childAlignment = TextAnchor.UpperLeft;
            perkLayout.childControlWidth = true;
            perkLayout.childControlHeight = false;
            perkLayout.childForceExpandWidth = true;
            perkLayout.childForceExpandHeight = false;
            BuildCratePerkRows(perkRows, i);

            RectTransform buyRect = CreateRect("Buy_Button", crate, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(320f, 62f));
            LayoutElement buyLayout = buyRect.gameObject.AddComponent<LayoutElement>();
            buyLayout.preferredHeight = 62f;
            Image buyBg = buyRect.gameObject.AddComponent<Image>();
            buyBg.sprite = launchButtonCleanSprite != null ? launchButtonCleanSprite : launchSprite;
            buyBg.type = Image.Type.Simple;
            buyBg.preserveAspect = false;
            buyBg.color = Color.white;
            Outline buyOutline = buyRect.gameObject.AddComponent<Outline>();
            buyOutline.effectColor = new Color(0.78f, 0.5f, 0.1f, 0.9f);
            buyOutline.effectDistance = new Vector2(1f, -2f);
            Button buyButton = buyRect.gameObject.AddComponent<Button>();
            buyButton.onClick.AddListener(() => OnShopCratePressed(capturedIndex));

            TMP_Text buyLabel = CreateLabel("Buy_Label", buyRect, $"BUY - {GetCratePrice(i)} Balls", 24f, TextAlignmentOptions.Center);
            ApplyToyCoreButtonTextStyle(buyLabel);
            buyLabel.color = Color.white;
            buyLabel.rectTransform.offsetMin = Vector2.zero;
            buyLabel.rectTransform.offsetMax = Vector2.zero;

            shopCrateRects[i] = crate;
            shopCrateButtons[i] = buyButton;
            shopCrateCanvasGroups[i] = crateCanvasGroup;
            shopCrateFrameImages[i] = frame;
            shopCrateBuyButtonImages[i] = buyBg;
            shopCrateIconImages[i] = icon;
            shopCratePriceLabels[i] = buyLabel;
        }

        shopPanel.gameObject.SetActive(false);

        RectTransform shopCloseRect = CreateRect("Shop_MainMenu_Button", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(760f, 102f));
        shopCloseButtonRect = shopCloseRect;
        shopCloseButtonRoot = shopCloseRect.gameObject;
        Image closeBg = shopCloseRect.gameObject.AddComponent<Image>();
        shopCloseButtonBackgroundImage = closeBg;
        closeBg.sprite = whiteButtonSprite != null ? whiteButtonSprite : (launchButtonCleanSprite != null ? launchButtonCleanSprite : launchSprite);
        closeBg.type = whiteButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        closeBg.preserveAspect = false;
        closeBg.color = whiteButtonSprite != null ? Color.white : new Color(0.62f, 0.72f, 0.9f, 1f);
        Outline closeOutline = shopCloseRect.gameObject.AddComponent<Outline>();
        closeOutline.effectColor = whiteButtonSprite != null ? new Color(0f, 0f, 0f, 0f) : new Color(0.35f, 0.47f, 0.73f, 0.92f);
        closeOutline.effectDistance = new Vector2(2f, -3f);
        shopCloseButton = shopCloseRect.gameObject.AddComponent<Button>();
        shopCloseButton.transition = Selectable.Transition.None;
        shopCloseButton.onClick.AddListener(OnShopClosePressed);

        if (dailyDropBadgeSprite != null)
        {
            Image badge = CreateImage("Shop_MainMenu_Badge", shopCloseRect, dailyDropBadgeSprite);
            badge.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            badge.rectTransform.pivot = new Vector2(0f, 0.5f);
            badge.rectTransform.sizeDelta = new Vector2(72f, 72f);
            badge.rectTransform.anchoredPosition = new Vector2(24f, 0f);
        }

        shopCloseText = CreateLabel("Shop_MainMenu_Label", shopCloseRect, "MAIN MENU", 34f, TextAlignmentOptions.Center);
        ApplyToyCoreButtonTextStyle(shopCloseText);
        shopCloseText.color = Color.white;
        shopCloseText.rectTransform.anchorMin = Vector2.zero;
        shopCloseText.rectTransform.anchorMax = Vector2.one;
        shopCloseText.rectTransform.offsetMin = dailyDropBadgeSprite != null ? new Vector2(96f, 0f) : Vector2.zero;
        shopCloseText.rectTransform.offsetMax = Vector2.zero;
        shopCloseButtonRoot.SetActive(false);
    }

    void BuildInventoryRow(RectTransform parent)
    {
        RectTransform inventoryTray = CreateRect("Inventory_Dock", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(760f, 156f));
        Image trayBackground = inventoryTray.gameObject.AddComponent<Image>();
        trayBackground.sprite = panelBackgroundSprite;
        trayBackground.type = Image.Type.Simple;
        trayBackground.preserveAspect = false;
        trayBackground.color = new Color(0.92f, 0.96f, 1f, 0.78f);
        Outline trayOutline = inventoryTray.gameObject.AddComponent<Outline>();
        trayOutline.effectColor = new Color(0.67f, 0.76f, 0.9f, 0.86f);
        trayOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform queueBadgeRect = CreateRect("Queue_Full_Badge", inventoryTray, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(410f, 46f));
        queueFullBadgeRoot = queueBadgeRect.gameObject;
        Image queueBadgeBg = queueBadgeRect.gameObject.AddComponent<Image>();
        queueBadgeBg.color = queueFullFrameColor;
        Outline queueBadgeOutline = queueBadgeRect.gameObject.AddComponent<Outline>();
        queueBadgeOutline.effectColor = new Color(0.93f, 0.78f, 0.28f, 0.95f);
        queueBadgeOutline.effectDistance = new Vector2(2f, -2f);
        queueFullText = CreateLabel("Queue_Full_Label", queueBadgeRect, "QUEUE FULL", 27f, TextAlignmentOptions.Center);
        queueFullText.color = queueFullColor;
        queueFullBadgeRoot.SetActive(false);

        RectTransform inventoryRow = CreateRect("InventoryRow", inventoryTray, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-56f, -36f));
        HorizontalLayoutGroup inventoryLayout = inventoryRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        inventoryLayout.spacing = 18f;
        inventoryLayout.padding = new RectOffset(24, 24, 0, 0);
        inventoryLayout.childAlignment = TextAnchor.MiddleCenter;
        inventoryLayout.childControlWidth = false;
        inventoryLayout.childControlHeight = false;
        inventoryLayout.childForceExpandWidth = false;
        inventoryLayout.childForceExpandHeight = false;

        for (int i = 0; i < InventorySlotCount; i++)
        {
            RectTransform inventorySlot = CreateRect($"Slot_Circle_Empty_{i + 1}", inventoryRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(128f, 128f));
            LayoutElement slotLayout = inventorySlot.gameObject.AddComponent<LayoutElement>();
            slotLayout.preferredWidth = 128f;
            slotLayout.preferredHeight = 128f;

            Image frame = inventorySlot.gameObject.AddComponent<Image>();
            frame.sprite = slotFrameEmptySprite;
            frame.type = Image.Type.Simple;
            frame.preserveAspect = true;
            if (frame.sprite == null)
                frame.color = new Color(0.75f, 0.78f, 0.84f, 1f);
            else
                frame.color = new Color(0.94f, 0.97f, 1f, 0.95f);

            Image glow = CreateImage("Inventory_Glow", inventorySlot, slotFrameSelectedSprite != null ? slotFrameSelectedSprite : slotFrameEmptySprite);
            glow.rectTransform.offsetMin = new Vector2(10f, 10f);
            glow.rectTransform.offsetMax = new Vector2(-10f, -10f);
            glow.color = new Color(0.42f, 0.84f, 1f, 0f);
            glow.enabled = false;

            Image icon = CreateImage("Inventory_Icon", inventorySlot, null);
            icon.rectTransform.anchorMin = Vector2.zero;
            icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = new Vector2(18f, 18f);
            icon.rectTransform.offsetMax = new Vector2(-18f, -18f);
            Shadow iconShadow = icon.gameObject.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            iconShadow.effectDistance = new Vector2(2f, -2f);
            icon.enabled = false;

            inventorySlotFrameImages[i] = frame;
            inventorySlotGlowImages[i] = glow;
            inventorySlotIconImages[i] = icon;
        }
    }

    void BuildDailyRewardZone(RectTransform parent)
    {
        const float buttonWidth = 760f;
        const float buttonHeight = 102f;
        const float launchToShopGap = 24f;
        const float shopToClaimGap = 12f;

        RectTransform zone = CreateRect("Action_Zone", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(buttonWidth, 342f));
        actionZoneRoot = zone.gameObject;

        RectTransform launchRect = CreateRect("LAUNCH_BUTTON", zone, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(buttonWidth, buttonHeight));
        launchButtonRoot = launchRect.gameObject;

        Image launchImage = launchRect.gameObject.AddComponent<Image>();
        launchButtonBackgroundImage = launchImage;
        launchImage.sprite = orangeButtonSprite != null ? orangeButtonSprite : launchSprite;
        launchImage.type = orangeButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        launchImage.preserveAspect = false;
        if (orangeButtonSprite == null && launchSprite == null)
            launchImage.color = new Color(0.97f, 0.66f, 0.15f, 1f);
        else
            launchImage.color = Color.white;

        launchButton = launchRect.gameObject.AddComponent<Button>();
        launchButton.transition = Selectable.Transition.None;
        launchButton.onClick.AddListener(OnLaunchPressed);

        if (orangeButtonSprite != null || launchSprite == null)
        {
            TMP_Text launchLabel = CreateLabel("Launch_Label", launchRect, "LAUNCH", 52f, TextAlignmentOptions.Center);
            ApplyToyCoreButtonTextStyle(launchLabel);
            launchLabel.color = Color.white;
            launchLabel.rectTransform.anchorMin = Vector2.zero;
            launchLabel.rectTransform.anchorMax = Vector2.one;
            launchLabel.rectTransform.offsetMin = Vector2.zero;
            launchLabel.rectTransform.offsetMax = Vector2.zero;
        }

        RectTransform shopButtonRect = CreateRect(
            "Shop_Button",
            zone,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -(buttonHeight + launchToShopGap)),
            new Vector2(buttonWidth, buttonHeight));
        dailyDropButton = shopButtonRect.gameObject.AddComponent<Button>();
        Image shopBg = shopButtonRect.gameObject.AddComponent<Image>();
        shopButtonBackgroundImage = shopBg;
        if (whiteButtonSprite != null)
        {
            shopBg.sprite = whiteButtonSprite;
            shopBg.type = Image.Type.Sliced;
            shopBg.preserveAspect = false;
        }
        else if (launchSprite != null)
        {
            shopBg.sprite = launchSprite;
            shopBg.type = Image.Type.Simple;
            shopBg.preserveAspect = false;
        }
        shopBg.color = whiteButtonSprite != null ? Color.white : new Color(0.62f, 0.72f, 0.9f, 1f);
        Outline shopOutline = shopButtonRect.gameObject.AddComponent<Outline>();
        shopOutline.effectColor = whiteButtonSprite != null ? new Color(0f, 0f, 0f, 0f) : new Color(0.35f, 0.47f, 0.73f, 0.92f);
        shopOutline.effectDistance = new Vector2(2f, -3f);
        dailyDropButton.transition = Selectable.Transition.None;

        if (dailyDropBadgeSprite != null)
        {
            Image badge = CreateImage("Shop_Badge", shopButtonRect, dailyDropBadgeSprite);
            badge.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            badge.rectTransform.pivot = new Vector2(0f, 0.5f);
            badge.rectTransform.sizeDelta = new Vector2(72f, 72f);
            badge.rectTransform.anchoredPosition = new Vector2(24f, 0f);
        }

        dailyDropText = CreateLabel("Shop_Label", shopButtonRect, "SHOP", 34f, TextAlignmentOptions.Center);
        ApplyToyCoreButtonTextStyle(dailyDropText);
        dailyDropText.color = Color.white;
        dailyDropText.rectTransform.anchorMin = Vector2.zero;
        dailyDropText.rectTransform.anchorMax = Vector2.one;
        dailyDropText.rectTransform.offsetMin = dailyDropBadgeSprite != null ? new Vector2(96f, 0f) : Vector2.zero;
        dailyDropText.rectTransform.offsetMax = Vector2.zero;
        dailyDropButton.onClick.AddListener(OnShopTriggerPressed);

        RectTransform dailyClaimRect = CreateRect(
            "Daily_Claim_Button",
            zone,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -(buttonHeight + launchToShopGap + buttonHeight + shopToClaimGap)),
            new Vector2(buttonWidth, buttonHeight));
        dailyClaimButton = dailyClaimRect.gameObject.AddComponent<Button>();
        Image claimBg = dailyClaimRect.gameObject.AddComponent<Image>();
        dailyClaimButtonBackgroundImage = claimBg;
        if (whiteButtonSprite != null)
        {
            claimBg.sprite = whiteButtonSprite;
            claimBg.type = Image.Type.Sliced;
            claimBg.preserveAspect = false;
            claimBg.color = Color.white;
        }
        else
        {
            claimBg.color = Color.white;
        }
        Outline claimOutline = dailyClaimRect.gameObject.AddComponent<Outline>();
        claimOutline.effectColor = whiteButtonSprite != null ? new Color(0f, 0f, 0f, 0f) : new Color(0.77f, 0.8f, 0.86f, 1f);
        claimOutline.effectDistance = new Vector2(2f, -2f);
        dailyClaimButton.transition = Selectable.Transition.None;

        if (dailyDropBadgeSprite != null)
        {
            Image badge = CreateImage("Daily_Claim_Badge", dailyClaimRect, dailyDropBadgeSprite);
            badge.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            badge.rectTransform.pivot = new Vector2(0f, 0.5f);
            badge.rectTransform.sizeDelta = new Vector2(72f, 72f);
            badge.rectTransform.anchoredPosition = new Vector2(24f, 0f);
        }

        dailyClaimText = CreateLabel("Daily_Claim_Label", dailyClaimRect, "DAILY CLAIM", 30f, TextAlignmentOptions.Center);
        ApplyToyCoreButtonTextStyle(dailyClaimText);
        dailyClaimText.rectTransform.anchorMin = Vector2.zero;
        dailyClaimText.rectTransform.anchorMax = Vector2.one;
        dailyClaimText.rectTransform.offsetMin = dailyDropBadgeSprite != null ? new Vector2(96f, 0f) : Vector2.zero;
        dailyClaimText.rectTransform.offsetMax = Vector2.zero;
        dailyClaimButton.onClick.AddListener(OnDailyClaimPressed);
    }

    void BuildFooter(RectTransform parent)
    {
        RectTransform utilityRow = CreateRect("Utility_Buttons", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(-120f, 46f));
        utilityButtonsRoot = utilityRow.gameObject;
        HorizontalLayoutGroup utilityLayout = utilityRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        utilityLayout.spacing = 12f;
        utilityLayout.padding = new RectOffset(0, 0, 0, 0);
        utilityLayout.childAlignment = TextAnchor.MiddleCenter;
        utilityLayout.childControlWidth = false;
        utilityLayout.childControlHeight = false;
        utilityLayout.childForceExpandWidth = false;
        utilityLayout.childForceExpandHeight = false;

        CreateUtilityButton(utilityRow, "Reset_Perks_Button", "Reset Perks", OnResetPerksPressed, 260f);
        CreateUtilityButton(utilityRow, "Add_Balls_Button", "+100 Balls", OnAddBallsPressed, 240f);
    }

    void HideLegacyMenu(GameObject playPanelRoot)
    {
        if (playPanelRoot == null) return;

        foreach (Transform child in playPanelRoot.transform)
        {
            if (child == null) continue;
            if (child.name == RootName) continue;
            child.gameObject.SetActive(false);
        }

        SetObjectActiveByName("Achievements", false);
        SetObjectActiveByName("Achievements Panel", false);
        SetObjectActiveByName("Controls_Panel", false);
        SetObjectActiveByName("Reset_Progress_Button", false);
        SetObjectActiveByName("Destroy Balls", false);

        if (playerGui != null)
        {
            if (playerGui.InfoName != null) playerGui.InfoName.gameObject.SetActive(false);
            if (playerGui.InfoDescription != null) playerGui.InfoDescription.gameObject.SetActive(false);
        }
    }

    void HandleMenuInput()
    {
        if (InputAdapter.MenuBackPressedThisFrame() && IsShopPanelVisible())
        {
            SetShopPanelVisible(false);
            return;
        }

        if (InputAdapter.MenuDailyDropPressedThisFrame() && !IsShopPanelVisible())
        {
            OnShopTriggerPressed();
            return;
        }

        if (!IsShopPanelVisible())
        {
            if (InputAdapter.MenuSubmitPressedThisFrame())
                OnLaunchPressed();
            return;
        }

        if (InputAdapter.MenuDownPressedThisFrame())
            selectedShopFocus = ShopMainMenuFocusIndex;

        if (InputAdapter.MenuUpPressedThisFrame())
            selectedShopFocus = selectedCrate;

        if (selectedShopFocus < ShopCrateCount)
        {
            if (InputAdapter.MenuLeftPressedThisFrame())
                selectedCrate = (selectedCrate + ShopCrateCount - 1) % ShopCrateCount;

            if (InputAdapter.MenuRightPressedThisFrame())
                selectedCrate = (selectedCrate + 1) % ShopCrateCount;

            selectedShopFocus = selectedCrate;
        }

        if (!InputAdapter.MenuSubmitPressedThisFrame()) return;

        if (selectedShopFocus == ShopMainMenuFocusIndex)
            OnShopClosePressed();
        else
            ActivateSelectedElement();
    }

    void AnimateSelectedCrate()
    {
        if (!IsShopPanelVisible())
        {
            for (int i = 0; i < shopCrateRects.Length; i++)
                if (shopCrateRects[i] != null)
                    shopCrateRects[i].localScale = Vector3.one;
            if (shopCloseButtonRect != null)
                shopCloseButtonRect.localScale = Vector3.one;
            return;
        }

        for (int i = 0; i < shopCrateRects.Length; i++)
        {
            if (shopCrateRects[i] == null) continue;
            float target = 1f;
            Vector3 targetScale = new Vector3(target, target, 1f);
            shopCrateRects[i].localScale = Vector3.Lerp(shopCrateRects[i].localScale, targetScale, Time.unscaledDeltaTime * SlotScaleLerpSpeed);

            if (shopCrateFrameImages[i] != null)
            {
                shopCrateFrameImages[i].color = new Color(1f, 1f, 1f, 0f);
            }
        }

        if (shopCloseButtonRect != null)
        {
            float target = selectedShopFocus == ShopMainMenuFocusIndex ? SlotSelectedScale : 1f;
            Vector3 targetScale = new Vector3(target, target, 1f);
            shopCloseButtonRect.localScale = Vector3.Lerp(shopCloseButtonRect.localScale, targetScale, Time.unscaledDeltaTime * SlotScaleLerpSpeed);
        }
    }

    void OnShopCratePressed(int crateIndex)
    {
        selectedCrate = Mathf.Clamp(crateIndex, 0, ShopCrateCount - 1);
        selectedShopFocus = selectedCrate;
        ActivateSelectedElement();
    }

    void ActivateSelectedElement()
    {
        PurchaseCrate(selectedCrate);
    }

    public void PurchaseCrate(int crateType)
    {
        if (crateType < 0 || crateType >= ShopCrateCount) return;

        int price = GetCratePrice(crateType);
        int currentBalls = GetCurrentBalls();
        if (currentBalls < price)
        {
            PlayPurchaseFailSound();
            return;
        }

        int inventorySlotIndex = GetFirstEmptyInventorySlot();
        if (inventorySlotIndex < 0)
        {
            PlayPurchaseFailSound();
            return;
        }

        Item droppedItem = GetDropFromCrate(crateType);
        if (IsEmptyItem(droppedItem)) return;

        SetCurrentBalls(currentBalls - price);

        Inventory.Slots[inventorySlotIndex] = droppedItem;
        Inventory.SetMemory();
        PlayerPrefs.Save();

        if (ItemGUI.instance != null)
        {
            ItemGUI.instance.LoadItems();
            ItemGUI.instance.PurchaseSound();
        }

        PrepareInventorySlotForTransit(inventorySlotIndex);
        RefreshShopRow();

        if (inventorySlotPulseCoroutines[inventorySlotIndex] != null)
            StopCoroutine(inventorySlotPulseCoroutines[inventorySlotIndex]);
        inventorySlotPulseCoroutines[inventorySlotIndex] = StartCoroutine(
            PlayPurchaseTransit(crateType, inventorySlotIndex, droppedItem.Icon));
    }

    void OnLaunchPressed()
    {
        if (playerGui == null) return;
        playerGui.PlayFromMenu();
    }

    void OnResetPerksPressed()
    {
        Item emptyItem = Items.instance != null ? Items.NoItem : null;
        int slotCount = Mathf.Min(InventorySlotCount, Inventory.Slots.Length);
        for (int i = 0; i < slotCount; i++)
        {
            Inventory.Slots[i] = emptyItem;
            inventorySlotAnimating[i] = false;
            if (inventorySlotPulseCoroutines[i] != null)
            {
                StopCoroutine(inventorySlotPulseCoroutines[i]);
                inventorySlotPulseCoroutines[i] = null;
            }
        }

        if (root != null)
        {
            List<Transform> transitNodes = new List<Transform>();
            foreach (Transform child in root)
            {
                if (child != null && child.name.StartsWith("Drop_Transit_Icon", StringComparison.Ordinal))
                    transitNodes.Add(child);
            }

            for (int i = 0; i < transitNodes.Count; i++)
                Destroy(transitNodes[i].gameObject);
        }

        Inventory.SetMemory();
        PlayerPrefs.Save();

        if (ItemGUI.instance != null)
            ItemGUI.instance.LoadItems();

        RefreshShopRow();
        RefreshInventoryRow();
    }

    void OnAddBallsPressed()
    {
        SetCurrentBalls(GetCurrentBalls() + 100);
        PlayerPrefs.Save();
        RefreshShopRow();
    }

    void OnWalletPressed()
    {
        walletUi = WalletConnectUI.Instance;
        if (walletUi == null) return;
        walletUi.ToggleConnectionFromExternalUI();
    }

    void OnDailyClaimPressed()
    {
        walletUi = WalletConnectUI.Instance;
        if (walletUi == null || !walletUi.IsConnected)
        {
            OnWalletPressed();
            return;
        }

        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (PlayerPrefs.GetString(DailyClaimKey, string.Empty) == today)
        {
            RefreshWalletAndDailyState();
            return;
        }

        SetCurrentBalls(GetCurrentBalls() + DailyRewardAmount);
        PlayerPrefs.SetString(DailyClaimKey, today);
        PlayerPrefs.Save();
        RefreshWalletAndDailyState();
    }

    void OnShopTriggerPressed()
    {
        SetShopPanelVisible(true);
    }

    void OnShopClosePressed()
    {
        SetShopPanelVisible(false);
    }

    void RefreshShopRow()
    {
        int currentBalls = GetCurrentBalls();
        bool hasInventorySpace = GetFirstEmptyInventorySlot() >= 0;
        if (queueFullText != null)
            queueFullText.text = "QUEUE FULL";
        if (queueFullBadgeRoot != null)
            queueFullBadgeRoot.SetActive(!hasInventorySpace && IsShopPanelVisible());

        for (int i = 0; i < ShopCrateCount; i++)
        {
            Image icon = shopCrateIconImages[i];
            TMP_Text label = shopCratePriceLabels[i];
            if (icon == null || label == null) continue;

            int price = GetCratePrice(i);
            bool canPurchase = hasInventorySpace && currentBalls >= price;
            if (shopCrateButtons[i] != null)
                shopCrateButtons[i].interactable = canPurchase;
            if (shopCrateCanvasGroups[i] != null)
                shopCrateCanvasGroups[i].alpha = canPurchase ? 1f : 0.7f;
            if (shopCrateBuyButtonImages[i] != null)
                shopCrateBuyButtonImages[i].color = canPurchase ? Color.white : new Color(0.56f, 0.56f, 0.56f, 1f);

            icon.enabled = true;
            icon.sprite = GetCrateSpriteForIndex(i);
            icon.color = canPurchase ? Color.white : disabledIconColor;

            label.text = $"BUY - {GetCratePrice(i)} Balls";
            label.color = canPurchase ? Color.white : new Color(0.82f, 0.82f, 0.82f, 1f);
        }
    }

    void RefreshInventoryRow()
    {
        for (int i = 0; i < InventorySlotCount; i++)
        {
            if (inventorySlotFrameImages[i] != null)
            {
                if (!inventorySlotAnimating[i])
                    inventorySlotFrameImages[i].color = new Color(0.94f, 0.97f, 1f, 0.95f);
            }

            Image icon = inventorySlotIconImages[i];
            if (icon == null) continue;
            if (inventorySlotAnimating[i]) continue;

            Item item = i < Inventory.Slots.Length ? Inventory.Slots[i] : null;
            if (IsEmptyItem(item))
            {
                icon.enabled = false;
                icon.sprite = null;
                icon.color = Color.white;
                if (inventorySlotGlowImages[i] != null)
                {
                    inventorySlotGlowImages[i].enabled = false;
                    inventorySlotGlowImages[i].color = new Color(0.42f, 0.84f, 1f, 0f);
                }
                continue;
            }

            icon.enabled = item.Icon != null;
            icon.sprite = item.Icon;
            icon.color = Color.white;
            if (inventorySlotGlowImages[i] != null)
            {
                inventorySlotGlowImages[i].enabled = true;
                inventorySlotGlowImages[i].color = new Color(0.42f, 0.84f, 1f, 0.38f);
            }
        }
    }

    void RefreshWalletAndDailyState()
    {
        walletUi = WalletConnectUI.Instance;
        bool connected = walletUi != null && walletUi.IsConnected;

        if (walletButtonBackgroundImage != null)
        {
            Sprite walletButtonSprite = connected ? whiteButtonSprite : blueButtonSprite;
            if (walletButtonSprite != null)
            {
                walletButtonBackgroundImage.sprite = walletButtonSprite;
                walletButtonBackgroundImage.type = Image.Type.Sliced;
                walletButtonBackgroundImage.preserveAspect = false;
                walletButtonBackgroundImage.color = Color.white;
            }
        }
        if (walletText != null)
            walletText.text = connected ? "CONNECTED" : "CONNECT";

        if (dailyDropButton != null && dailyDropText != null)
        {
            dailyDropButton.interactable = !IsShopPanelVisible();
            dailyDropText.text = "SHOP";
        }

        if (shopCloseButton != null && shopCloseText != null)
        {
            shopCloseButton.interactable = IsShopPanelVisible();
            shopCloseText.text = "MAIN MENU";
        }

        if (dailyClaimButton == null || dailyClaimText == null) return;

        if (!connected)
        {
            dailyClaimButton.interactable = true;
            dailyClaimText.text = "CONNECT FOR DAILY";
            return;
        }

        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        bool alreadyClaimed = PlayerPrefs.GetString(DailyClaimKey, string.Empty) == today;
        dailyClaimButton.interactable = !alreadyClaimed;
        dailyClaimText.text = alreadyClaimed ? "CLAIMED TODAY" : "GET DAILY +10";
    }

    void RefreshLaunchButtonState()
    {
        if (launchButton == null) return;

        launchButton.interactable = true;

        Image launchImage = launchButton.GetComponent<Image>();
        if (launchImage != null)
            launchImage.color = Color.white;
    }

    bool IsShopPanelVisible() => shopPanel != null && shopPanel.gameObject.activeSelf;

    void SetShopPanelVisible(bool visible)
    {
        if (shopPanel == null) return;
        if (shopPanel.gameObject.activeSelf == visible) return;
        shopPanel.gameObject.SetActive(visible);
        if (visible)
            selectedShopFocus = Mathf.Clamp(selectedCrate, 0, ShopCrateCount - 1);

        if (launchButtonRoot != null)
            launchButtonRoot.SetActive(!visible);
        if (utilityButtonsRoot != null)
            utilityButtonsRoot.SetActive(!visible);
        if (actionZoneRoot != null)
            actionZoneRoot.SetActive(!visible);
        if (shopCloseButtonRoot != null)
            shopCloseButtonRoot.SetActive(visible);

        if (visible)
        {
            RefreshShopRow();
            RefreshInventoryRow();
        }

        RefreshWalletAndDailyState();
    }

    void EnsureToyCoreButtonTextStyleAssets()
    {
        if (toyCoreButtonMaterialPreset != null && toyCoreButtonFontAsset != null)
            return;

        if (toyCoreButtonFontAsset == null)
        {
            toyCoreButtonFontAsset = Resources.Load<TMP_FontAsset>("UI/Fonts/Nunito-ExtraBold SDF");
            if (toyCoreButtonFontAsset == null)
            {
                Font nunitoFont = Resources.Load<Font>("UI/Fonts/Nunito-ExtraBold");
                if (nunitoFont != null)
                    toyCoreButtonFontAsset = TryCreateRuntimeFontAsset(nunitoFont);
            }

            if (toyCoreButtonFontAsset == null)
                toyCoreButtonFontAsset = TMP_Settings.defaultFontAsset;
        }

        if (toyCoreButtonFontAsset == null || toyCoreButtonFontAsset.material == null)
            return;

        toyCoreButtonMaterialPreset = new Material(toyCoreButtonFontAsset.material);
        toyCoreButtonMaterialPreset.name = "ToyCore_Button3D_Runtime";
        ConfigureToyCoreButtonMaterial(toyCoreButtonMaterialPreset);
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
                if (result is TMP_FontAsset fontAsset)
                    return fontAsset;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"PSG1PlayPanelController: Could not create runtime TMP font asset: {exception.Message}");
        }

        return null;
    }

    static void ConfigureToyCoreButtonMaterial(Material material)
    {
        if (material == null) return;

        material.EnableKeyword("BEVEL_ON");
        material.EnableKeyword("UNDERLAY_ON");

        if (material.HasProperty("_FaceColor"))
            material.SetColor("_FaceColor", Color.white);

        if (material.HasProperty("_Bevel"))
            material.SetFloat("_Bevel", 0.2f);
        if (material.HasProperty("_BevelWidth"))
            material.SetFloat("_BevelWidth", 0.22f);
        if (material.HasProperty("_BevelRoundness"))
            material.SetFloat("_BevelRoundness", 0.3f);
        if (material.HasProperty("_BevelOffset"))
            material.SetFloat("_BevelOffset", 0.05f);
        if (material.HasProperty("_BevelClamp"))
            material.SetFloat("_BevelClamp", 0f);

        if (material.HasProperty("_LightAngle"))
            material.SetFloat("_LightAngle", Mathf.PI * 0.5f);
        if (material.HasProperty("_SpecularColor"))
            material.SetColor("_SpecularColor", new Color(1f, 1f, 1f, 0.9f));
        if (material.HasProperty("_SpecularPower"))
            material.SetFloat("_SpecularPower", 1.7f);
        if (material.HasProperty("_Diffuse"))
            material.SetFloat("_Diffuse", 0.55f);
        if (material.HasProperty("_Ambient"))
            material.SetFloat("_Ambient", 0.45f);

        if (material.HasProperty("_UnderlayColor"))
            material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.34f));
        if (material.HasProperty("_UnderlayOffsetX"))
            material.SetFloat("_UnderlayOffsetX", 1f);
        if (material.HasProperty("_UnderlayOffsetY"))
            material.SetFloat("_UnderlayOffsetY", -1f);
        if (material.HasProperty("_UnderlayDilate"))
            material.SetFloat("_UnderlayDilate", 0.1f);
        if (material.HasProperty("_UnderlaySoftness"))
            material.SetFloat("_UnderlaySoftness", 0.5f);
    }

    void ApplyToyCoreButtonTextStyle(TMP_Text label, bool autoSize = false, float autoSizeMin = 14f, float autoSizeMax = 40f)
    {
        if (label == null) return;

        EnsureToyCoreButtonTextStyleAssets();

        if (toyCoreButtonFontAsset != null)
            label.font = toyCoreButtonFontAsset;
        if (toyCoreButtonMaterialPreset != null)
            label.fontSharedMaterial = toyCoreButtonMaterialPreset;

        label.fontStyle = FontStyles.Bold;
        label.enableWordWrapping = false;
        label.color = Color.white;
        label.enableVertexGradient = true;
        label.colorGradient = new VertexGradient(
            new Color32(255, 255, 255, 255),
            new Color32(255, 255, 255, 255),
            new Color32(229, 233, 240, 255),
            new Color32(229, 233, 240, 255));

        if (autoSize)
        {
            label.enableAutoSizing = true;
            label.fontSizeMin = autoSizeMin;
            label.fontSizeMax = autoSizeMax;
        }
    }

    void LoadSprites()
    {
        currencyBallSprite = LoadSprite("currency_ball");
        panelBackgroundSprite = LoadSprite("panel_background");
        plethBackgroundSprite = LoadSprite("pleth_background");
        if (plethBackgroundSprite == null)
            plethBackgroundSprite = panelBackgroundSprite;
        slotFrameEmptySprite = LoadSprite("slot_frame_empty");
        if (slotFrameEmptySprite == null)
            slotFrameEmptySprite = LoadSprite("slot_frame");
        slotFrameSelectedSprite = LoadSprite("slot_frame_selected");
        if (slotFrameSelectedSprite == null)
            slotFrameSelectedSprite = slotFrameEmptySprite;
        launchSprite = LoadSprite("launch_button");
        launchButtonCleanSprite = LoadSprite("launch_button_clean");
        if (launchButtonCleanSprite == null)
            launchButtonCleanSprite = launchSprite;
        dailyDropBadgeSprite = LoadSprite("daily_drop_badge");

        crateRustySprite = LoadSprite("crate_rusty");
        crateBrassSprite = LoadSprite("crate_brass");
        crateGoldenSprite = LoadSprite("crate_golden");
        orangeButtonSprite = LoadSprite("orange_btn");
        blueButtonSprite = LoadSprite("blue_btn");
        whiteButtonSprite = LoadSprite("white_btn");
        Sprite defaultCrate = LoadSprite("icon_crate") ?? LoadSprite("icon_noitem");
        if (crateRustySprite == null) crateRustySprite = defaultCrate;
        if (crateBrassSprite == null) crateBrassSprite = defaultCrate;
        if (crateGoldenSprite == null) crateGoldenSprite = defaultCrate;
    }

    Sprite GetCrateSpriteForIndex(int index)
    {
        if (index <= 0) return crateRustySprite;
        return crateBrassSprite;
    }

    string GetCrateTitle(int crateIndex)
    {
        switch (crateIndex)
        {
            case 0:
                return "Basic Box - 3 Balls";
            case 1:
                return "Pro Box - 7 Balls";
            default:
                return "Box";
        }
    }

    void BuildCratePerkRows(RectTransform parent, int crateIndex)
    {
        if (parent == null) return;

        switch (crateIndex)
        {
            case 0:
                AddPerkInfoRow(parent, "icon_pingpong", "Ping Pong: Stronger flippers.");
                AddPerkInfoRow(parent, "icon_healthbonus", "Health Bonus: +1 life chance.");
                AddPerkInfoRow(parent, "icon_ticketprize", "Ball Prize: instant Balls.");
                break;
            case 1:
                AddPerkInfoRow(parent, "icon_extraball", "Extra Ball: summon another ball.");
                AddPerkInfoRow(parent, "icon_angelwings", "Angel Wings: prevent one fall.");
                AddPerkInfoRow(parent, "icon_healthbonus", "Elite Health: better life roll.");
                break;
        }
    }

    void AddPerkInfoRow(RectTransform parent, string iconName, string lineText)
    {
        RectTransform row = CreateRect("Perk_Row", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 30f));
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 30f;

        HorizontalLayoutGroup rowGroup = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 8f;
        rowGroup.padding = new RectOffset(2, 2, 0, 0);
        rowGroup.childAlignment = TextAnchor.MiddleLeft;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = false;
        rowGroup.childForceExpandWidth = true;
        rowGroup.childForceExpandHeight = false;

        Sprite iconSprite = LoadSprite(iconName);
        Image icon = CreateImage("Perk_Icon", row, iconSprite);
        icon.rectTransform.sizeDelta = new Vector2(24f, 24f);
        icon.color = iconSprite == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
        LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 24f;
        iconLayout.preferredHeight = 24f;

        TMP_Text line = CreateLabel("Perk_Text", row, lineText, 18f, TextAlignmentOptions.Left);
        line.enableWordWrapping = false;
        line.color = new Color(0.16f, 0.18f, 0.23f, 1f);
        LayoutElement textLayout = line.gameObject.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        textLayout.preferredHeight = 30f;
    }

    int GetCratePrice(int crateType) =>
        crateType >= 0 && crateType < cratePrices.Length ? cratePrices[crateType] : 0;

    Crates GetCrateType(int crateType) =>
        crateType >= 0 && crateType < crateTypes.Length ? crateTypes[crateType] : Crates.Rusty;

    int GetFirstEmptyInventorySlot()
    {
        int slotCount = Mathf.Min(InventorySlotCount, Inventory.Slots.Length);
        for (int i = 0; i < slotCount; i++)
        {
            if (IsEmptyItem(Inventory.Slots[i]))
                return i;
        }

        return -1;
    }

    void SanitizeInventorySlots()
    {
        if (Items.instance == null) return;

        bool changed = false;
        int slotCount = Mathf.Min(InventorySlotCount, Inventory.Slots.Length);
        for (int i = 0; i < slotCount; i++)
        {
            Item item = Inventory.Slots[i];
            if (IsAllowedShopPerk(item)) continue;
            Inventory.Slots[i] = Items.instance != null ? Items.NoItem : null;
            changed = true;
        }

        if (!changed) return;
        Inventory.SetMemory();
        PlayerPrefs.Save();
        if (ItemGUI.instance != null)
            ItemGUI.instance.LoadItems();
    }

    static bool IsAllowedShopPerk(Item item)
    {
        if (item == null) return true;
        if (item is NoItem) return true;
        if (Items.instance == null) return false;
        return item == Items.PingPong ||
               item == Items.ExtraBall ||
               item == Items.HealthBonus ||
               item == Items.TicketPrize ||
               item == Items.AngelWings ||
               item == Items.NoItem;
    }

    Item GetDropFromCrate(int crateType)
    {
        switch (GetCrateType(crateType))
        {
            case Crates.Rusty:
                return LootTable.GetRustyDrop();
            case Crates.Brass:
                return LootTable.GetBrassDrop();
            case Crates.Golden:
                return LootTable.GetGoldenDrop();
            default:
                return Items.NoItem;
        }
    }

    int GetCurrentBalls()
    {
        if (Player.instance != null)
            return Player.instance.Tickets;

        return PlayerPrefs.GetInt("ticketCount", 0);
    }

    void SetCurrentBalls(int value)
    {
        int clampedValue = Mathf.Max(0, value);
        if (Player.instance != null)
        {
            Player.instance.Tickets = clampedValue;
            return;
        }

        PlayerPrefs.SetInt("ticketCount", clampedValue);
        if (ballCounterText != null)
            ballCounterText.text = clampedValue.ToString();
    }

    void PrepareInventorySlotForTransit(int inventorySlotIndex)
    {
        if (inventorySlotIndex < 0 || inventorySlotIndex >= InventorySlotCount) return;

        inventorySlotAnimating[inventorySlotIndex] = true;

        Image icon = inventorySlotIconImages[inventorySlotIndex];
        if (icon != null)
        {
            icon.enabled = false;
            icon.sprite = null;
            icon.color = Color.white;
            icon.rectTransform.localScale = Vector3.one;
        }

        Image glow = inventorySlotGlowImages[inventorySlotIndex];
        if (glow != null)
        {
            glow.enabled = false;
            glow.color = new Color(0.42f, 0.84f, 1f, 0f);
        }
    }

    IEnumerator PlayPurchaseTransit(int crateType, int inventorySlotIndex, Sprite droppedIcon)
    {
        Image targetIcon = inventorySlotIconImages[inventorySlotIndex];
        Image targetFrame = inventorySlotFrameImages[inventorySlotIndex];
        Image targetGlow = inventorySlotGlowImages[inventorySlotIndex];
        Image sourceIcon = crateType >= 0 && crateType < shopCrateIconImages.Length ? shopCrateIconImages[crateType] : null;
        RectTransform sourceRect = sourceIcon != null ? sourceIcon.rectTransform : null;
        RectTransform targetRect = targetIcon != null ? targetIcon.rectTransform : null;

        if (targetIcon == null || droppedIcon == null || sourceRect == null || targetRect == null || root == null)
        {
            FinalizeTransit(inventorySlotIndex, droppedIcon);
            yield break;
        }

        RectTransform floating = CreateRect("Drop_Transit_Icon", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));
        Image floatingImage = floating.gameObject.AddComponent<Image>();
        floatingImage.sprite = droppedIcon;
        floatingImage.preserveAspect = true;

        Vector3 sourceLocal3 = root.InverseTransformPoint(sourceRect.TransformPoint(sourceRect.rect.center));
        Vector3 targetLocal3 = root.InverseTransformPoint(targetRect.TransformPoint(targetRect.rect.center));
        Vector2 sourceLocal = new Vector2(sourceLocal3.x, sourceLocal3.y);
        Vector2 targetLocal = new Vector2(targetLocal3.x, targetLocal3.y);
        Vector2 sourceLifted = sourceLocal + new Vector2(0f, 26f);

        floating.anchoredPosition = sourceLocal;
        floating.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < PurchaseExtractDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PurchaseExtractDuration);
            floating.anchoredPosition = Vector2.Lerp(sourceLocal, sourceLifted, t);
            floating.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < PurchaseTransitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PurchaseTransitDuration);
            floating.anchoredPosition = Vector2.Lerp(sourceLifted, targetLocal, Mathf.SmoothStep(0f, 1f, t));
            floating.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, t);
            yield return null;
        }

        Destroy(floating.gameObject);

        targetIcon.sprite = droppedIcon;
        targetIcon.enabled = true;
        targetIcon.color = Color.white;
        targetIcon.rectTransform.localScale = Vector3.one;

        elapsed = 0f;
        while (elapsed < SlotFlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / SlotFlashDuration);
            if (targetFrame != null)
            {
                float pulse = Mathf.Sin(t * Mathf.PI);
                Color baseColor = new Color(0.94f, 0.97f, 1f, 0.95f);
                targetFrame.color = Color.Lerp(baseColor, slotFlashColor, pulse);
            }
            if (targetGlow != null)
            {
                targetGlow.enabled = true;
                targetGlow.color = new Color(0.42f, 0.84f, 1f, Mathf.Lerp(0.22f, 0.48f, Mathf.Sin(t * Mathf.PI)));
            }
            yield return null;
        }

        if (targetFrame != null)
            targetFrame.color = new Color(0.94f, 0.97f, 1f, 0.95f);
        if (targetGlow != null)
        {
            targetGlow.enabled = true;
            targetGlow.color = new Color(0.42f, 0.84f, 1f, 0.38f);
        }

        inventorySlotAnimating[inventorySlotIndex] = false;
        inventorySlotPulseCoroutines[inventorySlotIndex] = null;
        RefreshInventoryRow();
    }

    void FinalizeTransit(int inventorySlotIndex, Sprite droppedIcon)
    {
        if (inventorySlotIndex >= 0 && inventorySlotIndex < inventorySlotAnimating.Length)
            inventorySlotAnimating[inventorySlotIndex] = false;

        Image targetIcon = inventorySlotIndex >= 0 && inventorySlotIndex < inventorySlotIconImages.Length
            ? inventorySlotIconImages[inventorySlotIndex]
            : null;
        if (targetIcon != null)
        {
            targetIcon.sprite = droppedIcon;
            targetIcon.enabled = droppedIcon != null;
            targetIcon.color = Color.white;
            targetIcon.rectTransform.localScale = Vector3.one;
        }

        if (inventorySlotIndex >= 0 && inventorySlotIndex < inventorySlotPulseCoroutines.Length)
            inventorySlotPulseCoroutines[inventorySlotIndex] = null;

        RefreshInventoryRow();
    }

    bool TryApplyToyCoreItemIcons()
    {
        if (Items.instance == null) return false;

        AssignItemIcon(Items.Fireball, "icon_fireball");
        AssignItemIcon(Items.WaterDroplet, "icon_waterdroplet");
        AssignItemIcon(Items.LuckyCharm, "icon_luckycharm");
        AssignItemIcon(Items.CurseOfAnubis, "icon_curseofanubis");
        AssignItemIcon(Items.AngelWings, "icon_angelwings");
        AssignItemIcon(Items.CameraFlip, "icon_cameraflip");
        AssignItemIcon(Items.ExtraBall, "icon_extraball");
        AssignItemIcon(Items.HealthBonus, "icon_healthbonus");
        AssignItemIcon(Items.PingPong, "icon_pingpong");
        AssignItemIcon(Items.Rock, "icon_rock");
        AssignItemIcon(Items.TennisBall, "icon_tennisball");
        AssignItemIcon(Items.TicketPrize, "icon_ticketprize");
        return true;
    }

    void AssignItemIcon(Item item, string spriteName)
    {
        if (item == null) return;

        Sprite icon = LoadSprite(spriteName);
        if (icon != null)
            item.Icon = icon;
    }

    static bool IsEmptyItem(Item item)
    {
        if (item == null) return true;
        if (item is NoItem) return true;
        if (Items.instance != null && item == Items.NoItem) return true;
        return item.Icon == null;
    }

    static void PlayPurchaseFailSound()
    {
        if (ItemGUI.instance != null)
            ItemGUI.instance.PurchaseFailSound();
    }

    Sprite LoadSprite(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        if (spriteCache.TryGetValue(fileName, out Sprite cached))
            return cached;

        Sprite sprite = Resources.Load<Sprite>($"UI/Sprites/PSG-Ball/{fileName}");
        spriteCache[fileName] = sprite;
        return sprite;
    }

    static void SetObjectActiveByName(string objectName, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject sceneObject = allObjects[i];
            if (sceneObject == null) continue;
            if (sceneObject.name != objectName) continue;
            if (sceneObject.hideFlags != HideFlags.None) continue;
            if (sceneObject.scene.IsValid())
                sceneObject.SetActive(isActive);
        }
    }

    static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject node = new GameObject(name, typeof(RectTransform));
        RectTransform rect = node.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    static TMP_Text CreateLabel(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = new Color(0.11f, 0.14f, 0.19f, 1f);
        label.enableWordWrapping = false;
        return label;
    }

    static Button CreateUtilityButton(Transform parent, string objectName, string labelText, UnityEngine.Events.UnityAction onClick, float width)
    {
        RectTransform buttonRect = CreateRect(objectName, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 44f));
        LayoutElement layout = buttonRect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 44f;

        Image background = buttonRect.gameObject.AddComponent<Image>();
        background.color = new Color(0.93f, 0.94f, 0.96f, 1f);
        Outline outline = buttonRect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.81f, 0.87f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        if (onClick != null)
            button.onClick.AddListener(onClick);

        TMP_Text label = CreateLabel("Label", buttonRect, labelText, 22f, TextAlignmentOptions.Center);
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }
}
