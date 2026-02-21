using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI brain for a single non-human player.
/// Called by GameManager when it's this player's turn.
/// AI difficulty is intentionally "smart-casual":
///   - Prioritizes killing the Emperor if it's a Rebel/Assassin
///   - Protects the Emperor if it's on the Emperor's side
///   - Uses hidden actions when it has enough stamina and a good target exists
///   - Reveals role voluntarily only if tactically beneficial
/// </summary>
public class AIPlayer
{
    private readonly Player self;
    private readonly GameManager gm;

    // Delay between AI actions so the player can follow along
    private const float ActionDelay = 0.9f;

    public AIPlayer(Player player, GameManager manager)
    {
        self = player;
        gm   = manager;
    }

    /// <summary>GameManager calls this to run the AI's full turn as a coroutine.</summary>
    public IEnumerator TakeTurn()
    {
        yield return new WaitForSeconds(ActionDelay);

        if (!self.isAlive || gm.gamePhase != GamePhase.Playing)
            yield break;

        // 1. Try to activate a pending hidden action on a high-value target
        if (TryActivateHiddenAction())
            yield return new WaitForSeconds(ActionDelay);

        // 2. Try to place a hidden action if no action used yet
        if (!self.hasUsedActionThisTurn)
        {
            TryPlaceHiddenAction();
            yield return new WaitForSeconds(ActionDelay);
        }

        // 3. Draw a card if haven't yet and hand is not full
        if (!self.hasDrawnThisTurn && self.hand.Count < Player.MaxHandSize && DeckManager.Instance != null)
        {
            DeckManager.Instance.DealTo(self);
            self.hasDrawnThisTurn = true;
            yield return new WaitForSeconds(ActionDelay * 0.5f);
        }

        // 4. Play a card from hand if useful
        if (!self.hasUsedActionThisTurn)
        {
            TryPlayCard();
            yield return new WaitForSeconds(ActionDelay);
        }

        // 5. Try to attack
        if (!self.hasAttackedThisTurn && self.stamina >= 3)
        {
            TryAttack();
            yield return new WaitForSeconds(ActionDelay);
        }

        // 6. Consider revealing role voluntarily
        TryRevealRole();

        yield return new WaitForSeconds(ActionDelay * 0.5f);

        // 5. End turn (use AIEndTurn to bypass the aiTurnRunning guard)
        if (gm.gamePhase == GamePhase.Playing)
            gm.AIEndTurn();
    }

    // ── AI DECISIONS ──────────────────────────────────────────────

    bool TryActivateHiddenAction()
    {
        if (self.stamina < 5) return false;

        // Find a target that has our hidden action and is high priority
        Player target = FindBestHiddenActionTarget();
        if (target == null) return false;

        int idx = gm.players.IndexOf(target);
        gm.SelectTarget(idx);
        gm.ActivateHiddenAction();
        return true;
    }

    void TryPlayCard()
    {
        if (self.hand.Count == 0) return;

        // Prefer heal if low HP
        CardData heal = self.hand.FirstOrDefault(c => c.effectType == CardEffectType.ActionHeal);
        if (heal != null && self.hp < self.baseHp / 2)
        {
            gm.PlayCard(heal);
            return;
        }

        // Equip items
        CardData item = self.hand.FirstOrDefault(c => c.cardType == CardType.Item);
        if (item != null)
        {
            gm.PlayCard(item);
            return;
        }

        // Play events (they affect everyone, AI always plays them)
        CardData ev = self.hand.FirstOrDefault(c => c.cardType == CardType.Event);
        if (ev != null)
        {
            gm.PlayCard(ev);
            return;
        }

        // Poison a threat if stamina allows
        CardData poison = self.hand.FirstOrDefault(c => c.effectType == CardEffectType.ActionPoison);
        if (poison != null && self.stamina >= poison.staminaCost)
        {
            Player target = FindBestAttackTarget();
            if (target != null)
            {
                gm.SelectTarget(gm.players.IndexOf(target));
                gm.PlayCard(poison);
                return;
            }
        }

        // Steal from richest hand
        CardData steal = self.hand.FirstOrDefault(c => c.effectType == CardEffectType.ActionSteal);
        if (steal != null && self.stamina >= steal.staminaCost)
        {
            Player richest = gm.AlivePlayers()
                .Where(p => p != self && p.hand.Count > 0)
                .OrderByDescending(p => p.hand.Count)
                .FirstOrDefault();
            if (richest != null)
            {
                gm.SelectTarget(gm.players.IndexOf(richest));
                gm.PlayCard(steal);
                return;
            }
        }
    }

    void TryPlaceHiddenAction()
    {
        // Only place if we have enough stamina to activate next round (need 5 later)
        if (self.stamina < 2) return;

        // Hidden action can target Emperor even when shielded
        Player target = FindBestHiddenPlaceTarget();
        if (target == null) return;

        // Don't place hidden on same target we already placed one on
        bool alreadyPlaced = target.hiddenActionsOnMe.Any(a => a.owner == self);
        if (alreadyPlaced) return;

        int idx = gm.players.IndexOf(target);
        gm.SelectTarget(idx);
        gm.PlaceHiddenAction();
    }

    void TryAttack()
    {
        Player target = FindBestAttackTarget();
        if (target == null) return;

        int idx = gm.players.IndexOf(target);
        gm.SelectTarget(idx);
        gm.AttackSelected();
    }

    void TryRevealRole()
    {
        if (self.isRevealed) return;

        // RedDevil: reveal early to start getting immunity
        if (self.role?.roleType == RoleType.RedDevil && gm.currentRound >= 2)
        {
            gm.RevealCurrentPlayerRole();
            return;
        }

        // Guard: reveal to block Assassin effect if Assassin is low HP
        if (self.role?.roleType == RoleType.Guard)
        {
            bool assassinLowHp = gm.players.Any(p =>
                p.isAlive && p.role?.roleType == RoleType.Assassin && p.hp <= 2);
            if (assassinLowHp)
                gm.RevealCurrentPlayerRole();
        }
    }

    // ── TARGET SELECTION ──────────────────────────────────────────

    // Target cho đòn đánh thường — không được nhắm Hoàng đế khi còn shield
    Player FindBestAttackTarget()
    {
        List<Player> candidates = gm.AlivePlayers()
            .Where(p => p != self)
            .Where(p => !(p.role?.roleType == RoleType.Emperor && gm.IsEmperorShielded()))
            .ToList();

        if (candidates.Count == 0) return null;

        Faction myFaction = self.role?.faction ?? Faction.Neutral;

        // Sort priority: highest threat first
        return candidates
            .OrderByDescending(p => ThreatScore(p, myFaction))
            .FirstOrDefault();
    }

    // Target để đặt hidden action — Hoàng đế vẫn hợp lệ dù còn shield
    Player FindBestHiddenPlaceTarget()
    {
        List<Player> candidates = gm.AlivePlayers()
            .Where(p => p != self)
            .ToList();

        if (candidates.Count == 0) return null;

        bool alreadyPlaced(Player p) => p.hiddenActionsOnMe.Any(a => a.owner == self);

        return candidates
            .Where(p => !alreadyPlaced(p))
            .OrderByDescending(p => ThreatScore(p, self.role?.faction ?? Faction.Neutral))
            .FirstOrDefault();
    }

    Player FindBestHiddenActionTarget()
    {
        // Find targets who have our hidden action placed and it's been a round
        return gm.players
            .Where(p => p.isAlive && p != self &&
                        p.hiddenActionsOnMe.Any(a =>
                            a.owner == self && gm.currentRound > a.placedRound))
            .OrderByDescending(p => ThreatScore(p, self.role?.faction ?? Faction.Neutral))
            .FirstOrDefault();
    }

    int ThreatScore(Player target, Faction myFaction)
    {
        int score = 0;

        // Revealed roles give us info
        if (target.isRevealed && target.role != null)
        {
            switch (myFaction)
            {
                case Faction.Emperor:
                    // Attack Rebels/Assassins
                    if (target.role.faction == Faction.Rebel) score += 100;
                    if (target.role.roleType == RoleType.Assassin) score += 80;
                    if (target.role.roleType == RoleType.RedDevil) score += 40;
                    break;

                case Faction.Rebel:
                    // Attack Emperor and his allies
                    if (target.role.roleType == RoleType.Emperor) score += 200;
                    if (target.role.faction == Faction.Emperor) score += 60;
                    break;

                case Faction.Third: // RedDevil
                    // Attack whoever is strongest (most hp left)
                    score += target.hp * 10;
                    break;

                case Faction.Neutral: // Farmer
                    // Farmer tries to survive — attack whoever threatens them most
                    // (target with highest attack stat)
                    score += target.attack * 8;
                    break;
            }
        }
        else
        {
            // Unknown role — attack weakest target to reduce threats
            score += (10 - target.hp) * 5;
        }

        // Prefer low-HP targets (closer to kill)
        score += (10 - Mathf.Clamp(target.hp, 0, 10)) * 3;

        // Prefer targets we can actually damage
        int damage = self.attack - target.defense;
        if (damage > 0) score += damage * 4;

        return score;
    }
}
