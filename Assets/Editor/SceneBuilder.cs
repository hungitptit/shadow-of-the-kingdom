using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

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

        // Layout constants (all in anchor ratios, no mixed pixel offsets for panels)
        // Left column:  x 0.00 → 0.15
        // Center area:  x 0.15 → 0.79
        // Right column: x 0.79 → 1.00
        // Top bar:      y 0.94 → 1.00  (≈60px at 1080p)
        // Button panel: y 0.00 → 0.37
        // Info panel:   y 0.37 → 0.94

        // ── Top Bar ──────────────────────────────────────────────
        var topBar = CreateImage(canvasGO, "TopBar", PANEL_BG);
        SetAnchored(topBar,
            new Vector2(0f, 0.94f), new Vector2(1f, 1f),
            new Vector2(0, 0), new Vector2(0, 0));

        var titleTxt = CreateTMPText(topBar, "TitleText", "BÓNG TỐI TRIỀU ĐÌNH", 26, TEXT_GOLD, TextAlignmentOptions.Left);
        SetAnchored(titleTxt, new Vector2(0, 0), new Vector2(0.45f, 1), new Vector2(20, 0), new Vector2(0, 0));

        var roundTxt = CreateTMPText(topBar, "RoundText", "Vòng 1", 24, TEXT_WHITE, TextAlignmentOptions.Center);
        SetAnchored(roundTxt, new Vector2(0.35f, 0), new Vector2(0.65f, 1), Vector2.zero, Vector2.zero);

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

        // ── Player Panels Area (center bottom, x=15%→79%, y=0→37%) ──
        var panelContainer = new GameObject("PlayerPanelContainer");
        panelContainer.transform.SetParent(canvasGO.transform, false);
        panelContainer.AddComponent<RectTransform>();
        SetAnchored(panelContainer,
            new Vector2(0.15f, 0f), new Vector2(0.79f, 0.37f),
            new Vector2(4, 4), new Vector2(-4, -4));

        var hlg = panelContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(8, 8, 8, 8);

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
        SetAnchored(goRestartBtn, new Vector2(0.35f, 0.28f), new Vector2(0.65f, 0.38f), Vector2.zero, Vector2.zero);
        goRestartBtn.AddComponent<RestartButton>();

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
            EditorUtility.SetDirty(ui);
        }

        if (gm != null)
        {
            gm.playerPanelContainer = panelContainer.transform;
            EditorUtility.SetDirty(gm);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneBuilder] UI built and scene saved. Now run 'Game > Setup > Create All Role Assets' then 'Auto-Assign Roles to GameManager'.");
    }

    // ── Helper: Create PlayerPanel Prefab ────────────────────────
    [MenuItem("Game/Setup/Create PlayerPanel Prefab")]
    public static void CreatePlayerPanelPrefab()
    {
        string prefabPath = "Assets/Prefabs/PlayerPanel.prefab";

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

        // Name label
        var nameTxt = CreateTMPText(root, "PlayerName", "Player 1", 20, Color.white, TextAlignmentOptions.Center);
        SetAnchored(nameTxt, new Vector2(0, 0.72f), new Vector2(1, 1), new Vector2(4, 0), new Vector2(-4, -4));
        ppUI.nameText = nameTxt.GetComponent<TextMeshProUGUI>();

        // Role thumbnail area
        var roleThumb = CreateImage(root, "RoleCardImage", Color.white);
        SetAnchored(roleThumb, new Vector2(0.02f, 0.32f), new Vector2(0.42f, 0.72f), Vector2.zero, Vector2.zero);
        roleThumb.GetComponent<Image>().preserveAspect = true;
        roleThumb.SetActive(false);
        ppUI.roleCardImage = roleThumb.GetComponent<Image>();

        // Role name
        var roleNameTxt = CreateTMPText(root, "RoleNameText", "???", 15, new Color(0.95f, 0.8f, 0.3f), TextAlignmentOptions.Center);
        SetAnchored(roleNameTxt, new Vector2(0.44f, 0.55f), new Vector2(0.98f, 0.72f), Vector2.zero, Vector2.zero);
        ppUI.roleNameText = roleNameTxt.GetComponent<TextMeshProUGUI>();

        // HP
        var hpT = CreateTMPText(root, "HpText", "HP 4", 17, new Color(1, 0.3f, 0.3f), TextAlignmentOptions.Left);
        SetAnchored(hpT, new Vector2(0.02f, 0.42f), new Vector2(0.5f, 0.56f), Vector2.zero, Vector2.zero);
        ppUI.hpText = hpT.GetComponent<TextMeshProUGUI>();

        // Stamina
        var stT = CreateTMPText(root, "StaminaText", "ST 4", 17, new Color(0.3f, 0.8f, 1f), TextAlignmentOptions.Left);
        SetAnchored(stT, new Vector2(0.5f, 0.42f), new Vector2(0.98f, 0.56f), Vector2.zero, Vector2.zero);
        ppUI.staminaText = stT.GetComponent<TextMeshProUGUI>();

        // ATK/DEF
        var adT = CreateTMPText(root, "AtkDefText", "ATK 4 / DEF 3", 14, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
        SetAnchored(adT, new Vector2(0.02f, 0.28f), new Vector2(0.98f, 0.42f), Vector2.zero, Vector2.zero);
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

        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        // Assign to GameManager
        GameManager gm = Object.FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.playerPanelPrefab = prefab;
            EditorUtility.SetDirty(gm);
        }

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
