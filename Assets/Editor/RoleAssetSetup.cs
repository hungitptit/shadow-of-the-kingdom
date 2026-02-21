using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class RoleAssetSetup
{
    private const string RoleSavePath = "Assets/ScriptableObjects/Roles";

    [MenuItem("Game/Setup/Create All Role Assets")]
    public static void CreateAllRoles()
    {
        if (!AssetDatabase.IsValidFolder(RoleSavePath))
        {
            Directory.CreateDirectory(Application.dataPath + "/ScriptableObjects/Roles");
            AssetDatabase.Refresh();
        }

        CreateRole(RoleType.Emperor, Faction.Emperor, "vua",
            "Hoàng đế. Nếu bị hạ → phe Phản thần thắng ngay.");
        CreateRole(RoleType.Queen, Faction.Emperor, "hoanghau",
            "Khi lật Vai trò → Hoàng đế hồi 1 Khí huyết.");
        CreateRole(RoleType.Guard, Faction.Emperor, "camquan",
            "Khi Thích khách bị hạ và đã công khai → ngăn hiệu ứng. 1 lần/ván.");
        CreateRole(RoleType.Judge, Faction.Emperor, "quanan",
            "Sau vòng 6, nếu còn sống: lật bài, chọn 2 người khác phải lật ngay.");
        CreateRole(RoleType.Rebel, Faction.Rebel, "phanthan",
            "Chiến thắng khi Hoàng đế bị hạ.");
        CreateRole(RoleType.Assassin, Faction.Rebel, "thichkhach",
            "Khi bị hạ → Hoàng đế mất 1 Khí huyết (nếu không có Cấm quân can).");
        CreateRole(RoleType.Farmer, Faction.Neutral, "nongdan",
            "Chiến thắng khi một phe chính thắng và Nông dân còn sống.");
        CreateRole(RoleType.RedDevil, Faction.Third, "Quydo",
            "Miễn nhiễm đòn đầu tiên mỗi vòng sau khi công khai. Thắng khi ≤2 người còn sống.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RoleAssetSetup] Tất cả Role assets đã được tạo tại " + RoleSavePath);
    }

    static void CreateRole(RoleType roleType, Faction faction, string spriteName, string description)
    {
        string assetPath = $"{RoleSavePath}/{roleType}.asset";

        // Skip if already exists
        RoleData existing = AssetDatabase.LoadAssetAtPath<RoleData>(assetPath);
        if (existing != null)
        {
            existing.faction = faction;
            existing.description = description;

            Sprite sprite = TryLoadSprite(spriteName);
            if (sprite != null) existing.cardImage = sprite;

            EditorUtility.SetDirty(existing);
            Debug.Log($"[RoleAssetSetup] Updated: {roleType}");
            return;
        }

        RoleData role = ScriptableObject.CreateInstance<RoleData>();
        role.roleType = roleType;
        role.faction = faction;
        role.description = description;

        Sprite cardSprite = TryLoadSprite(spriteName);
        if (cardSprite != null)
            role.cardImage = cardSprite;
        else
            Debug.LogWarning($"[RoleAssetSetup] Sprite '{spriteName}' not found for {roleType}");

        AssetDatabase.CreateAsset(role, assetPath);
        Debug.Log($"[RoleAssetSetup] Created: {assetPath}");
    }

    static Sprite TryLoadSprite(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"{name} t:Sprite", new[] { "Assets/Sprites/roles" });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    [MenuItem("Game/Setup/Auto-Assign Roles to GameManager")]
    public static void AutoAssignRolesToGameManager()
    {
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            // GameManager not in scene yet — create a host GameObject for it
            var gmGO = new GameObject("GameManager");
            gm = gmGO.AddComponent<GameManager>();

            // Also add UIManager on the same object if missing
            if (Object.FindFirstObjectByType<UIManager>() == null)
                gmGO.AddComponent<UIManager>();

            Debug.Log("[RoleAssetSetup] GameManager not found — created one in scene.");
        }

        if (gm.allRoles == null)
            gm.allRoles = new System.Collections.Generic.List<RoleData>();
        gm.allRoles.Clear();

        string[] guids = AssetDatabase.FindAssets("t:RoleData", new[] { RoleSavePath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RoleData rd = AssetDatabase.LoadAssetAtPath<RoleData>(path);
            if (rd != null)
                gm.allRoles.Add(rd);
        }

        EditorUtility.SetDirty(gm);

        // Save scene so the new objects persist
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log($"[RoleAssetSetup] Assigned {gm.allRoles.Count} roles to GameManager. Scene saved.");
    }
}
