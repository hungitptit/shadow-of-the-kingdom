#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CardAssetSetup
{
    const string CardFolder = "Assets/Resources/Cards";

    // ── Card definitions (name, type, effect, cost, copies, sprite path) ──────

    static readonly (string name, CardType type, CardEffectType fx, int cost, int copies, string sprite, string desc)[]
    CardDefs = new[]
    {
        // ── Items (10 bản mỗi loại) ──────────────────────────────
        ("Áo giáp",  CardType.Item, CardEffectType.ItemArmor,  0, 10,
         "Assets/Sprites/item/aogiap - Copy (2).png",
         "Tăng 1 điểm Phòng thủ vĩnh viễn."),
        ("Binh khí", CardType.Item, CardEffectType.ItemWeapon, 0, 10,
         "Assets/Sprites/item/binhkhi - Copy (2).png",
         "Tăng 1 điểm Tấn công vĩnh viễn."),
        ("Thuốc bổ", CardType.Item, CardEffectType.ItemPotion, 0, 10,
         "Assets/Sprites/item/thuoc - Copy (2).png",
         "Tăng 1 điểm Thể lực tối đa."),

        // ── Actions ──────────────────────────────────────────────
        ("Ăn xin",                 CardType.Action, CardEffectType.ActionBeg,           1, 3,
         "Assets/Sprites/actions/anxin.png",
         "Xin mỗi người chơi 1 lá bài, họ tự chọn lá bỏ thí."),
        ("Cải tử hoàn sinh",       CardType.Action, CardEffectType.ActionRevive,         2, 1,
         "Assets/Sprites/actions/caituhoansinh.png",
         "Hủy toàn bộ lá bài đang sở hữu để hồi sinh 1 người chơi đã bị loại."),
        ("Chạy giặc",              CardType.Action, CardEffectType.ActionFlee,           1, 1,
         "Assets/Sprites/actions/chaygiac.png",
         "Miễn nhiễm sự kiện Giặc ngoại xâm vòng này chỉ cho bản thân."),
        ("Đánh đuổi ngoại xâm",   CardType.Action, CardEffectType.ActionRepelInvasion,  1, 2,
         "Assets/Sprites/actions/danhduoingoaixam - Copy.png",
         "Khi có sự kiện Giặc ngoại xâm, không ai bị mất lá bài nào. Riêng bạn được thưởng thêm 2 lá bài."),
        ("Ăn trộm",                CardType.Action, CardEffectType.ActionSteal,          1, 5,
         "Assets/Sprites/actions/trom - Copy (2).png",
         "Lén lút đột nhập, lấy 1 lá bài ngẫu nhiên của người chơi khác."),
        ("Thuốc hồi phục",         CardType.Action, CardEffectType.ActionHeal,           1, 6,
         "Assets/Sprites/actions/thuochoiphuc - Copy - Copy.png",
         "Hồi phục 1 điểm Khí huyết."),
        ("Thuốc độc",              CardType.Action, CardEffectType.ActionPoison,         2, 3,
         "Assets/Sprites/actions/thuocdoc - Copy (2).png",
         "Chọn 1 người chơi để hạ độc; người đó giảm 1 Khí huyết mỗi vòng trong 3 vòng."),
        ("Thuốc đảo lộn",          CardType.Action, CardEffectType.ActionSwapStats,      1, 3,
         "Assets/Sprites/actions/thuocdaolon - Copy (2).png",
         "Chọn 1 người chơi, đảo chỗ giá trị Tấn công và Phòng thủ của người đó."),
        ("Thầy bùa",               CardType.Action, CardEffectType.ActionExorcism,       1, 5,
         "Assets/Sprites/actions/thaybua - Copy (2).png",
         "Diệt trừ Oán linh hoặc chuyển Oán linh sang người chơi khác."),
        ("Thầy bói",               CardType.Action, CardEffectType.ActionFortune,        1, 5,
         "Assets/Sprites/actions/thayboi - Copy (2).png",
         "Xem trước 3 lá bài trên đỉnh deck."),
        ("Phản đòn",               CardType.Action, CardEffectType.ActionCounter,        1, 5,
         "Assets/Sprites/actions/phandon - Copy (2).png",
         "Phản lại đòn tấn công tiếp theo nhận vào trong vòng này."),
        ("Oán linh",               CardType.Action, CardEffectType.ActionCurse,          1, 2,
         "Assets/Sprites/actions/oanlinh - Copy.png",
         "Ám 1 người chơi; Thể lực tối đa của người đó bị khóa ở 2."),
        ("Cướp vũ khí",            CardType.Action, CardEffectType.ActionStealWeapon,    1, 2,
         "Assets/Sprites/actions/cuopbinhkhi - Copy (2).png",
         "Cướp Binh khí của 1 người chơi khác."),
        ("Cướp áo giáp",           CardType.Action, CardEffectType.ActionStealArmor,     1, 2,
         "Assets/Sprites/actions/cuopaogiap - Copy (2).png",
         "Cướp Áo giáp của 1 người chơi khác."),

        // ── Events (3 bản mỗi loại) ──────────────────────────────
        ("Hạn hán",                CardType.Event, CardEffectType.EventDrought,    0, 3,
         "Assets/Sprites/events/hanhan.png",
         "Hạn hán gây mất mùa — -3 Thể lực hiện tại với tất cả người chơi."),
        ("Giặc ngoại xâm",         CardType.Event, CardEffectType.EventInvasion,   0, 3,
         "Assets/Sprites/events/giac - Copy (2).png",
         "Giặc kéo đến cướp phá, mỗi người chơi phải hủy 2 lá bài bất kỳ."),
        ("Góp gạo thổi cơm chung", CardType.Event, CardEffectType.EventShareRice,  0, 3,
         "Assets/Sprites/events/gopgao.png",
         "Cả làng cùng góp gạo, xáo lại và chia đều cho mọi người."),
        ("Cô Thương",              CardType.Event, CardEffectType.EventGoddess,    0, 3,
         "Assets/Sprites/events/cothuong - Copy (2).png",
         "Thánh mẫu phù hộ — người rút lá này hồi phục toàn bộ Khí huyết và bốc thêm 1 lá."),

        // ── Secret cards (5 bản mỗi loại) ───────────────────────
        ("Ám sát", CardType.HiddenAction, CardEffectType.HiddenAssassinate, 0, 5,
         "Assets/Sprites/secret/amsat - Copy (2).png",
         "Đặt bí mật lên 1 người chơi. Kích hoạt vòng sau tốn 5 Thể lực để hạ gục ngay lập tức."),
        ("Bảo vệ", CardType.HiddenAction, CardEffectType.HiddenProtect,     0, 5,
         "Assets/Sprites/secret/baove - Copy.png",
         "Đặt bí mật lên 1 người chơi. Người đó miễn nhiễm mọi đòn tấn công và ám sát cho đến khi lá bị kích hoạt."),
    };

    // ── Menu items ────────────────────────────────────────────────────────────

    [MenuItem("Game/Setup/Create All Card Assets")]
    public static void CreateAllCardAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(CardFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Cards");

        int created = 0;
        foreach (var def in CardDefs)
        {
            string path = $"{CardFolder}/{def.name}.asset";

            CardData existing = AssetDatabase.LoadAssetAtPath<CardData>(path);
            CardData card = existing ?? ScriptableObject.CreateInstance<CardData>();

            card.cardName    = def.name;
            card.cardType    = def.type;
            card.effectType  = def.fx;
            card.staminaCost = def.cost;
            card.count       = def.copies;
            card.description = def.desc;

            // Load sprite
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(def.sprite);
            if (sprite == null)
            {
                // Try loading the texture and marking as Sprite
                TextureImporter ti = AssetImporter.GetAtPath(def.sprite) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.Sprite)
                {
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    AssetDatabase.ImportAsset(def.sprite);
                }
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(def.sprite);
            }
            card.artwork = sprite;

            if (existing == null)
            {
                AssetDatabase.CreateAsset(card, path);
                created++;
            }
            else
            {
                EditorUtility.SetDirty(card);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CardAssetSetup] Done. Created/updated {CardDefs.Length} CardData assets.");
    }

    [MenuItem("Game/Setup/Create Card UI Prefab")]
    public static void CreateCardUIPrefab()
    {
        // Create prefab folder
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        const string prefabPath = "Assets/Prefabs/CardUI.prefab";

        // Card size: 120×180 (2:3 ratio — standard playing card)
        const float W = 120f, H = 180f;

        // ── Root ──────────────────────────────────────────────────
        GameObject root = new GameObject("CardUI");
        RectTransform rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(W, H);

        // Card background
        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.13f, 0.10f, 0.07f);
        Button btn = root.AddComponent<Button>();

        // LayoutElement so HorizontalLayoutGroup respects fixed size
        LayoutElement le = root.AddComponent<LayoutElement>();
        le.preferredWidth  = W;
        le.preferredHeight = H;
        le.minWidth  = W * 0.7f;
        le.minHeight = H * 0.7f;

        // Glow outline (hidden by default, shown on hover)
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform, false);
        RectTransform glowRT = glow.AddComponent<RectTransform>();
        glowRT.anchorMin = Vector2.zero;
        glowRT.anchorMax = Vector2.one;
        glowRT.offsetMin = new Vector2(-4, -4);
        glowRT.offsetMax = new Vector2(4, 4);
        Image glowImg = glow.AddComponent<Image>();
        glowImg.color = new Color(1f, 0.85f, 0.1f, 0.7f);
        glow.SetActive(false);

        // ── Type color bar (top 10%) ───────────────────────────────
        // Anchored: y 90%→100%
        GameObject bar = new GameObject("TypeBar");
        bar.transform.SetParent(root.transform, false);
        RectTransform barRT = bar.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0, 0.90f);
        barRT.anchorMax = new Vector2(1, 1f);
        barRT.offsetMin = barRT.offsetMax = Vector2.zero;
        bar.AddComponent<Image>().color = new Color(0.2f, 0.4f, 0.9f);

        // Card name inside bar (left-aligned)
        GameObject nameObj = new GameObject("CardName");
        nameObj.transform.SetParent(bar.transform, false);
        RectTransform nameRT = nameObj.AddComponent<RectTransform>();
        nameRT.anchorMin = Vector2.zero;
        nameRT.anchorMax = new Vector2(0.78f, 1f);
        nameRT.offsetMin = new Vector2(5, 1);
        nameRT.offsetMax = new Vector2(0, -1);
        TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.fontSize = 9f;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.text = "Tên bài";
        nameTmp.color = Color.white;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.enableWordWrapping = false;

        // ── Artwork (top 58%, below bar) ───────────────────────────
        // Anchored: y 35%→89%
        GameObject art = new GameObject("Artwork");
        art.transform.SetParent(root.transform, false);
        RectTransform artRT = art.AddComponent<RectTransform>();
        artRT.anchorMin = new Vector2(0.03f, 0.35f);
        artRT.anchorMax = new Vector2(0.97f, 0.89f);
        artRT.offsetMin = artRT.offsetMax = Vector2.zero;
        Image artImg = art.AddComponent<Image>();
        artImg.color = Color.white;
        artImg.preserveAspect = true;

        // ── Description (middle band, y 12%→34%) ──────────────────
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(root.transform, false);
        RectTransform descRT = descObj.AddComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0.12f);
        descRT.anchorMax = new Vector2(1, 0.34f);
        descRT.offsetMin = new Vector2(5, 2);
        descRT.offsetMax = new Vector2(-5, -2);
        TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.fontSize = 6.5f;
        descTmp.text = "Mô tả...";
        descTmp.color = new Color(0.88f, 0.88f, 0.88f);
        descTmp.overflowMode = TextOverflowModes.Ellipsis;
        descTmp.enableWordWrapping = true;

        // ── Stamina cost badge (bottom-right, y 0→12%) ────────────
        GameObject costObj = new GameObject("Cost");
        costObj.transform.SetParent(root.transform, false);
        RectTransform costRT = costObj.AddComponent<RectTransform>();
        costRT.anchorMin = new Vector2(0.62f, 0f);
        costRT.anchorMax = new Vector2(1f, 0.13f);
        costRT.offsetMin = new Vector2(2, 2);
        costRT.offsetMax = new Vector2(-2, -2);
        costObj.AddComponent<Image>().color = new Color(0.75f, 0.15f, 0.08f);

        GameObject costTextObj = new GameObject("CostText");
        costTextObj.transform.SetParent(costObj.transform, false);
        RectTransform ctRT = costTextObj.AddComponent<RectTransform>();
        ctRT.anchorMin = Vector2.zero;
        ctRT.anchorMax = Vector2.one;
        ctRT.offsetMin = ctRT.offsetMax = Vector2.zero;
        TextMeshProUGUI costTmp = costTextObj.AddComponent<TextMeshProUGUI>();
        costTmp.fontSize = 11f;
        costTmp.fontStyle = FontStyles.Bold;
        costTmp.text = "1";
        costTmp.color = Color.white;
        costTmp.alignment = TextAlignmentOptions.Center;

        // ── Card type label (bottom-left, y 0→12%) ────────────────
        GameObject typeObj = new GameObject("TypeLabel");
        typeObj.transform.SetParent(root.transform, false);
        RectTransform typeRT = typeObj.AddComponent<RectTransform>();
        typeRT.anchorMin = new Vector2(0f, 0f);
        typeRT.anchorMax = new Vector2(0.6f, 0.12f);
        typeRT.offsetMin = new Vector2(4, 2);
        typeRT.offsetMax = new Vector2(0, -2);
        TextMeshProUGUI typeTmp = typeObj.AddComponent<TextMeshProUGUI>();
        typeTmp.fontSize = 7f;
        typeTmp.text = "Hành động";
        typeTmp.color = new Color(0.7f, 0.7f, 0.7f);
        typeTmp.enableWordWrapping = false;

        // ── Wire CardUI component ─────────────────────────────────
        CardUI cardUI = root.AddComponent<CardUI>();
        cardUI.artworkImage    = artImg;
        cardUI.cardNameText    = nameTmp;
        cardUI.descriptionText = descTmp;
        cardUI.costText        = costTmp;
        cardUI.typeColorBar    = bar.GetComponent<Image>();
        cardUI.selectedGlow    = glow;
        cardUI.cardBackground  = bg;

        // ── Save prefab ───────────────────────────────────────────
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        if (prefab != null)
            Debug.Log($"[CardAssetSetup] CardUI prefab saved at {prefabPath}");
        else
            Debug.LogError("[CardAssetSetup] Failed to save CardUI prefab.");

        AssetDatabase.Refresh();
    }

    [MenuItem("Game/Setup/Auto-Assign Cards to DeckManager")]
    public static void AutoAssignCardsToDeckManager()
    {
        DeckManager dm = Object.FindFirstObjectByType<DeckManager>();
        if (dm == null)
        {
            // Create DeckManager if not present in scene
            var dmGO = new GameObject("DeckManager");
            dm = dmGO.AddComponent<DeckManager>();
            Debug.Log("[CardAssetSetup] DeckManager not found — created one in scene.");

            // Also wire it to GameManager if present
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.deckManager = dm;
                EditorUtility.SetDirty(gm);
            }
        }

        string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { CardFolder });
        dm.allCards = new System.Collections.Generic.List<CardData>();
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            dm.allCards.Add(AssetDatabase.LoadAssetAtPath<CardData>(p));
        }

        // Assign card back sprite placeholder
        Sprite back = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/roles/role.png");
        dm.cardBackSprite = back;

        EditorUtility.SetDirty(dm);
        AssetDatabase.SaveAssets();

        // Mark scene dirty so the new DeckManager object is saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[CardAssetSetup] Assigned {dm.allCards.Count} cards to DeckManager. Scene saved.");
    }
}
#endif
