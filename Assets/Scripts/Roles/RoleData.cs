using UnityEngine;

public enum RoleType
{
    Emperor,
    Rebel,
    Assassin,
    Queen
}

[CreateAssetMenu(menuName = "Game/Role")]
public class RoleData : ScriptableObject
{
    public RoleType roleType;
    public Sprite cardImage;
}
