using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Builds the full professional card-game UI layout in GameScene.
/// Run via menu: Game > Setup > Build Game UI
/// </summary>
public static class SceneBuilder
{
    // ── Color palette ──────────────────────────────────────────
    static readonly Color BG_DARK       = new Color(0.08f, 0.06f, 0.12f, 1f);
    static readonly Color PANEL_BG      = new Color(0.12f, 0.10f, 0.18f, 0.95f);
    static readonly Color PANEL_BORDER  = new Color(0.5f, 0.35f, 0.1f, 1f);
    static readonly Color BTN_ATTACK    = new Color(0.7f, 0.15f, 0.1f, 1f);
    static readonly Color BTN_ENDTURN   = new Color(0.1f, 0.45f, 0.1f, 1f);
    static readonly Color BTN_HIDDEN    = new Color(0.3f, 0.1f, 0.5f, 1f);
    static readonly Color BTN_REVEAL    = new Color(0.5f, 0.4f, 0.0f, 1f);
    static readonly Color BTN_ACTIVATE  = new Color(0.6f, 0.2f, 0.4f, 1f);
    static readonly Color BTN_MAINMENU  = new Color(0.15f, 0.35f, 0.55f, 1f);
    static readonly Color TEXT_GOLD     = new Color(0.95f, 0.8f, 0.3f, 1f);
    static readonly Color TEXT_WHITE    = Color.white;
    static readonly Color TEXT_DIM      = new Color(0.7f, 0.7f, 0.7f, 1f);
    static readonly Color LOG_BG        = new Color(0.05f, 0.04f, 0.08f, 0.92f);
    static readonly Color GAMEOVER_BG   = new Color(0f, 0f, 0f, 0.85f);

    [MenuItem("Game/Setup/Build Game UI")]
    public static void BuildGameUI()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.name.Contains("Game") && !scene.name.Contains("Sample"))
        {
            Debug.LogWarning("[SceneBuilder] Open GameScene first.");
        }

        // Clear existing Canvas
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            Object.DestroyImmediate(canvas.gameObject);

        // Ensure EventSystem
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ── Root Canvas ──────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas2 = canvasGO.AddComponent<Canvas>();
        canvas2.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas2.sortingOrder = 0;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Background ───────────────────────────────────────────
        var bgGO = CreateImage(canvasGO, "Background", BG_DARK);
        StretchFull(bgGO);

        // Layout constants (all in anchor ratios)
        // Left column:      x 0.00 → 0.15
        // Center area:      x 0.15 → 0.79
        //   Player panels:  y 0.00 → 0.28  (taller panels)
        //   Hand area:      y 0.28 → 0.62  (tall enough for card art)
        //   Deck/discard:   y 0.62 → 0.74
        //   Empty/upper:    y 0.74 → 0.94
        // Right column:     x 0.79 → 1.00
        // Top bar:          y 0.94 → 1.00

        // ── Top Bar ──────────────────────────────────────────────
        var topBar = CreateImage(canvasGO, "TopBar", PANEL_BG);
        SetAnchored(topBar,
            new Vector2(0f, 0.94f), new Vector2(1f, 1f),
            new Vector2(0, 0), new Vector2(0, 0));

        var titleTxt = CreateTMPText(topBar, "TitleText", "BÓNG TỐI TRIỀU ĐÌNH", 26, TEXT_GOLD, TextAlignmentOptions.Left);
        SetAnchored(titleTxt, new Vector2(0, 0), new Vector2(0.45f, 1), new Vector2(20, 0), new Vector2(0, 0));

        var roundTxt = CreateTMPText(topBar, "RoundText", "Vòng 1", 24, TEXT_WHITE, TextAlignmentOptions.Center);
        SetAnchored(roundTxt, new Vector2(0.35f, 0), new Vector2(0.65f, 1), Vector2.zero, Vector2.zero);

        var btnTopMainMenu = CreateButton(topBar, "TopBarMainMenuButton", "Main Menu", BTN_MAINMENU);
        SetAnchored(btnTopMainMenu, new Vector2(0.80f, 0.1f), new Vector2(0.995f, 0.9f), Vector2.zero, Vector2.zero);

        // ── Current Player Info Panel (left, upper) ──────────────
        var infoPanel = CreateImage(canvasGO, "InfoPanel", PANEL_BG);
        SetAnchored(infoPanel,
            new Vector2(0f, 0.37f), new Vector2(0.15f, 0.94f),
            new Vector2(6, 4), new Vector2(-4, -4));
        AddOutline(infoPanel, PANEL_BORDER);

        var infoTitle = CreateTMPText(infoPanel, "InfoTitle", "Lượt hiện tại", 16, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchored(infoTitle, new Vector2(0, 0.90f), new Vector2(1, 1f), new Vector2(4, 0), new Vector2(-4, 0));

        var playerNameTxt = CreateTMPText(infoPanel, "PlayerNameText", "Player 1", 20, TEXT_WHITE, TextAlignmentOptions.Center);
        SetAnchored(playerNameTxt, new Vector2(0, 0.78f), new Vector2(1, 0.90f), new Vector2(4, 0), new Vector2(-4, 0));

        var hpTxt = CreateTMPText(infoPanel, "HpText", "Khí huyết: 4", 15, new Color(1, 0.3f, 0.3f), TextAlignmentOptions.Left);
        SetAnchored(hpTxt, new Vector2(0, 0.67f), new Vector2(1, 0.78f), new Vector2(10, 0), new Vector2(-4, 0));

        var staminaTxt = CreateTMPText(infoPanel, "StaminaText", "Thể lực: 4", 15, new Color(0.3f, 0.8f, 1f), TextAlignmentOptions.Left);
        SetAnchored(staminaTxt, new Vector2(0, 0.56f), new Vector2(1, 0.67f), new Vector2(10, 0), new Vector2(-4, 0));

        var atkTxt = CreateTMPText(infoPanel, "AttackText", "Tấn công: 4", 15, new Color(1, 0.6f, 0.2f), TextAlignmentOptions.Left);
        SetAnchored(atkTxt, new Vector2(0, 0.45f), new Vector2(1, 0.56f), new Vector2(10, 0), new Vector2(-4, 0));

        var defTxt = CreateTMPText(infoPanel, "DefenseText", "Phòng thủ: 3", 15, new Color(0.4f, 0.8f, 0.4f), TextAlignmentOptions.Left);
        SetAnchored(defTxt, new Vector2(0, 0.34f), new Vector2(1, 0.45f), new Vector2(10, 0), new Vector2(-4, 0));

        var roleTxt = CreateTMPText(infoPanel, "RoleText", "Vai trò: Ẩn", 15, TEXT_GOLD, TextAlignmentOptions.Left);
        SetAnchored(roleTxt, new Vector2(0, 0.23f), new Vector2(1, 0.34f), new Vector2(10, 0), new Vector2(-4, 0));

        // Role image inside info panel
        var roleImgGO = CreateImage(infoPanel, "RoleImage", Color.white);
        SetAnchored(roleImgGO, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.22f), Vector2.zero, Vector2.zero);
        var roleImg = roleImgGO.GetComponent<Image>();
        roleImg.preserveAspect = true;
        roleImgGO.SetActive(false);

        // ── Action Buttons (left, lower) ─────────────────────────
        // 5 buttons stacked evenly, each row = 1/6 height, label row on top
        var btnPanel = CreateImage(canvasGO, "ButtonPanel", PANEL_BG);
        SetAnchored(btnPanel,
            new Vector2(0f, 0f), new Vector2(0.15f, 0.37f),
            new Vector2(6, 4), new Vector2(-4, -4));
        AddOutline(btnPanel, PANEL_BORDER);

        var targetInfoTxt = CreateTMPText(btnPanel, "TargetInfoText", "Chưa chọn mục tiêu", 13, TEXT_DIM, TextAlignmentOptions.Center);
        SetAnchored(targetInfoTxt, new Vector2(0, 0.83f), new Vector2(1, 1f), new Vector2(4, 0), new Vector2(-4, 0));

        // 5 buttons, each occupies 1/6 of height (0.83 remaining / 5 = ~0.166 each)
        // rows from bottom: EndTurn(0→0.166), Activate(0.166→0.332), Hidden(0.332→0.498), Attack(0.498→0.664), Reveal(0.664→0.83)
        var btnReveal = CreateButton(btnPanel, "RevealRoleButton", "Công khai Vai trò", BTN_REVEAL);
        SetAnchored(btnReveal, new Vector2(0.04f, 0.664f), new Vector2(0.96f, 0.825f), Vector2.zero, Vector2.zero);

        var btnAttack = CreateButton(btnPanel, "AttackButton", "Đòn đánh (3 ST)", BTN_ATTACK);
        SetAnchored(btnAttack, new Vector2(0.04f, 0.498f), new Vector2(0.96f, 0.66f), Vector2.zero, Vector2.zero);

        var btnHidden = CreateButton(btnPanel, "PlaceHiddenButton", "Đặt Ám sát Ẩn", BTN_HIDDEN);
        SetAnchored(btnHidden, new Vector2(0.04f, 0.332f), new Vector2(0.96f, 0.494f), Vector2.zero, Vector2.zero);

        var btnActivate = CreateButton(btnPanel, "ActivateHiddenButton", "Kích hoạt Ẩn (5 ST)", BTN_ACTIVATE);
        SetAnchored(btnActivate, new Vector2(0.04f, 0.166f), new Vector2(0.96f, 0.328f), Vector2.zero, Vector2.zero);

        var btnEnd = CreateButton(btnPanel, "EndTurnButton", "Kết thúc lượt", BTN_ENDTURN);
        SetAnchored(btnEnd, new Vector2(0.04f, 0f), new Vector2(0.96f, 0.162f), new Vector2(0, 3), new Vector2(0, -3));

        // ── Player Panels Area (center bottom, y=0→28%) ──────────
        var panelContainer = new GameObject("PlayerPanelContainer");
        panelContainer.transform.SetParent(canvasGO.transform, false);
        panelContainer.AddComponent<RectTransform>();
        SetAnchored(panelContainer,
            new Vector2(0.15f, 0f), new Vector2(0.79f, 0.28f),
            new Vector2(4, 4), new Vector2(-4, -4));

        var hlg = panelContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.padding = new RectOffset(6, 6, 6, 6);

        // ── Hand Area (center, y=28%→62%) ─────────────────────────
        var handArea = CreateImage(canvasGO, "HandArea", new Color(0.05f, 0.08f, 0.06f, 0.85f));
        SetAnchored(handArea,
            new Vector2(0.15f, 0.28f), new Vector2(0.79f, 0.62f),
            new Vector2(4, 4), new Vector2(-4, -4));
        AddOutline(handArea, PANEL_BORDER);

        var handTitle = CreateTMPText(handArea, "HandTitle", "Bài trên Tay", 16, TEXT_GOLD, TextAlignmentOptions.Left);
        SetAnchored(handTitle, new Vector2(0, 0.88f), new Vector2(0.5f, 1f), new Vector2(10, 0), new Vector2(0, 0));

        var deckCountTxt = CreateTMPText(handArea, "DeckCountText", "Deck: 0", 15, TEXT_DIM, TextAlignmentOptions.Right);
        SetAnchored(deckCountTxt, new Vector2(0.5f, 0.88f), new Vector2(1f, 1f), new Vector2(0, 0), new Vector2(-10, 0));

        // Card slots container — fills the lower 88% of HandArea
        var handContainer = new GameObject("HandContainer");
        handContainer.transform.SetParent(handArea.transform, false);
        handContainer.AddComponent<RectTransform>();
        SetAnchored(handContainer, new Vector2(0f, 0f), new Vector2(1f, 0.87f), new Vector2(10, 6), new Vector2(-10, -6));
        var handHLG = handContainer.AddComponent<HorizontalLayoutGroup>();
        handHLG.spacing = 10;
        handHLG.childForceExpandWidth = false;
        handHLG.childForceExpandHeight = true;
        handHLG.childAlignment = TextAnchor.MiddleCenter;
        handHLG.padding = new RectOffset(6, 6, 4, 4);

        // ── Deck / Discard / Draw button (center, y=62%→74%) ──────
        var deckArea = CreateImage(canvasGO, "DeckArea", PANEL_BG);
        SetAnchored(deckArea,
            new Vector2(0.15f, 0.62f), new Vector2(0.79f, 0.74f),
            new Vector2(4, 4), new Vector2(-4, -4));
        AddOutline(deckArea, PANEL_BORDER);

        // Deck pile visual (left side)
        var deckPile = CreateImage(deckArea, "DeckPile", new Color(0.2f, 0.15f, 0.05f));
        SetAnchored(deckPile, new Vector2(0.02f, 0.08f), new Vector2(0.20f, 0.92f), Vector2.zero, Vector2.zero);
        var deckPileImg = deckPile.GetComponent<Image>();
        deckPileImg.color = new Color(0.15f, 0.10f, 0.04f);
        AddOutline(deckPile, new Color(0.6f, 0.45f, 0.15f));

        var deckLabel = CreateTMPText(deckPile, "DeckLabel", "Bộ bài\n(Sấp)", 13, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchored(deckLabel, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));

        // Draw button
        var btnDraw = CreateButton(deckArea, "DrawCardButton", "Bốc bài", new Color(0.1f, 0.4f, 0.55f));
        SetAnchored(btnDraw, new Vector2(0.22f, 0.15f), new Vector2(0.45f, 0.85f), Vector2.zero, Vector2.zero);

        // Spacer — no second end-turn button needed

        // Discard pile visual (right side)
        var discardPile = CreateImage(deckArea, "DiscardPile", new Color(0.12f, 0.05f, 0.05f));
        SetAnchored(discardPile, new Vector2(0.74f, 0.08f), new Vector2(0.98f, 0.92f), Vector2.zero, Vector2.zero);
        AddOutline(discardPile, new Color(0.5f, 0.2f, 0.1f));

        var discardLabel = CreateTMPText(discardPile, "DiscardLabel", "Bài\nBỏ ra", 13, TEXT_DIM, TextAlignmentOptions.Center);
        SetAnchored(discardLabel, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));

        // ── Peek Overlay (full-screen, hidden by default) ─────────
        var peekOverlay = CreateImage(canvasGO, "PeekOverlay", new Color(0, 0, 0, 0.80f));
        StretchFull(peekOverlay);
        peekOverlay.SetActive(false);

        var peekTitle = CreateTMPText(peekOverlay, "PeekTitle", "Thầy bói — 3 lá trên deck:", 24, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchored(peekTitle, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.84f), Vector2.zero, Vector2.zero);

        var peekContainer = new GameObject("PeekContainer");
        peekContainer.transform.SetParent(peekOverlay.transform, false);
        peekContainer.AddComponent<RectTransform>();
        SetAnchored(peekContainer, new Vector2(0.2f, 0.40f), new Vector2(0.8f, 0.72f), Vector2.zero, Vector2.zero);
        var peekHLG = peekContainer.AddComponent<HorizontalLayoutGroup>();
        peekHLG.spacing = 20;
        peekHLG.childForceExpandWidth = false;
        peekHLG.childForceExpandHeight = true;
        peekHLG.childAlignment = TextAnchor.MiddleCenter;

        var peekClose = CreateButton(peekOverlay, "PeekCloseButton", "Đóng", BTN_ENDTURN);
        SetAnchored(peekClose, new Vector2(0.4f, 0.30f), new Vector2(0.6f, 0.40f), Vector2.zero, Vector2.zero);
        // Listener is wired at runtime by UIManager.Start()

        // ── Game Log Panel (right column, x=79%→100%, full height) ──
        var logPanel = CreateImage(canvasGO, "LogPanel", LOG_BG);
        SetAnchored(logPanel,
            new Vector2(0.79f, 0f), new Vector2(1f, 0.94f),
            new Vector2(4, 4), new Vector2(-4, -4));
        AddOutline(logPanel, PANEL_BORDER);

        var logTitle = CreateTMPText(logPanel, "LogTitle", "Nhật ký vòng chơi", 16, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchored(logTitle, new Vector2(0, 0.92f), new Vector2(1, 1f), new Vector2(5, 0), new Vector2(-5, 0));

        var logTxt = CreateTMPText(logPanel, "LogText", "", 14, TEXT_DIM, TextAlignmentOptions.TopLeft);
        SetAnchored(logTxt, new Vector2(0, 0), new Vector2(1, 0.92f), new Vector2(10, 8), new Vector2(-10, -4));
        logTxt.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Truncate;

        // ── GameOver Panel ────────────────────────────────────────
        var goPanel = CreateImage(canvasGO, "GameOverPanel", GAMEOVER_BG);
        StretchFull(goPanel);
        goPanel.SetActive(false);

        var goTxt = CreateTMPText(goPanel, "GameOverText", "Kết thúc", 48, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchored(goTxt, new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.7f), Vector2.zero, Vector2.zero);

        var goRestartBtn = CreateButton(goPanel, "RestartButton", "Ván mới", BTN_ENDTURN);
        SetAnchored(goRestartBtn, new Vector2(0.35f, 0.28f), new Vector2(0.55f, 0.38f), Vector2.zero, Vector2.zero);
        goRestartBtn.AddComponent<RestartButton>();

        var goMainMenuBtn = CreateButton(goPanel, "GameOverMainMenuButton", "Về Main Menu", BTN_MAINMENU);
        SetAnchored(goMainMenuBtn, new Vector2(0.35f, 0.16f), new Vector2(0.55f, 0.26f), Vector2.zero, Vector2.zero);

        // ── Wire up GameManager & UIManager ──────────────────────
        GameManager gm = Object.FindObjectOfType<GameManager>();
        UIManager ui = Object.FindObjectOfType<UIManager>();

        if (ui != null)
        {
            ui.playerNameText    = playerNameTxt.GetComponent<TextMeshProUGUI>();
            ui.hpText            = hpTxt.GetComponent<TextMeshProUGUI>();
            ui.staminaText       = staminaTxt.GetComponent<TextMeshProUGUI>();
            ui.attackText        = atkTxt.GetComponent<TextMeshProUGUI>();
            ui.defenseText       = defTxt.GetComponent<TextMeshProUGUI>();
            ui.roleText          = roleTxt.GetComponent<TextMeshProUGUI>();
            ui.roundText         = roundTxt.GetComponent<TextMeshProUGUI>();
            ui.roleImage         = roleImg;
            ui.logText           = logTxt.GetComponent<TextMeshProUGUI>();
            ui.gameOverPanel     = goPanel;
            ui.gameOverText      = goTxt.GetComponent<TextMeshProUGUI>();
            ui.attackButton      = btnAttack.GetComponent<Button>();
            ui.endTurnButton     = btnEnd.GetComponent<Button>();
            ui.placeHiddenButton = btnHidden.GetComponent<Button>();
            ui.activateHiddenButton = btnActivate.GetComponent<Button>();
            ui.revealRoleButton  = btnReveal.GetComponent<Button>();
            ui.targetInfoText    = targetInfoTxt.GetComponent<TextMeshProUGUI>();
            // Card system
            ui.drawCardButton    = btnDraw.GetComponent<Button>();
            ui.handContainer     = handContainer.transform;
            ui.deckCountText     = deckCountTxt.GetComponent<TextMeshProUGUI>();
            ui.peekOverlay       = peekOverlay;
            ui.peekContainer     = peekContainer.transform;

            // Load CardUI prefab if exists
            GameObject cardPrefabAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardUI.prefab");
            if (cardPrefabAsset != null)
                ui.cardUIPrefab = cardPrefabAsset;

            EditorUtility.SetDirty(ui);
        }

        if (gm != null)
        {
            gm.playerPanelContainer = panelContainer.transform;

            // Assign PlayerPanel prefab if it already exists
            GameObject ppPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerPanel.prefab");
            if (ppPrefab != null)
                gm.playerPanelPrefab = ppPrefab;

            // Ensure DeckManager exists in scene
            DeckManager dm = Object.FindFirstObjectByType<DeckManager>();
            if (dm == null)
            {
                var dmGO = new GameObject("DeckManager");
                dm = dmGO.AddComponent<DeckManager>();
            }
            gm.deckManager = dm;
            EditorUtility.SetDirty(gm);
            EditorUtility.SetDirty(dm);
        }

        // ── Ensure Main Camera is set up for 2D UI ────────────────
        var mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            mainCam = camGO.AddComponent<Camera>();
            if (camGO.GetComponent<AudioListener>() == null)
                camGO.AddComponent<AudioListener>();
        }
        mainCam.orthographic = true;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = BG_DARK;
        mainCam.transform.position = new Vector3(0, 0, -10);
        mainCam.transform.rotation = Quaternion.identity;
        mainCam.nearClipPlane = 0.1f;
        mainCam.farClipPlane  = 100f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] Game UI built and saved. Run: Create All Role Assets -> Auto-Assign Roles -> Create PlayerPanel Prefab -> Create Card UI Prefab -> Auto-Assign Cards.");
    }

    // ── Helper: Create PlayerPanel Prefab ────────────────────────
    [MenuItem("Game/Setup/Create PlayerPanel Prefab")]
    public static void CreatePlayerPanelPrefab()
    {
        // Save to both Prefabs/ (for Inspector) and Resources/Prefabs/ (for runtime fallback)
        string prefabPath = "Assets/Prefabs/PlayerPanel.prefab";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefabs"))
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");

        // Root
        var root = new GameObject("PlayerPanel");
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0.12f, 0.10f, 0.18f, 0.95f);
        var btn = root.AddComponent<Button>();
        var ppUI = root.AddComponent<PlayerPanelUI>();

        // Background (separate from root for highlight)
        var bg = CreateImage(root, "Background", new Color(0.12f, 0.10f, 0.18f, 0.95f));
        StretchFull(bg);
        ppUI.background = bg.GetComponent<Image>();

        // Outline border
        var outline = bg.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.35f, 0.1f, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        // Name label (top 15%)
        var nameTxt = CreateTMPText(root, "PlayerName", "Player 1", 16, Color.white, TextAlignmentOptions.Center);
        SetAnchored(nameTxt, new Vector2(0, 0.86f), new Vector2(1, 1f), new Vector2(3, 0), new Vector2(-3, -2));
        nameTxt.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
        nameTxt.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Ellipsis;
        ppUI.nameText = nameTxt.GetComponent<TextMeshProUGUI>();

        // Role thumbnail area (left 45%, middle band)
        var roleThumb = CreateImage(root, "RoleCardImage", Color.white);
        SetAnchored(roleThumb, new Vector2(0.03f, 0.42f), new Vector2(0.48f, 0.85f), Vector2.zero, Vector2.zero);
        roleThumb.GetComponent<Image>().preserveAspect = true;
        roleThumb.SetActive(false);
        ppUI.roleCardImage = roleThumb.GetComponent<Image>();

        // Role name (right side, upper middle)
        var roleNameTxt = CreateTMPText(root, "RoleNameText", "???", 13, new Color(0.95f, 0.8f, 0.3f), TextAlignmentOptions.Center);
        SetAnchored(roleNameTxt, new Vector2(0.50f, 0.66f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero);
        roleNameTxt.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;
        ppUI.roleNameText = roleNameTxt.GetComponent<TextMeshProUGUI>();

        // HP (left of middle row)
        var hpT = CreateTMPText(root, "HpText", "HP 4", 14, new Color(1, 0.35f, 0.35f), TextAlignmentOptions.Center);
        SetAnchored(hpT, new Vector2(0.02f, 0.50f), new Vector2(0.50f, 0.66f), Vector2.zero, Vector2.zero);
        ppUI.hpText = hpT.GetComponent<TextMeshProUGUI>();

        // Stamina (right of middle row)
        var stT = CreateTMPText(root, "StaminaText", "ST 4", 14, new Color(0.3f, 0.8f, 1f), TextAlignmentOptions.Center);
        SetAnchored(stT, new Vector2(0.50f, 0.50f), new Vector2(0.98f, 0.66f), Vector2.zero, Vector2.zero);
        ppUI.staminaText = stT.GetComponent<TextMeshProUGUI>();

        // ATK / DEF (bottom band)
        var adT = CreateTMPText(root, "AtkDefText", "ATK 4 / DEF 3", 12, new Color(0.75f, 0.75f, 0.75f), TextAlignmentOptions.Center);
        SetAnchored(adT, new Vector2(0.02f, 0.33f), new Vector2(0.98f, 0.50f), Vector2.zero, Vector2.zero);
        ppUI.atkDefText = adT.GetComponent<TextMeshProUGUI>();

        // Dead overlay
        var deadOv = CreateImage(root, "DeadOverlay", new Color(0, 0, 0, 0.7f));
        StretchFull(deadOv);
        var deadTxt = CreateTMPText(deadOv, "DeadText", "✝ BỊ HẠ", 24, new Color(0.8f, 0.1f, 0.1f), TextAlignmentOptions.Center);
        SetAnchored(deadTxt, new Vector2(0, 0.3f), new Vector2(1, 0.7f), Vector2.zero, Vector2.zero);
        deadOv.SetActive(false);
        ppUI.deadOverlay = deadOv;

        // Selected indicator (outline glow)
        var selInd = CreateImage(root, "SelectedIndicator", new Color(0.9f, 0.6f, 0, 0));
        StretchFull(selInd);
        var selOutline = selInd.AddComponent<Outline>();
        selOutline.effectColor = new Color(0.9f, 0.6f, 0, 1f);
        selOutline.effectDistance = new Vector2(3, -3);
        selInd.SetActive(false);
        ppUI.selectedIndicator = selInd;

        ppUI.button = btn;

        // LayoutElement — panels grow to fill container height, fixed min width
        var le = root.AddComponent<LayoutElement>();
        le.preferredWidth  = 150;
        le.preferredHeight = 200;
        le.minWidth  = 110;
        le.minHeight = 160;

        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        // Also copy to Resources for runtime fallback
        string resPrefabPath = "Assets/Resources/Prefabs/PlayerPanel.prefab";
        AssetDatabase.CopyAsset(prefabPath, resPrefabPath);

        // Assign to GameManager and save scene
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.playerPanelPrefab = prefab;
            EditorUtility.SetDirty(gm);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] PlayerPanel prefab assigned to GameManager and scene saved.");
        }
        else
        {
            Debug.LogWarning("[SceneBuilder] PlayerPanel prefab created but GameManager not found — run Build Game UI first, then Create PlayerPanel Prefab again.");
        }

        AssetDatabase.Refresh();
        Debug.Log("[SceneBuilder] PlayerPanel prefab created at " + prefabPath);
    }

    // ── Low-level helpers ─────────────────────────────────────────

    static GameObject CreateImage(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject CreateTMPText(GameObject parent, string name, string text, int fontSize,
        Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        return go;
    }

    static GameObject CreateButton(GameObject parent, string name, string label, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();

        // Button color block
        var cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.3f;
        cb.pressedColor = bgColor * 0.7f;
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = cb;

        var rounded = go.AddComponent<Outline>();
        rounded.effectColor = new Color(1, 1, 1, 0.3f);
        rounded.effectDistance = new Vector2(1, -1);

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var rt = labelGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        return go;
    }

    static void AddOutline(GameObject go, Color color)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2, -2);
    }

    static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetAnchored(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
