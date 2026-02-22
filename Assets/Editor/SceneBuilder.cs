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

        // ── Layout constants ─────────────────────────────────────
        // TopBar:            y 0.95 → 1.00  (5%)
        // Left column:       x 0.00 → 0.14  (14%)
        //   InfoPanel:       y 0.30 → 0.95  (65%)
        //   ButtonPanel:     y 0.00 → 0.30  (30%)
        // Center column:     x 0.14 → 0.79  (65%)
        //   DeckArea:        y 0.74 → 0.95  (21%)
        //   HandArea:        y 0.30 → 0.74  (44%)
        //   PlayerPanels:    y 0.00 → 0.30  (30%)
        // Right column:      x 0.79 → 1.00  (21%)
        //   LogPanel:        y 0.00 → 0.95  (full)

        const float LEFT_X   = 0.14f;
        const float RIGHT_X  = 0.79f;
        const float TOP_Y    = 0.95f;
        const float DECK_Y   = 0.74f;
        const float HAND_Y   = 0.30f;
        const float BTN_Y    = 0.30f;
        const float PAD      = 4f;

        // ── Top Bar ──────────────────────────────────────────────
        var topBar = CreateImage(canvasGO, "TopBar", PANEL_BG);
        SetAnchored(topBar,
            new Vector2(0f, TOP_Y), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero);

        var titleTxt = CreateTMPText(topBar, "TitleText", "BÓNG TỐI TRIỀU ĐÌNH", 26, TEXT_GOLD, TextAlignmentOptions.Left);
        SetAnchored(titleTxt, new Vector2(0f, 0f), new Vector2(0.40f, 1f), new Vector2(16, 0), Vector2.zero);

        var roundTxt = CreateTMPText(topBar, "RoundText", "Vòng 1", 24, TEXT_WHITE, TextAlignmentOptions.Center);
        SetAnchored(roundTxt, new Vector2(0.38f, 0f), new Vector2(0.62f, 1f), Vector2.zero, Vector2.zero);

        var btnTopMainMenu = CreateButton(topBar, "TopBarMainMenuButton", "Main Menu", BTN_MAINMENU);
        SetAnchored(btnTopMainMenu, new Vector2(0.80f, 0.08f), new Vector2(0.995f, 0.92f), Vector2.zero, Vector2.zero);

        // ── Info Panel (left, upper — y: BTN_Y → TOP_Y) ─────────
        var infoPanel = CreateImage(canvasGO, "InfoPanel", PANEL_BG);
        SetAnchored(infoPanel,
            new Vector2(0f, BTN_Y), new Vector2(LEFT_X, TOP_Y),
            new Vector2(PAD, PAD), new Vector2(-PAD, -PAD));
        AddOutline(infoPanel, PANEL_BORDER);

        var infoTitle = CreateTMPText(infoPanel, "InfoTitle", "Lượt hiện tại", 15, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchored(infoTitle, new Vector2(0f, 0.91f), new Vector2(1f, 1f), new Vector2(4, 0), new Vector2(-4, 0));

        var playerNameTxt = CreateTMPText(infoPanel, "PlayerNameText", "Player 1", 19, TEXT_WHITE, TextAlignmentOptions.Center);
        SetAnchored(playerNameTxt, new Vector2(0f, 0.80f), new Vector2(1f, 0.91f), new Vector2(4, 0), new Vector2(-4, 0));

        var hpTxt = CreateTMPText(infoPanel, "HpText", "Khí huyết: 4", 14, new Color(1f, 0.3f, 0.3f), TextAlignmentOptions.Left);
        SetAnchored(hpTxt, new Vector2(0f, 0.69f), new Vector2(1f, 0.80f), new Vector2(10, 0), new Vector2(-4, 0));

        var staminaTxt = CreateTMPText(infoPanel, "StaminaText", "Thể lực: 4", 14, new Color(0.3f, 0.8f, 1f), TextAlignmentOptions.Left);
        SetAnchored(staminaTxt, new Vector2(0f, 0.58f), new Vector2(1f, 0.69f), new Vector2(10, 0), new Vector2(-4, 0));

        var atkTxt = CreateTMPText(infoPanel, "AttackText", "Tấn công: 4", 14, new Color(1f, 0.6f, 0.2f), TextAlignmentOptions.Left);
        SetAnchored(atkTxt, new Vector2(0f, 0.47f), new Vector2(1f, 0.58f), new Vector2(10, 0), new Vector2(-4, 0));

        var defTxt = CreateTMPText(infoPanel, "DefenseText", "Phòng thủ: 3", 14, new Color(0.4f, 0.8f, 0.4f), TextAlignmentOptions.Left);
        SetAnchored(defTxt, new Vector2(0f, 0.36f), new Vector2(1f, 0.47f), new Vector2(10, 0), new Vector2(-4, 0));

        var roleTxt = CreateTMPText(infoPanel, "RoleText", "Vai trò: Ẩn", 14, TEXT_GOLD, TextAlignmentOptions.Left);
        SetAnchored(roleTxt, new Vector2(0f, 0.25f), new Vector2(1f, 0.36f), new Vector2(10, 0), new Vector2(-4, 0));

        var roleImgGO = CreateImage(infoPanel, "RoleImage", Color.white);
        SetAnchored(roleImgGO, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.24f), Vector2.zero, Vector2.zero);
        var roleImg = roleImgGO.GetComponent<Image>();
        roleImg.preserveAspect = true;
        roleImgGO.SetActive(false);

        // ── Button Panel (left, lower — y: 0 → BTN_Y) ───────────
        var btnPanel = CreateImage(canvasGO, "ButtonPanel", PANEL_BG);
        SetAnchored(btnPanel,
            new Vector2(0f, 0f), new Vector2(LEFT_X, BTN_Y),
            new Vector2(PAD, PAD), new Vector2(-PAD, -PAD));
        AddOutline(btnPanel, PANEL_BORDER);

        var targetInfoTxt = CreateTMPText(btnPanel, "TargetInfoText", "Chưa chọn mục tiêu", 12, TEXT_DIM, TextAlignmentOptions.Center);
        SetAnchored(targetInfoTxt, new Vector2(0f, 0.82f), new Vector2(1f, 1f), new Vector2(4, 0), new Vector2(-4, 0));

        // 4 nút đều nhau, rows từ dưới lên
        var btnReveal = CreateButton(btnPanel, "RevealRoleButton", "Công khai Vai trò", BTN_REVEAL);
        SetAnchored(btnReveal, new Vector2(0.04f, 0.615f), new Vector2(0.96f, 0.815f), Vector2.zero, Vector2.zero);

        var btnAttack = CreateButton(btnPanel, "AttackButton", "Đòn đánh (3 ST)", BTN_ATTACK);
        SetAnchored(btnAttack, new Vector2(0.04f, 0.41f), new Vector2(0.96f, 0.61f), Vector2.zero, Vector2.zero);

        var btnActivate = CreateButton(btnPanel, "ActivateSecretButton", "Kích hoạt Ám sát (5 ST)", BTN_ACTIVATE);
        SetAnchored(btnActivate, new Vector2(0.04f, 0.205f), new Vector2(0.96f, 0.405f), Vector2.zero, Vector2.zero);

        var btnEnd = CreateButton(btnPanel, "EndTurnButton", "Kết thúc lượt", BTN_ENDTURN);
        SetAnchored(btnEnd, new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.20f), Vector2.zero, Vector2.zero);

        // ── Deck / Discard / Draw (center, y: DECK_Y → TOP_Y) ────
        var deckArea = CreateImage(canvasGO, "DeckArea", PANEL_BG);
        SetAnchored(deckArea,
            new Vector2(LEFT_X, DECK_Y), new Vector2(RIGHT_X, TOP_Y),
            new Vector2(PAD, PAD), new Vector2(-PAD, -PAD));
        AddOutline(deckArea, PANEL_BORDER);

        // DeckArea uses HorizontalLayoutGroup for 3 sections
        var deckHLG = deckArea.AddComponent<HorizontalLayoutGroup>();
        deckHLG.childForceExpandWidth  = true;
        deckHLG.childForceExpandHeight = true;
        deckHLG.spacing  = 8;
        deckHLG.padding  = new RectOffset(8, 8, 6, 6);

        var deckPile = CreateImage(deckArea, "DeckPile", new Color(0.15f, 0.10f, 0.04f));
        AddOutline(deckPile, new Color(0.6f, 0.45f, 0.15f));
        var deckLE = deckPile.AddComponent<LayoutElement>();
        deckLE.flexibleWidth = 1f;
        var deckLabel = CreateTMPText(deckPile, "DeckLabel", "Bộ bài\n(Sấp)", 13, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchored(deckLabel, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));

        var btnDraw = CreateButton(deckArea, "DrawCardButton", "Bốc bài", new Color(0.1f, 0.4f, 0.55f));
        var drawLE = btnDraw.AddComponent<LayoutElement>();
        drawLE.flexibleWidth = 1.4f;

        var discardPile = CreateImage(deckArea, "DiscardPile", new Color(0.12f, 0.05f, 0.05f));
        AddOutline(discardPile, new Color(0.5f, 0.2f, 0.1f));
        var discardLE = discardPile.AddComponent<LayoutElement>();
        discardLE.flexibleWidth = 1f;
        var discardLabel = CreateTMPText(discardPile, "DiscardLabel", "Bài\nBỏ ra", 13, TEXT_DIM, TextAlignmentOptions.Center);
        SetAnchored(discardLabel, Vector2.zero, Vector2.one, new Vector2(2, 2), new Vector2(-2, -2));

        // ── Hand Area (center, y: HAND_Y → DECK_Y) ───────────────
        var handArea = CreateImage(canvasGO, "HandArea", new Color(0.05f, 0.08f, 0.06f, 0.92f));
        SetAnchored(handArea,
            new Vector2(LEFT_X, HAND_Y), new Vector2(RIGHT_X, DECK_Y),
            new Vector2(PAD, PAD), new Vector2(-PAD, -PAD));
        AddOutline(handArea, PANEL_BORDER);

        var handTitle = CreateTMPText(handArea, "HandTitle", "Bài trên Tay", 15, TEXT_GOLD, TextAlignmentOptions.Left);
        SetAnchored(handTitle, new Vector2(0f, 0.88f), new Vector2(0.5f, 1f), new Vector2(10, 0), Vector2.zero);

        var deckCountTxt = CreateTMPText(handArea, "DeckCountText", "Deck: 0", 14, TEXT_DIM, TextAlignmentOptions.Right);
        SetAnchored(deckCountTxt, new Vector2(0.5f, 0.88f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-10, 0));

        var handContainer = new GameObject("HandContainer");
        handContainer.transform.SetParent(handArea.transform, false);
        handContainer.AddComponent<RectTransform>();
        SetAnchored(handContainer, Vector2.zero, new Vector2(1f, 0.87f), new Vector2(8, 4), new Vector2(-8, -4));
        var handHLG = handContainer.AddComponent<HorizontalLayoutGroup>();
        handHLG.spacing            = 8;
        handHLG.childForceExpandWidth  = true;
        handHLG.childForceExpandHeight = true;
        handHLG.childAlignment     = TextAnchor.MiddleCenter;
        handHLG.padding            = new RectOffset(6, 6, 4, 4);

        // ── Player Panels (center, y: 0 → HAND_Y) ────────────────
        var panelContainer = new GameObject("PlayerPanelContainer");
        panelContainer.transform.SetParent(canvasGO.transform, false);
        panelContainer.AddComponent<RectTransform>();
        SetAnchored(panelContainer,
            new Vector2(LEFT_X, 0f), new Vector2(RIGHT_X, HAND_Y),
            new Vector2(PAD, PAD), new Vector2(-PAD, -PAD));

        var hlg = panelContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing            = 6;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment     = TextAnchor.MiddleCenter;
        hlg.padding            = new RectOffset(6, 6, 6, 6);

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

        // ── Card Preview Panel (layout 2 cột: trái = artwork, phải = info + target + nút) ──
        // Dùng Canvas riêng với sortingOrder cao để luôn render trên tất cả UI khác
        var cardPreviewGO = new GameObject("CardPreviewPanel");
        cardPreviewGO.transform.SetParent(canvasGO.transform, false);
        var cpRootRT = cardPreviewGO.AddComponent<RectTransform>();
        cpRootRT.anchorMin = Vector2.zero;
        cpRootRT.anchorMax = Vector2.one;
        cpRootRT.offsetMin = Vector2.zero;
        cpRootRT.offsetMax = Vector2.zero;
        var cpCanvas = cardPreviewGO.AddComponent<Canvas>();
        cpCanvas.overrideSorting = true;
        cpCanvas.sortingOrder    = 50;        // trên tất cả panel khác (sortingOrder 0)
        cardPreviewGO.AddComponent<GraphicRaycaster>();
        var cpBg = cardPreviewGO.AddComponent<Image>();
        cpBg.color = new Color(0f, 0f, 0f, 0.78f);
        cardPreviewGO.SetActive(false);

        // Outer frame — chiếm 70% chiều rộng, 80% chiều cao màn hình
        var cpFrame = CreateImage(cardPreviewGO, "CardPreviewFrame", new Color(0.09f, 0.07f, 0.14f, 1f));
        SetAnchored(cpFrame, new Vector2(0.15f, 0.10f), new Vector2(0.85f, 0.90f), Vector2.zero, Vector2.zero);
        AddOutline(cpFrame, new Color(0.85f, 0.65f, 0.15f, 1f));

        // ── CỘT TRÁI: Artwork chiếm full chiều cao (40% chiều rộng frame) ──
        var cpArtGO = CreateImage(cpFrame, "CPArtwork", new Color(0.05f, 0.04f, 0.08f));
        SetAnchored(cpArtGO, Vector2.zero, new Vector2(0.40f, 1f), Vector2.zero, Vector2.zero);
        cpArtGO.GetComponent<Image>().preserveAspect = false;

        // Artwork image bên trong, giữ tỉ lệ
        var cpArtImg = CreateImage(cpArtGO, "CPArtworkInner", Color.white);
        SetAnchored(cpArtImg, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
        cpArtImg.GetComponent<Image>().preserveAspect = true;

        // ── CỘT PHẢI: info + target + nút (60% chiều rộng còn lại) ──
        var cpRight = new GameObject("CPRight");
        cpRight.transform.SetParent(cpFrame.transform, false);
        cpRight.AddComponent<RectTransform>();
        SetAnchored(cpRight, new Vector2(0.40f, 0f), new Vector2(1f, 1f), new Vector2(10, 8), new Vector2(-10, -8));

        // Type bar (trên cùng cột phải, 8% chiều cao)
        var cpTypeBar = CreateImage(cpRight, "CPTypeBar", new Color(0.2f, 0.4f, 0.9f));
        SetAnchored(cpTypeBar, new Vector2(0f, 0.92f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var cpTypeLabel = CreateTMPText(cpTypeBar, "CPTypeLabel", "Hành động", 13, Color.white, TextAlignmentOptions.Center);
        SetAnchored(cpTypeLabel, Vector2.zero, Vector2.one, new Vector2(4, 0), new Vector2(-4, 0));

        // Tên lá bài (14% chiều cao)
        var cpName = CreateTMPText(cpRight, "CPCardName", "Tên lá bài", 22, TEXT_GOLD, TextAlignmentOptions.Left);
        SetAnchored(cpName, new Vector2(0f, 0.78f), new Vector2(0.75f, 0.92f), new Vector2(4, 2), new Vector2(-4, -2));
        cpName.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        cpName.GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
        cpName.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Ellipsis;

        // Cost badge (góc phải trên)
        var cpCostGO = CreateImage(cpRight, "CPCostBadge", new Color(0.15f, 0.35f, 0.65f));
        SetAnchored(cpCostGO, new Vector2(0.75f, 0.79f), new Vector2(1f, 0.92f), new Vector2(2, 2), new Vector2(-2, -2));
        var cpCost = CreateTMPText(cpCostGO, "CPCostText", "3 ST", 13, Color.white, TextAlignmentOptions.Center);
        SetAnchored(cpCost, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // Mô tả (20% chiều cao)
        var cpDesc = CreateTMPText(cpRight, "CPDescription", "Mô tả hiệu ứng...", 13, TEXT_DIM, TextAlignmentOptions.TopLeft);
        SetAnchored(cpDesc, new Vector2(0f, 0.56f), new Vector2(1f, 0.78f), new Vector2(4, 2), new Vector2(-4, -2));
        cpDesc.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        // Đường kẻ phân cách
        var cpDivider = CreateImage(cpRight, "CPDivider", new Color(0.5f, 0.35f, 0.1f, 0.6f));
        SetAnchored(cpDivider, new Vector2(0f, 0.545f), new Vector2(1f, 0.555f), Vector2.zero, Vector2.zero);

        // Target section (chỉ hiện khi lá cần target) — 44% chiều cao giữa
        var cpTargetSection = new GameObject("CPTargetSection");
        cpTargetSection.transform.SetParent(cpRight.transform, false);
        cpTargetSection.AddComponent<RectTransform>();
        SetAnchored(cpTargetSection, new Vector2(0f, 0.10f), new Vector2(1f, 0.545f), new Vector2(0, 4), new Vector2(0, -4));

        var cpTargetLabel = CreateTMPText(cpTargetSection, "CPTargetLabel", "Chọn mục tiêu:", 12,
            new Color(0.7f, 0.85f, 1f), TextAlignmentOptions.Left);
        SetAnchored(cpTargetLabel, new Vector2(0f, 0.85f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        var cpTargetList = new GameObject("CPTargetList");
        cpTargetList.transform.SetParent(cpTargetSection.transform, false);
        cpTargetList.AddComponent<RectTransform>();
        SetAnchored(cpTargetList, new Vector2(0f, 0f), new Vector2(1f, 0.84f), Vector2.zero, Vector2.zero);
        var cpTargetHLG = cpTargetList.AddComponent<HorizontalLayoutGroup>();
        cpTargetHLG.spacing            = 5;
        cpTargetHLG.childForceExpandWidth  = true;
        cpTargetHLG.childForceExpandHeight = true;
        cpTargetHLG.childAlignment     = TextAnchor.MiddleCenter;
        cpTargetHLG.padding            = new RectOffset(2, 2, 2, 2);

        // 2 nút nhỏ ở cuối cột phải (10% chiều cao)
        var cpConfirm = CreateButton(cpRight, "CPConfirmButton", "✔ Xác nhận", BTN_ENDTURN);
        SetAnchored(cpConfirm, new Vector2(0f, 0f), new Vector2(0.48f, 0.09f), Vector2.zero, Vector2.zero);
        var cpConfirmLabel = cpConfirm.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (cpConfirmLabel != null) cpConfirmLabel.fontSize = 13;

        var cpCancel = CreateButton(cpRight, "CPCancelButton", "✘ Hủy", new Color(0.45f, 0.12f, 0.12f));
        SetAnchored(cpCancel, new Vector2(0.52f, 0f), new Vector2(1f, 0.09f), Vector2.zero, Vector2.zero);
        var cpCancelLabel = cpCancel.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (cpCancelLabel != null) cpCancelLabel.fontSize = 13;

        // ── Protect Confirm Overlay (hỏi human có muốn kích hoạt Bảo vệ không) ──
        var protectConfirmGO = new GameObject("ProtectConfirmPanel");
        protectConfirmGO.transform.SetParent(canvasGO.transform, false);
        var pcRootRT = protectConfirmGO.AddComponent<RectTransform>();
        pcRootRT.anchorMin = Vector2.zero; pcRootRT.anchorMax = Vector2.one;
        pcRootRT.offsetMin = Vector2.zero; pcRootRT.offsetMax = Vector2.zero;
        var pcRootCanvas = protectConfirmGO.AddComponent<Canvas>();
        pcRootCanvas.overrideSorting = true;
        pcRootCanvas.sortingOrder    = 51;
        protectConfirmGO.AddComponent<GraphicRaycaster>();
        var pcRootBg = protectConfirmGO.AddComponent<Image>();
        pcRootBg.color = new Color(0f, 0f, 0f, 0.78f);
        protectConfirmGO.SetActive(false);

        var pcFrame = CreateImage(protectConfirmGO, "ProtectConfirmFrame", new Color(0.10f, 0.07f, 0.20f, 1f));
        SetAnchored(pcFrame, new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.68f), Vector2.zero, Vector2.zero);
        AddOutline(pcFrame, new Color(0.4f, 0.7f, 1f, 1f));

        var pcTitle = CreateTMPText(pcFrame, "ProtectConfirmTitle",
            "🛡 Bảo vệ bí mật", 22, new Color(0.4f, 0.8f, 1f), TextAlignmentOptions.Center);
        SetAnchored(pcTitle, new Vector2(0f, 0.72f), new Vector2(1f, 1f), new Vector2(8, 0), new Vector2(-8, 0));
        pcTitle.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var pcMsg = CreateTMPText(pcFrame, "ProtectConfirmMsg",
            "Bạn đang bị tấn công!\nCó muốn kích hoạt Bảo vệ bí mật để chặn đòn không?",
            16, TEXT_DIM, TextAlignmentOptions.Center);
        SetAnchored(pcMsg, new Vector2(0f, 0.30f), new Vector2(1f, 0.72f), new Vector2(10, 0), new Vector2(-10, 0));
        pcMsg.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        var pcYes = CreateButton(pcFrame, "ProtectYesButton", "✔ Kích hoạt", new Color(0.1f, 0.45f, 0.7f));
        SetAnchored(pcYes, new Vector2(0.08f, 0.05f), new Vector2(0.46f, 0.28f), Vector2.zero, Vector2.zero);

        var pcNo = CreateButton(pcFrame, "ProtectNoButton", "✘ Bỏ qua", new Color(0.4f, 0.15f, 0.15f));
        SetAnchored(pcNo, new Vector2(0.54f, 0.05f), new Vector2(0.92f, 0.28f), Vector2.zero, Vector2.zero);

        // ── Event Notification Overlay (hiện khi bốc trúng lá Event) ──
        var eventNotifGO = new GameObject("EventNotificationPanel");
        eventNotifGO.transform.SetParent(canvasGO.transform, false);
        var enRootRT = eventNotifGO.AddComponent<RectTransform>();
        enRootRT.anchorMin = Vector2.zero; enRootRT.anchorMax = Vector2.one;
        enRootRT.offsetMin = Vector2.zero; enRootRT.offsetMax = Vector2.zero;
        var enRootCanvas = eventNotifGO.AddComponent<Canvas>();
        enRootCanvas.overrideSorting = true;
        enRootCanvas.sortingOrder    = 52;
        eventNotifGO.AddComponent<GraphicRaycaster>();
        var enRootBg = eventNotifGO.AddComponent<Image>();
        enRootBg.color = new Color(0f, 0f, 0f, 0.82f);
        eventNotifGO.SetActive(false);

        // Card frame bên trong (giữa màn hình)
        var evFrame = CreateImage(eventNotifGO, "EventCardFrame", new Color(0.10f, 0.07f, 0.16f, 1f));
        SetAnchored(evFrame, new Vector2(0.30f, 0.22f), new Vector2(0.70f, 0.82f), Vector2.zero, Vector2.zero);
        AddOutline(evFrame, new Color(0.9f, 0.55f, 0.1f, 1f));

        // Nhãn "SỰ KIỆN"
        var evTag = CreateTMPText(evFrame, "EventTag", "⚡ SỰ KIỆN", 18, new Color(0.95f, 0.6f, 0.1f), TextAlignmentOptions.Center);
        SetAnchored(evTag, new Vector2(0f, 0.86f), new Vector2(1f, 1f), new Vector2(6, 0), new Vector2(-6, 0));

        // Ảnh artwork lá bài
        var evArtGO = CreateImage(evFrame, "EventCardArt", Color.white);
        SetAnchored(evArtGO, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
        evArtGO.GetComponent<Image>().preserveAspect = true;

        // Tên lá bài
        var evName = CreateTMPText(evFrame, "EventCardName", "Tên sự kiện", 22, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchored(evName, new Vector2(0f, 0.30f), new Vector2(1f, 0.42f), new Vector2(6, 0), new Vector2(-6, 0));
        evName.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Mô tả
        var evDesc = CreateTMPText(evFrame, "EventCardDesc", "Mô tả hiệu ứng...", 14, TEXT_DIM, TextAlignmentOptions.Center);
        SetAnchored(evDesc, new Vector2(0f, 0f), new Vector2(1f, 0.30f), new Vector2(8, 4), new Vector2(-8, -4));
        evDesc.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        // ── Game Log Panel (right column, x=79%→100%, y: 0→TOP_Y) ──
        var logPanel = CreateImage(canvasGO, "LogPanel", LOG_BG);
        SetAnchored(logPanel,
            new Vector2(RIGHT_X, 0f), new Vector2(1f, TOP_Y),
            new Vector2(PAD, PAD), new Vector2(-PAD, -PAD));
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
            ui.attackButton         = btnAttack.GetComponent<Button>();
            ui.endTurnButton        = btnEnd.GetComponent<Button>();
            ui.activateSecretButton = btnActivate.GetComponent<Button>();
            ui.revealRoleButton     = btnReveal.GetComponent<Button>();
            ui.targetInfoText    = targetInfoTxt.GetComponent<TextMeshProUGUI>();
            // Card system
            ui.drawCardButton    = btnDraw.GetComponent<Button>();
            ui.handContainer     = handContainer.transform;
            ui.deckCountText     = deckCountTxt.GetComponent<TextMeshProUGUI>();
            ui.peekOverlay       = peekOverlay;
            ui.peekContainer     = peekContainer.transform;

            // Card preview panel
            ui.cardPreviewPanel    = cardPreviewGO;
            ui.cpArtworkImage      = cpArtImg.GetComponent<Image>();   // inner image giữ tỉ lệ
            ui.cpCardNameText      = cpName.GetComponent<TextMeshProUGUI>();
            ui.cpDescText          = cpDesc.GetComponent<TextMeshProUGUI>();
            ui.cpCostText          = cpCost.GetComponent<TextMeshProUGUI>();
            ui.cpTypeBar           = cpTypeBar.GetComponent<Image>();
            ui.cpTypeLabel         = cpTypeLabel.GetComponent<TextMeshProUGUI>();
            ui.cpConfirmButton     = cpConfirm.GetComponent<Button>();
            ui.cpCancelButton      = cpCancel.GetComponent<Button>();
            ui.cpTargetSection     = cpTargetSection;
            ui.cpTargetList        = cpTargetList.transform;

            // Protect confirm overlay
            ui.protectConfirmPanel = protectConfirmGO;
            ui.protectYesButton    = pcYes.GetComponent<Button>();
            ui.protectNoButton     = pcNo.GetComponent<Button>();

            // Event notification overlay
            ui.eventNotificationPanel = eventNotifGO;
            ui.eventCardArtImage      = evArtGO.GetComponent<Image>();
            ui.eventCardNameText      = evName.GetComponent<TextMeshProUGUI>();
            ui.eventCardDescText      = evDesc.GetComponent<TextMeshProUGUI>();

            // Load CardUI prefab if exists
            GameObject cardPrefabAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CardUI.prefab");
            if (cardPrefabAsset != null)
                ui.cardUIPrefab = cardPrefabAsset;

            // Wire mặt sau lá bài (general.png)
            Sprite cardBack = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/roles/general.png");
            if (cardBack != null)
                ui.cardBackSprite = cardBack;
            else
                Debug.LogWarning("[SceneBuilder] Không tìm thấy Assets/Sprites/roles/general.png cho cardBackSprite.");

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

        // LayoutElement — dãn đều chiều ngang nhờ childForceExpandWidth trên container
        var le = root.AddComponent<LayoutElement>();
        le.flexibleWidth   = 1f;   // mỗi panel chiếm phần đều nhau
        le.minWidth        = 80;
        le.minHeight       = 120;

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
