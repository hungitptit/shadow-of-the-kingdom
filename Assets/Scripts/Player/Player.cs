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
    public int maxStamina;  // can be boosted by Thuoc bo item, locked by Oan linh
    public int attack;
    public int defense;

    // ── Status effects ────────────────────────────
    public int poisonRoundsLeft = 0;        // Thuoc doc: -1 HP per round
    public bool isStaminaLocked = false;    // Oan linh: max stamina locked at 2
    public bool hasCounter = false;         // Phan don: reflect next attack
    public bool fleeActive = false;         // Chay giac: immune to Invasion event this round

    // ── Items equipped (persistent) ───────────────
    public List<CardData> equippedItems = new List<CardData>();

    public bool isAlive = true;
    public bool isRevealed = false;
    // In single-player: human can see their own role even if not publicly revealed
    public bool isSelfKnown = false;

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

    // Card hand (max 5)
    public List<CardData> hand = new();
    public const int MaxHandSize = 5;

    // Lá secret đặt lên player này (hiện mặt sau trên panel)
    public List<HiddenAction> hiddenActionsOnMe = new();

    // Lá secret player này đã đặt (để dọn khi chết)
    public List<HiddenAction> hiddenActionsPlacedByMe = new();

    // Đang được bảo vệ bởi lá Protect (vô hiệu đòn tấn công + ám sát cho đến khi kích hoạt)
    public bool isProtected = false;

    public Player(string name)
    {
        playerName = name;
        ApplyBaseStats();
    }

    public void ApplyBaseStats()
    {
        hp = baseHp;
        stamina = baseStamina;
        maxStamina = baseStamina;
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
