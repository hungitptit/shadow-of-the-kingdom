using System.Collections.Generic;

public class Player
{
    public string playerName;
    public RoleData role;

    public int hp = 5;
    public int stamina = 5;
    public int attack = 2;
    public int defense = 1;

    public bool isAlive = true;
    public bool isRevealed = false;
    public List<HiddenAction> hiddenActionsOnMe = new();

    public Player(string name)
    {
        playerName = name;
    }
}
