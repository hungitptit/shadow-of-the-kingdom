using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Executes card effects. Called by GameManager when a card is played.
/// All effects that need a target use GameManager.Instance.selectedTargetIndex.
/// </summary>
public static class CardEffectExecutor
{
    static GameManager GM => GameManager.Instance;
    static DeckManager Deck => DeckManager.Instance;

    /// <summary>
    /// Main entry point. Returns false if the card cannot be played right now.
    /// </summary>
    public static bool Execute(Player owner, CardData card)
    {
        // Lá HiddenAction không tốn stamina khi đặt, chỉ cần chọn target
        if (card.cardType == CardType.Action && owner.stamina < card.staminaCost)
        {
            GM.LogEvent($"Không đủ Thể lực (cần {card.staminaCost}).");
            return false;
        }

        if (card.cardType == CardType.Action)
            owner.stamina -= card.staminaCost;

        bool success = card.effectType switch
        {
            CardEffectType.ItemArmor          => ApplyArmor(owner, card),
            CardEffectType.ItemWeapon         => ApplyWeapon(owner, card),
            CardEffectType.ItemPotion         => ApplyPotion(owner, card),
            CardEffectType.ActionBeg          => ActionBeg(owner),
            CardEffectType.ActionRevive       => ActionRevive(owner),
            CardEffectType.ActionFlee         => ActionFlee(owner),
            CardEffectType.ActionSteal        => ActionSteal(owner),
            CardEffectType.ActionHeal         => ActionHeal(owner),
            CardEffectType.ActionPoison       => ActionPoison(owner),
            CardEffectType.ActionSwapStats    => ActionSwapStats(owner),
            CardEffectType.ActionExorcism     => ActionExorcism(owner),
            CardEffectType.ActionFortune      => ActionFortune(owner),
            CardEffectType.ActionCounter      => ActionCounter(owner),
            CardEffectType.ActionCurse        => ActionCurse(owner),
            CardEffectType.ActionStealWeapon   => ActionStealWeapon(owner),
            CardEffectType.ActionStealArmor    => ActionStealArmor(owner),
            CardEffectType.ActionRepelInvasion => ActionRepelInvasion(owner),
            CardEffectType.EventDrought        => EventDrought(),
            CardEffectType.EventInvasion      => EventInvasion(),
            CardEffectType.EventShareRice     => EventShareRice(owner),
            CardEffectType.EventGoddess       => EventGoddess(owner),
            CardEffectType.HiddenAssassinate  => PlaceAssassinate(owner, card),
            CardEffectType.HiddenProtect      => PlaceProtect(owner, card),
            _                                 => false
        };

        if (!success && card.cardType == CardType.Action)
            owner.stamina += card.staminaCost;

        return success;
    }

    // ── ITEMS ────────────────────────────────────────────────────

    static bool ApplyArmor(Player p, CardData card)
    {
        p.defense++;
        p.equippedItems.Add(card);
        GM.LogEvent($"{p.playerName} trang bị Áo giáp. Phòng thủ +1.");
        return true;
    }

    static bool ApplyWeapon(Player p, CardData card)
    {
        p.attack++;
        p.equippedItems.Add(card);
        GM.LogEvent($"{p.playerName} trang bị Binh khí. Tấn công +1.");
        return true;
    }

    static bool ApplyPotion(Player p, CardData card)
    {
        p.maxStamina++;
        p.equippedItems.Add(card);
        GM.LogEvent($"{p.playerName} uống Thuốc bổ. Thể lực tối đa +1 (nay: {p.maxStamina}).");
        return true;
    }

    // ── ACTIONS ──────────────────────────────────────────────────

    static bool ActionBeg(Player owner)
    {
        foreach (Player p in GM.AlivePlayers())
        {
            if (p == owner || p.hand.Count == 0) continue;
            CardData given = p.hand[Random.Range(0, p.hand.Count)];
            p.hand.Remove(given);
            if (owner.hand.Count < Player.MaxHandSize)
                owner.hand.Add(given);
            else
                Deck.Discard(given);
        }
        GM.LogEvent($"{owner.playerName} Ăn xin — nhận bài từ mọi người.");
        return true;
    }

    static bool ActionRevive(Player owner)
    {
        List<Player> dead = GM.players.Where(p => !p.isAlive).ToList();
        if (dead.Count == 0)
        {
            GM.LogEvent("Không có người nào đã bị loại để hồi sinh.");
            return false;
        }

        Deck.DiscardAll(owner.hand);

        Player revived = GetDeadTarget() ?? dead[0];
        revived.isAlive = true;
        revived.hp = revived.baseHp;
        revived.stamina = Mathf.Min(2, revived.maxStamina);
        revived.hand.Clear();
        Deck.DealStartingHand(revived);

        GM.LogEvent($"{owner.playerName} Cải tử hoàn sinh — {revived.playerName} trở lại với {revived.hp} Khí huyết!");
        GM.CheckWinConditionsPublic();
        return true;
    }

    static bool ActionFlee(Player owner)
    {
        owner.fleeActive = true;
        GM.LogEvent($"{owner.playerName} Chạy giặc — miễn nhiễm Giặc ngoại xâm vòng này.");
        return true;
    }

    static bool ActionRepelInvasion(Player owner)
    {
        // Đánh đuổi ngoại xâm:
        //   - Tất cả người còn sống miễn nhiễm Giặc ngoại xâm vòng này (không mất lá bài)
        //   - Riêng người chơi lá này được thưởng thêm 2 lá bài
        foreach (Player p in GM.AlivePlayers())
            p.fleeActive = true;

        int drawn = 0;
        for (int i = 0; i < 2; i++)
        {
            if (owner.hand.Count < Player.MaxHandSize && Deck.DealTo(owner))
                drawn++;
        }

        GM.LogEvent($"{owner.playerName} Đánh đuổi ngoại xâm — cả làng miễn nhiễm Giặc vòng này! Bạn rút thêm {drawn} lá.");
        return true;
    }

    static bool ActionSteal(Player owner)
    {
        Player target = GetTarget();
        if (target == null) return false;
        if (target.hand.Count == 0)
        {
            GM.LogEvent($"{target.playerName} không có bài để trộm.");
            return false;
        }

        CardData stolen = target.hand[Random.Range(0, target.hand.Count)];
        target.hand.Remove(stolen);
        if (owner.hand.Count < Player.MaxHandSize)
            owner.hand.Add(stolen);
        else
            Deck.Discard(stolen);

        GM.LogEvent($"{owner.playerName} Ăn trộm 1 lá bài của {target.playerName}.");
        return true;
    }

    static bool ActionHeal(Player owner)
    {
        owner.hp = Mathf.Min(owner.hp + 1, owner.baseHp + 10);
        GM.LogEvent($"{owner.playerName} uống Thuốc hồi phục. +1 Khí huyết (nay: {owner.hp}).");
        return true;
    }

    static bool ActionPoison(Player owner)
    {
        Player target = GetTarget();
        if (target == null) return false;

        target.poisonRoundsLeft += 3;
        GM.LogEvent($"{owner.playerName} hạ độc {target.playerName} — mất 1 Khí huyết/vòng trong 3 vòng.");
        return true;
    }

    static bool ActionSwapStats(Player owner)
    {
        Player target = GetTarget();
        if (target == null) return false;

        (target.attack, target.defense) = (target.defense, target.attack);
        GM.LogEvent($"{owner.playerName} đảo lộn chỉ số {target.playerName}: Tấn công={target.attack}, Phòng thủ={target.defense}.");
        return true;
    }

    static bool ActionExorcism(Player owner)
    {
        if (owner.isStaminaLocked)
        {
            owner.isStaminaLocked = false;
            GM.LogEvent($"{owner.playerName} giải trừ Oán linh của bản thân.");
            return true;
        }

        Player target = GetTarget();
        if (target == null) return false;

        target.isStaminaLocked = false;
        Player transferTo = GM.AlivePlayers()
            .Where(p => p != owner && p != target)
            .OrderBy(_ => Random.value)
            .FirstOrDefault();

        if (transferTo != null)
        {
            transferTo.isStaminaLocked = true;
            GM.LogEvent($"{owner.playerName} chuyển Oán linh từ {target.playerName} sang {transferTo.playerName}.");
        }
        else
        {
            GM.LogEvent($"{owner.playerName} giải trừ Oán linh cho {target.playerName}.");
        }
        return true;
    }

    static bool ActionFortune(Player owner)
    {
        List<CardData> top3 = Deck.PeekTop(3);
        string names = string.Join(", ", top3.Select(c => c.cardName));
        GM.LogEvent($"{owner.playerName} Thầy bói — 3 lá trên deck: [{names}].");
        GM.uiManager?.ShowPeekCards(top3);
        return true;
    }

    static bool ActionCounter(Player owner)
    {
        owner.hasCounter = true;
        GM.LogEvent($"{owner.playerName} chuẩn bị Phản đòn.");
        return true;
    }

    static bool ActionCurse(Player owner)
    {
        Player target = GetTarget();
        if (target == null) return false;

        target.isStaminaLocked = true;
        target.stamina = Mathf.Min(target.stamina, 2);
        GM.LogEvent($"{owner.playerName} ám Oán linh lên {target.playerName} — Thể lực tối đa bị khóa ở 2!");
        return true;
    }

    static bool ActionStealWeapon(Player owner)
    {
        Player target = GetTarget();
        if (target == null) return false;

        CardData weapon = target.equippedItems.FirstOrDefault(c => c.effectType == CardEffectType.ItemWeapon);
        if (weapon == null)
        {
            GM.LogEvent($"{target.playerName} không có Binh khí.");
            return false;
        }

        target.equippedItems.Remove(weapon);
        target.attack--;
        owner.equippedItems.Add(weapon);
        owner.attack++;
        GM.LogEvent($"{owner.playerName} cướp Binh khí của {target.playerName}. Tấn công: {owner.playerName}+1, {target.playerName}-1.");
        return true;
    }

    static bool ActionStealArmor(Player owner)
    {
        Player target = GetTarget();
        if (target == null) return false;

        CardData armor = target.equippedItems.FirstOrDefault(c => c.effectType == CardEffectType.ItemArmor);
        if (armor == null)
        {
            GM.LogEvent($"{target.playerName} không có Áo giáp.");
            return false;
        }

        target.equippedItems.Remove(armor);
        target.defense--;
        owner.equippedItems.Add(armor);
        owner.defense++;
        GM.LogEvent($"{owner.playerName} cướp Áo giáp của {target.playerName}. Phòng thủ: {owner.playerName}+1, {target.playerName}-1.");
        return true;
    }

    // ── EVENTS ───────────────────────────────────────────────────

    static bool EventDrought()
    {
        foreach (Player p in GM.AlivePlayers())
            p.stamina = Mathf.Max(0, p.stamina - 3);
        GM.LogEvent("Sự kiện HẠN HÁN — tất cả mất 3 Thể lực!");
        return true;
    }

    static bool EventInvasion()
    {
        GM.LogEvent("Sự kiện GIẶC NGOẠI XÂM — mỗi người hủy 2 lá bài!");
        foreach (Player p in GM.AlivePlayers())
        {
            if (p.fleeActive)
            {
                GM.LogEvent($"{p.playerName} Chạy giặc — miễn nhiễm!");
                p.fleeActive = false;
                continue;
            }
            int toDiscard = Mathf.Min(2, p.hand.Count);
            for (int i = 0; i < toDiscard; i++)
            {
                CardData c = p.hand[p.hand.Count - 1];
                p.hand.RemoveAt(p.hand.Count - 1);
                Deck.Discard(c);
            }
        }
        return true;
    }

    static bool EventShareRice(Player owner)
    {
        List<CardData> pool = new();
        foreach (Player p in GM.AlivePlayers())
        {
            if (p.hand.Count > 0)
            {
                CardData c = p.hand[p.hand.Count - 1];
                p.hand.RemoveAt(p.hand.Count - 1);
                pool.Add(c);
            }
        }
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        List<Player> alive = GM.AlivePlayers();
        for (int i = 0; i < pool.Count; i++)
        {
            Player recipient = alive[i % alive.Count];
            if (recipient.hand.Count < Player.MaxHandSize)
                recipient.hand.Add(pool[i]);
            else
                Deck.Discard(pool[i]);
        }
        GM.LogEvent("Sự kiện GÓP GẠO THỔI CƠM CHUNG — bài được xáo và chia lại!");
        return true;
    }

    static bool EventGoddess(Player owner)
    {
        owner.hp = owner.baseHp;
        Deck.DealTo(owner);
        GM.LogEvent($"{owner.playerName} Cô Thương phù hộ — hồi phục toàn bộ Khí huyết + bốc 1 lá!");
        return true;
    }

    // ── SECRET CARDS ─────────────────────────────────────────────

    static bool PlaceAssassinate(Player owner, CardData card)
    {
        Player target = GetTarget();
        if (target == null) return false;
        GM.PlaceSecretCard(owner, target, SecretType.Assassinate, card.artwork);
        return true;
    }

    static bool PlaceProtect(Player owner, CardData card)
    {
        Player target = GetTarget();
        if (target == null) return false;
        GM.PlaceSecretCard(owner, target, SecretType.Protect, card.artwork);
        return true;
    }

    // ── HELPERS ──────────────────────────────────────────────────

    static Player GetTarget()
    {
        int idx = GM.selectedTargetIndex;
        if (idx < 0 || idx >= GM.players.Count)
        {
            GM.LogEvent("Chưa chọn mục tiêu.");
            return null;
        }
        Player t = GM.players[idx];
        if (!t.isAlive)
        {
            GM.LogEvent("Mục tiêu đã bị hạ.");
            return null;
        }
        return t;
    }

    static Player GetDeadTarget()
    {
        int idx = GM.selectedTargetIndex;
        if (idx < 0 || idx >= GM.players.Count) return null;
        Player t = GM.players[idx];
        return !t.isAlive ? t : null;
    }
}
