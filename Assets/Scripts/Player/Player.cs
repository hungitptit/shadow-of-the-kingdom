using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public string playerName;
    public RoleData role;

    // Base dice values (assigned at setup, total ≤15, min 1 each)
    public int baseHp = 4;
    public int baseStamina = 4;
    public int baseAttack = 4;
    public int baseDefense = 3;

    // Runtime stats (base + card/role modifiers)
    public int hp;
    public int stamina;
    public int attack;
    public int defense;

    public bool isAlive = true;
    public bool isRevealed = false;

    // Has this player taken their attack action this turn?
    public bool hasAttackedThisTurn = false;
    // Has this player taken their action card this turn?
    public bool hasUsedActionThisTurn = false;
    // Has this player drawn a card this turn?
    public bool hasDrawnThisTurn = false;

    // RedDevil: becomes immune to first attack each round after reveal
    public bool redDevilImmunityUsedThisRound = false;

    // Guard: can only intervene once per game
    public bool guardHasIntervened = false;

    // HiddenActions placed ON this player by others
    public List<HiddenAction> hiddenActionsOnMe = new();

    // HiddenActions this player has placed (to clean up on death)
    public List<HiddenAction> hiddenActionsPlacedByMe = new();

    public Player(string name)
    {
        playerName = name;
        ApplyBaseStats();
    }

    public void ApplyBaseStats()
    {
        hp = baseHp;
        stamina = baseStamina;
        attack = baseAttack;
        defense = baseDefense;
    }

    // Set all four base dice values. Total must be ≤15, each ≥1.
    public bool SetDiceValues(int hp, int stamina, int attack, int defense)
    {
        if (hp < 1 || stamina < 1 || attack < 1 || defense < 1) return false;
        if (hp + stamina + attack + defense > 15) return false;

        baseHp = hp;
        baseStamina = stamina;
        baseAttack = attack;
        baseDefense = defense;
        ApplyBaseStats();
        return true;
    }

    public void ResetTurnFlags()
    {
        hasAttackedThisTurn = false;
        hasUsedActionThisTurn = false;
        hasDrawnThisTurn = false;
    }
}
