using UnityEngine;

public enum CardType
{
    Item,       // Vật phẩm — đặt ngửa, hiệu ứng bị động vĩnh viễn
    Action,     // Hành động — kích hoạt ngay, tốn stamina
    HiddenAction, // Hành động ẩn — đặt úp, kích hoạt sau
    Event       // Sự kiện — ảnh hưởng toàn bộ người chơi
}

public enum CardEffectType
{
    // ── Items ──────────────────────────────────────
    ItemArmor,          // Áo giáp: +1 DEF
    ItemWeapon,         // Binh khí: +1 ATK
    ItemPotion,         // Thuốc bổ: +1 max stamina

    // ── Actions ────────────────────────────────────
    ActionBeg,          // Ăn xin: xin mỗi người 1 lá
    ActionRevive,       // Cải tử hoàn sinh: hủy tay bài, hồi sinh 1 người
    ActionFlee,         // Chạy giặc: miễn nhiễm sự kiện Giặc ngoại xâm
    ActionSteal,        // Ăn trộm: lấy 1 lá ngẫu nhiên từ người khác
    ActionHeal,         // Thuốc hồi phục: +1 HP
    ActionPoison,       // Thuốc độc: mục tiêu -1 HP/vòng trong 3 vòng
    ActionSwapStats,    // Thuốc đảo lộn: đảo ATK↔DEF của mục tiêu
    ActionExorcism,     // Thầy bùa: diệt/chuyển Oán linh
    ActionFortune,      // Thầy bói: xem trước 3 lá trên deck
    ActionCounter,      // Phản đòn: phản lại đòn tấn công tiếp theo
    ActionCurse,        // Oán linh: khóa max stamina mục tiêu ở 2
    ActionStealWeapon,  // Cướp vũ khí: cướp Binh khí
    ActionStealArmor,   // Cướp áo giáp: cướp Áo giáp

    // ── Events ─────────────────────────────────────
    EventDrought,       // Hạn hán: -3 stamina tất cả
    EventInvasion,      // Giặc ngoại xâm: mỗi người hủy 2 lá
    EventShareRice,     // Góp gạo thổi cơm chung: góp + xáo + chia đều
    EventGoddess,       // Cô Thương: người rút hồi HP + bốc thêm 1 lá

    // ── Hidden Actions ─────────────────────────────
    HiddenAssassinate,  // Ám sát: hạ mục tiêu ngay
    HiddenProtect,      // Bảo vệ: vô hiệu hóa toàn bộ sát thương
}

[CreateAssetMenu(menuName = "Game/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public CardEffectType effectType;
    public Sprite artwork;
    [TextArea(2, 4)]
    public string description;
    public int staminaCost;     // stamina cần để chơi (Item = 0)
    public int count = 2;       // số bản copy trong deck
}
