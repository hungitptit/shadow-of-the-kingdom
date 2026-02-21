using UnityEngine;

public enum RoleType
{
    Emperor,    // Hoàng đế
    Queen,      // Hoàng hậu
    Guard,      // Cấm quân
    Judge,      // Quan án
    Rebel,      // Phản thần
    Assassin,   // Thích khách
    Farmer,     // Nông dân
    RedDevil    // Quỷ đỏ
}

public enum Faction
{
    Emperor,  // Phe Hoàng đế
    Rebel,    // Phe Phản thần
    Neutral,  // Phe Trung lập (Nông dân)
    Third     // Phe Thứ ba (Quỷ đỏ)
}

[CreateAssetMenu(menuName = "Game/Role")]
public class RoleData : ScriptableObject
{
    public RoleType roleType;
    public Faction faction;
    public Sprite cardImage;
    [TextArea(2, 4)]
    public string description;
}
