using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Builds the Main Menu scene.
/// Run via: Game > Setup > Build Main Menu Scene
/// </summary>
public static class MainMenuBuilder
{
    // ── Colors ────────────────────────────────────────────────────
    static readonly Color BG_DARK      = new Color(0.06f, 0.04f, 0.10f, 1f);
    static readonly Color PANEL_BG     = new Color(0.10f, 0.08f, 0.16f, 0.97f);
    static readonly Color PANEL_BORDER = new Color(0.5f, 0.35f, 0.1f, 1f);
    static readonly Color BTN_SP       = new Color(0.15f, 0.45f, 0.15f, 1f);  // single player green
    static readonly Color BTN_MP       = new Color(0.15f, 0.25f, 0.55f, 1f);  // multi player blue
    static readonly Color BTN_START    = new Color(0.6f, 0.40f, 0.05f, 1f);   // gold start
    static readonly Color BTN_COUNT    = new Color(0.25f, 0.20f, 0.35f, 1f);
    static readonly Color BTN_QUIT     = new Color(0.45f, 0.10f, 0.10f, 1f);  // quit red
    static readonly Color TEXT_GOLD    = new Color(0.95f, 0.80f, 0.30f, 1f);
    static readonly Color TEXT_WHITE   = Color.white;
    static readonly Color TEXT_DIM     = new Color(0.7f, 0.7f, 0.7f, 1f);

    [MenuItem("Game/Setup/Build Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        // Create or open MainMenu scene
        string scenePath = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BG_DARK;
        cam.orthographic = true;
        camGO.AddComponent<AudioListener>();
        camGO.tag = "MainCamera";

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── Canvas ────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background
        var bg = CreateImg(canvasGO, "Background", BG_DARK);
        Stretch(bg);

        // ── Center card ───────────────────────────────────────────
        var card = CreateImg(canvasGO, "MenuCard", PANEL_BG);
        SetAnchor(card, new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.92f));
        AddOutline(card, PANEL_BORDER);

        // Title
        var title = CreateTMP(card, "Title", "BÓNG TỐI TRIỀU ĐÌNH", 42, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchor(title, new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f));

        var subtitle = CreateTMP(card, "Subtitle", "Trò chơi chiến thuật ẩn vai", 20, TEXT_DIM, TextAlignmentOptions.Center);
        SetAnchor(subtitle, new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.83f));

        // Divider
        var div = CreateImg(card, "Divider", PANEL_BORDER);
        SetAnchor(div, new Vector2(0.08f, 0.725f), new Vector2(0.92f, 0.73f));

        // ── Mode label ────────────────────────────────────────────
        var modeLabel = CreateTMP(card, "ModeLabel", "Chế độ chơi", 22, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchor(modeLabel, new Vector2(0.05f, 0.63f), new Vector2(0.95f, 0.71f));

        // Single player button
        var btnSP = CreateButton(card, "BtnSinglePlayer", "👤  Người chơi đơn  (vs AI)", BTN_SP, 22);
        SetAnchor(btnSP, new Vector2(0.08f, 0.51f), new Vector2(0.92f, 0.62f));

        // Multiplayer button
        var btnMP = CreateButton(card, "BtnMultiPlayer", "👥  Nhiều người chơi", BTN_MP, 22);
        SetAnchor(btnMP, new Vector2(0.08f, 0.39f), new Vector2(0.92f, 0.50f));

        // ── Player count ──────────────────────────────────────────
        var countLabel = CreateTMP(card, "CountSectionLabel", "Số người chơi", 22, TEXT_GOLD, TextAlignmentOptions.Center);
        SetAnchor(countLabel, new Vector2(0.05f, 0.29f), new Vector2(0.95f, 0.37f));

        // Minus button
        var btnMinus = CreateButton(card, "BtnMinus", "−", BTN_COUNT, 28);
        SetAnchor(btnMinus, new Vector2(0.15f, 0.18f), new Vector2(0.35f, 0.28f));

        // Count display
        var countDisplay = CreateTMP(card, "PlayerCountLabel", "4", 34, TEXT_WHITE, TextAlignmentOptions.Center);
        SetAnchor(countDisplay, new Vector2(0.38f, 0.18f), new Vector2(0.62f, 0.28f));

        // Plus button
        var btnPlus = CreateButton(card, "BtnPlus", "+", BTN_COUNT, 28);
        SetAnchor(btnPlus, new Vector2(0.65f, 0.18f), new Vector2(0.85f, 0.28f));

        // Count hint text
        var countHint = CreateTMP(card, "CountHint", "4 – 9 người chơi", 15, TEXT_DIM, TextAlignmentOptions.Center);
        SetAnchor(countHint, new Vector2(0.1f, 0.11f), new Vector2(0.9f, 0.18f));

        // ── Start button ──────────────────────────────────────────
        var btnStart = CreateButton(card, "BtnStart", "▶   BẮT ĐẦU VÁN CHƠI", BTN_START, 26);
        SetAnchor(btnStart, new Vector2(0.10f, 0.07f), new Vector2(0.90f, 0.15f));
        btnStart.GetComponent<Image>().color = BTN_START;

        // ── Quit button ───────────────────────────────────────────
        var btnQuit = CreateButton(card, "BtnQuit", "✕   THOÁT GAME", BTN_QUIT, 20);
        SetAnchor(btnQuit, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.06f));

        // ── Wire MainMenuManager ──────────────────────────────────
        var mmGO = new GameObject("MainMenuManager");
        var mm = mmGO.AddComponent<MainMenuManager>();

        mm.btnSinglePlayer   = btnSP.GetComponent<Button>();
        mm.btnMultiPlayer    = btnMP.GetComponent<Button>();
        mm.playerCountLabel  = countDisplay.GetComponent<TextMeshProUGUI>();
        mm.btnCountMinus     = btnMinus.GetComponent<Button>();
        mm.btnCountPlus      = btnPlus.GetComponent<Button>();
        mm.btnStart          = btnStart.GetComponent<Button>();
        mm.btnQuit           = btnQuit.GetComponent<Button>();
        mm.singlePlayerHighlight = btnSP.GetComponent<Image>();
        mm.multiPlayerHighlight  = btnMP.GetComponent<Image>();

        EditorUtility.SetDirty(mm);

        // Save scene
        EditorSceneManager.SaveScene(scene, scenePath);

        // Fix build settings order: MainMenu=0, GameScene=1
        FixBuildSettings();

        Debug.Log("[MainMenuBuilder] MainMenu scene created at " + scenePath + ". Build Settings: MainMenu=0, GameScene=1.");
    }

    // Ensures MainMenu is index 0 and GameScene is index 1 in Build Settings
    [MenuItem("Game/Setup/Fix Build Settings")]
    public static void FixBuildSettings()
    {
        const string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        const string gameScenePath = "Assets/Scenes/GameScene.unity";

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(mainMenuPath,  true),
            new EditorBuildSettingsScene(gameScenePath, true),
        };

        Debug.Log("[MainMenuBuilder] Build Settings fixed: MainMenu=0, GameScene=1.");
    }

    static void AddSceneToBuild(string path)
    {
        // Not used directly anymore — FixBuildSettings handles ordering
        // Kept for safety: ensure the scene exists in the list
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        foreach (var s in scenes)
            if (s.path == path) return;   // already present, do nothing

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ── Helpers ───────────────────────────────────────────────────

    static GameObject CreateImg(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    static GameObject CreateTMP(GameObject parent, string name, string text,
        int size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        return go;
    }

    static GameObject CreateButton(GameObject parent, string name, string label,
        Color bgColor, int fontSize = 18)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = bgColor;
        cb.highlightedColor = bgColor * 1.25f;
        cb.pressedColor = bgColor * 0.7f;
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = cb;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1, 1, 1, 0.25f);
        outline.effectDistance = new Vector2(1, -1);

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform, false);
        var lrt = lbl.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        return go;
    }

    static void AddOutline(GameObject go, Color color)
    {
        var o = go.AddComponent<Outline>();
        o.effectColor = color;
        o.effectDistance = new Vector2(2, -2);
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetAnchor(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
