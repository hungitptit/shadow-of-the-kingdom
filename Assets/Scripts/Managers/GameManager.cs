using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum GamePhase
{
    Setup,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Role Assets")]
    public List<RoleData> allRoles = new();  // All role ScriptableObjects (assign in Inspector)

    [Header("Player Setup")]
    public int playerCount = 4;

    [Header("Card System")]
    public DeckManager deckManager;

    [Header("UI References")]
    public UIManager uiManager;
    public Transform playerPanelContainer;
    public GameObject playerPanelPrefab;

    // Runtime state
    public List<Player> players = new List<Player>();
    private List<PlayerPanelUI> playerPanels = new List<PlayerPanelUI>();

    public int currentPlayerIndex = 0;
    public int currentRound = 1;
    public int selectedTargetIndex = -1;
    public GamePhase gamePhase = GamePhase.Setup;

    // Tracks how many alive players have taken their turn this round
    private int turnsThisRound = 0;

    // Win result
    public string winnerText = "";

    // ── AI / Single-player ──────────────────────────────────────
    // humanPlayerIndex = which player index the human controls (-1 = all human / multiplayer)
    public int humanPlayerIndex = 0;
    private Dictionary<int, AIPlayer> aiPlayers = new Dictionary<int, AIPlayer>();
    private bool aiTurnRunning = false;

    /// <summary>True khi đang chờ human chọn (vd: popup Bảo vệ). AI coroutine yield cho đến khi false.</summary>
    public bool isWaitingForHumanInput = false;

    public bool IsCurrentPlayerHuman =>
        GameConfig.Mode == GameConfig.GameMode.MultiPlayer ||
        currentPlayerIndex == humanPlayerIndex;

    void Awake()
    {
        Instance = this;

        // Auto-find UIManager if not assigned in Inspector
        if (uiManager == null)
            uiManager = Object.FindFirstObjectByType<UIManager>();
    }

    void Start()
    {
        // Read config from MainMenu if available
        int count = GameConfig.PlayerCount > 0 ? GameConfig.PlayerCount : playerCount;
        humanPlayerIndex = GameConfig.HumanPlayerIndex;
        SetupGame(count);
    }

    // ─────────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────────

    public void SetupGame(int count)
    {
        StopAllCoroutines();
        players.Clear();
        playerPanels.Clear();
        aiPlayers.Clear();
        currentPlayerIndex = 0;
        currentRound = 1;
        selectedTargetIndex = -1;
        gamePhase = GamePhase.Playing;
        winnerText = "";
        aiTurnRunning = false;

        uiManager?.HideGameOver();

        // Create players
        for (int i = 0; i < count; i++)
        {
            string name = (GameConfig.Mode == GameConfig.GameMode.SinglePlayer && i == humanPlayerIndex)
                ? "Bạn"
                : "Player " + (i + 1);
            players.Add(new Player(name));
        }

        // Assign roles by player count (Appendix A) and shuffle
        List<RoleData> selectedRoles = GetRolesForPlayerCount(count);
        ShuffleList(selectedRoles);

        for (int i = 0; i < players.Count; i++)
        {
            players[i].role = selectedRoles[i];
        }

        // Emperor reveals immediately
        Player emperor = players.FirstOrDefault(p => p.role.roleType == RoleType.Emperor);
        if (emperor != null)
            emperor.isRevealed = true;

        // In single-player: human always knows their own role
        if (GameConfig.Mode == GameConfig.GameMode.SinglePlayer)
        {
            // Mark human's role as "known to self" — shown in info panel but not on board
            players[humanPlayerIndex].isSelfKnown = true;
        }

        turnsThisRound = 0;

        // Build and deal cards
        if (deckManager != null)
        {
            deckManager.BuildDeck();
            foreach (Player p in players)
                deckManager.DealStartingHand(p);
        }

        // Create AI players for all non-human slots in single-player
        if (GameConfig.Mode == GameConfig.GameMode.SinglePlayer)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (i != humanPlayerIndex)
                    aiPlayers[i] = new AIPlayer(players[i], this);
            }
        }

        CreatePlayerPanels();
        RefreshAll();

        LogEvent("Ván chơi bắt đầu. Vòng 1.");
        LogEvent(players[humanPlayerIndex].playerName + " là " +
                 GetRoleName(players[humanPlayerIndex].role.roleType));
        LogEvent("Lượt: " + CurrentPlayer().playerName);

        // If first turn is AI, trigger it
        TriggerAIIfNeeded();
    }

    List<RoleData> GetRolesForPlayerCount(int count)
    {
        // Appendix A: roles by player count
        // 4: Emperor, Queen, Rebel, Assassin
        // 5: +Guard
        // 6: +Farmer
        // 7: +RedDevil
        // 8: +Judge
        // 9: +1 Rebel
        List<RoleType> needed = new List<RoleType> {
            RoleType.Emperor, RoleType.Queen, RoleType.Rebel, RoleType.Assassin
        };
        if (count >= 5) needed.Add(RoleType.Guard);
        if (count >= 6) needed.Add(RoleType.Farmer);
        if (count >= 7) needed.Add(RoleType.RedDevil);
        if (count >= 8) needed.Add(RoleType.Judge);
        if (count >= 9) needed.Add(RoleType.Rebel);

        List<RoleData> result = new List<RoleData>();
        foreach (RoleType rt in needed)
        {
            RoleData rd = allRoles.FirstOrDefault(r => r.roleType == rt && !result.Contains(r));
            if (rd != null) result.Add(rd);
            else Debug.LogWarning($"Missing RoleData for {rt}");
        }
        return result;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ─────────────────────────────────────────────
    // PLAYER ACCESS
    // ─────────────────────────────────────────────

    public Player CurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    public List<Player> AlivePlayers()
    {
        return players.Where(p => p.isAlive).ToList();
    }

    // ─────────────────────────────────────────────
    // TURN & ROUND
    // ─────────────────────────────────────────────

    // Called by the human player via UI button
    public void EndTurn()
    {
        if (gamePhase != GamePhase.Playing) return;
        if (aiTurnRunning) return;          // block human input during AI turn
        AdvanceTurn();
    }

    // Called internally by AI (bypasses the aiTurnRunning guard)
    public void AIEndTurn()
    {
        if (gamePhase != GamePhase.Playing) return;
        AdvanceTurn();
    }

    void AdvanceTurn()
    {
        CurrentPlayer().ResetTurnFlags();
        selectedTargetIndex = -1;
        turnsThisRound++;

        int aliveBefore = AlivePlayers().Count;

        if (turnsThisRound >= aliveBefore)
        {
            EndRound();
        }
        else
        {
            AdvanceToNextAlive();
            RefreshAll();
            if (gamePhase == GamePhase.Playing)
            {
                LogEvent("Lượt: " + CurrentPlayer().playerName);
                TriggerAIIfNeeded();
            }
        }
    }

    void TriggerAIIfNeeded()
    {
        if (gamePhase != GamePhase.Playing) return;
        if (IsCurrentPlayerHuman) return;
        if (aiTurnRunning) return;
        if (!aiPlayers.TryGetValue(currentPlayerIndex, out AIPlayer ai)) return;

        aiTurnRunning = true;
        StartCoroutine(RunAITurn(ai));
    }

    IEnumerator RunAITurn(AIPlayer ai)
    {
        yield return StartCoroutine(ai.TakeTurn());
        aiTurnRunning = false;
        // After flag is cleared, trigger next AI if round just wrapped
        TriggerAIIfNeeded();
    }

    void AdvanceToNextAlive()
    {
        int start = currentPlayerIndex;
        do
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        } while (!players[currentPlayerIndex].isAlive && currentPlayerIndex != start);
    }

    int GetFirstAliveIndex()
    {
        for (int i = 0; i < players.Count; i++)
            if (players[i].isAlive) return i;
        return 0;
    }

    void EndRound()
    {
        currentRound++;
        turnsThisRound = 0;
        LogEvent($"─── Vòng {currentRound} bắt đầu ───");

        // Per-round effects for all alive players
        foreach (Player p in players)
        {
            if (!p.isAlive) continue;

            // +1 stamina (capped at maxStamina, or 2 if cursed)
            int cap = p.isStaminaLocked ? 2 : p.maxStamina;
            p.stamina = Mathf.Min(p.stamina + 1, cap);

            // Poison tick
            if (p.poisonRoundsLeft > 0)
            {
                p.hp -= 1;
                p.poisonRoundsLeft--;
                LogEvent($"{p.playerName} trúng độc: -1 Khí huyết (còn {p.poisonRoundsLeft} vòng).");
                if (p.hp <= 0) KillPlayer(p);
            }

            // Reset per-round flags
            p.redDevilImmunityUsedThisRound = false;
            p.fleeActive = false;
        }

        // Check Judge effect (after Round 6)
        CheckJudgeEffect();

        // Set turn to first alive player
        currentPlayerIndex = GetFirstAliveIndex();

        RefreshAll();

        if (gamePhase == GamePhase.Playing)
        {
            LogEvent("Lượt: " + CurrentPlayer().playerName);
            // TriggerAIIfNeeded is called by RunAITurn after flag reset,
            // or directly here only if no AI turn is currently running
            if (!aiTurnRunning)
                TriggerAIIfNeeded();
        }
    }

    void CheckJudgeEffect()
    {
        if (currentRound <= 6) return;

        Player judge = players.FirstOrDefault(p =>
            p.isAlive && p.role?.roleType == RoleType.Judge && !p.isRevealed);

        // Judge effect: after round 6, if alive → reveal, then force 2 others to reveal
        // This is triggered once; we use a flag on Player to avoid repeat
        // For now: the Judge player must manually trigger via a button
        // (Full automation would need a "judgeEffectUsed" flag — handled in Phase 2E)
    }

    // ─────────────────────────────────────────────
    // COMBAT
    // ─────────────────────────────────────────────

    public void AttackSelected()
    {
        if (gamePhase != GamePhase.Playing) return;
        if (selectedTargetIndex == -1) return;

        Player attacker = CurrentPlayer();
        Player target = players[selectedTargetIndex];

        if (!attacker.isAlive || !target.isAlive) return;
        if (attacker.hasAttackedThisTurn)
        {
            LogEvent("Đã thực hiện đòn đánh trong lượt này.");
            return;
        }
        if (attacker.stamina < 3)
        {
            LogEvent("Không đủ Thể lực (cần 3).");
            return;
        }

        // Luật: không thể tấn công Hoàng đế bằng đòn đánh khi Cấm quân còn sống
        if (target.role?.roleType == RoleType.Emperor && IsEmperorShielded())
        {
            LogEvent("Không thể tấn công Hoàng đế — Cấm quân vẫn còn sống!");
            return;
        }

        attacker.stamina -= 3;
        attacker.hasAttackedThisTurn = true;

        // Nếu target có Bảo vệ bí mật và target là human → hỏi trước
        bool targetIsHuman = target == players.ElementAtOrDefault(humanPlayerIndex) ||
                             GameConfig.Mode == GameConfig.GameMode.MultiPlayer;

        if (target.isProtected && targetIsHuman && uiManager != null)
        {
            LogEvent($"{attacker.playerName} tấn công {target.playerName}! [{target.playerName}: chọn có dùng Bảo vệ không?]");
            RefreshAll();
            isWaitingForHumanInput = true;
            uiManager.AskProtectConfirm(useProtect =>
            {
                isWaitingForHumanInput = false;
                StartCoroutine(ResolveAttack(attacker, target, useProtect));
            });
            return;
        }

        // AI target hoặc không có Bảo vệ → xử lý ngay
        StartCoroutine(ResolveAttack(attacker, target, target.isProtected));
    }

    IEnumerator ResolveAttack(Player attacker, Player target, bool useProtect)
    {
        yield return null; // 1 frame để UI cập nhật

        if (useProtect && target.isProtected)
        {
            RemoveProtect(target);
            LogEvent($"{attacker.playerName} tấn công {target.playerName} — Bảo vệ bí mật kích hoạt, đòn bị chặn!");
            RefreshAll();
            yield break;
        }

        // RedDevil immunity: bỏ qua đòn đánh đầu tiên mỗi vòng
        if (target.isRevealed && target.role?.roleType == RoleType.RedDevil
            && !target.redDevilImmunityUsedThisRound)
        {
            target.redDevilImmunityUsedThisRound = true;
            LogEvent($"{attacker.playerName} tấn công {target.playerName} — Quỷ đỏ miễn nhiễm!");
            RefreshAll();
            yield break;
        }

        int damage = attacker.attack - target.defense;
        if (damage < 0) damage = 0;

        target.hp -= damage;
        LogEvent($"{attacker.playerName} tấn công {target.playerName}: -{damage} HP.");

        if (target.hp <= 0)
            KillPlayer(target);

        RefreshAll();
    }

    // ─────────────────────────────────────────────
    // SECRET CARDS (Ám sát / Bảo vệ)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Đặt lá secret (Assassinate hoặc Protect) lên target.
    /// Được gọi từ CardEffectExecutor khi chơi lá HiddenAction từ tay.
    /// </summary>
    public void PlaceSecretCard(Player owner, Player target, SecretType type, UnityEngine.Sprite artwork)
    {
        var action = new HiddenAction(owner, target, currentRound, type, artwork);
        target.hiddenActionsOnMe.Add(action);
        owner.hiddenActionsPlacedByMe.Add(action);

        if (type == SecretType.Protect)
            target.isProtected = true;

        string targetLabel = (target == owner) ? "chính mình" : target.playerName;
        LogEvent($"{owner.playerName} đặt một Hành động bí mật lên {targetLabel}.");
        RefreshAll();
    }

    /// <summary>
    /// Kích hoạt lá Ám sát đã đặt lên target (tốn 5 ST, phải qua 1 vòng).
    /// Human gọi bằng nút ActivateSecret trên UI.
    /// </summary>
    public void ActivateSecretCard()
    {
        if (gamePhase != GamePhase.Playing) return;
        if (selectedTargetIndex == -1) return;

        Player activator = CurrentPlayer();
        Player target = players[selectedTargetIndex];

        HiddenAction action = target.hiddenActionsOnMe.FirstOrDefault(a =>
            a.owner == activator &&
            a.secretType == SecretType.Assassinate &&
            currentRound > a.placedRound);

        if (action == null)
        {
            LogEvent("Không có Ám sát bí mật hợp lệ để kích hoạt.");
            return;
        }
        if (activator.stamina < 5)
        {
            LogEvent("Không đủ Thể lực (cần 5 để kích hoạt).");
            return;
        }

        activator.stamina -= 5;
        RemoveSecret(target, action);

        // Lá Protect chặn cả Ám sát
        if (target.isProtected)
        {
            RemoveProtect(target);
            LogEvent($"{activator.playerName} kích hoạt Ám sát lên {target.playerName} — Bảo vệ bí mật chặn lại!");
            RefreshAll();
            return;
        }

        LogEvent($"{activator.playerName} kích hoạt Ám sát bí mật — {target.playerName} bị hạ!");
        KillPlayer(target);
        RefreshAll();
    }

    /// AI version — bypass interactivity check
    public void AIActivateSecretCard(Player activator, Player target)
    {
        HiddenAction action = target.hiddenActionsOnMe.FirstOrDefault(a =>
            a.owner == activator &&
            a.secretType == SecretType.Assassinate &&
            currentRound > a.placedRound);

        if (action == null || activator.stamina < 5) return;

        activator.stamina -= 5;
        RemoveSecret(target, action);

        if (target.isProtected)
        {
            RemoveProtect(target);
            LogEvent($"{activator.playerName} kích hoạt Ám sát lên {target.playerName} — Bảo vệ bí mật chặn lại!");
            RefreshAll();
            return;
        }

        LogEvent($"{activator.playerName} kích hoạt Ám sát bí mật — {target.playerName} bị hạ!");
        KillPlayer(target);
        RefreshAll();
    }

    void RemoveSecret(Player target, HiddenAction action)
    {
        target.hiddenActionsOnMe.Remove(action);
        action.owner.hiddenActionsPlacedByMe.Remove(action);
    }

    void RemoveProtect(Player target)
    {
        HiddenAction protect = target.hiddenActionsOnMe.FirstOrDefault(
            a => a.secretType == SecretType.Protect);
        if (protect != null) RemoveSecret(target, protect);
        target.isProtected = false;
    }

    // ─────────────────────────────────────────────
    // REVEAL ROLE (voluntary)
    // ─────────────────────────────────────────────

    public void RevealCurrentPlayerRole()
    {
        if (gamePhase != GamePhase.Playing) return;
        Player p = CurrentPlayer();
        if (p.isRevealed) return;

        p.isRevealed = true;
        LogEvent($"{p.playerName} công khai Vai trò: {GetRoleName(p.role.roleType)}");
        HandleRevealEffects(p, false);
        RefreshAll();
    }

    // ─────────────────────────────────────────────
    // KILL & REVEAL
    // ─────────────────────────────────────────────

    public void KillPlayer(Player p)
    {
        p.hp = 0;
        p.isAlive = false;
        bool wasRevealed = p.isRevealed;
        p.isRevealed = true;

        LogEvent($"{p.playerName} bị hạ! Vai trò: {GetRoleName(p.role.roleType)}");

        // Dọn lá secret player này đã đặt lên người khác
        foreach (HiddenAction a in p.hiddenActionsPlacedByMe.ToList())
        {
            a.target.hiddenActionsOnMe.Remove(a);
            if (a.secretType == SecretType.Protect && a.target.isProtected)
            {
                // Kiểm tra còn lá protect khác không
                bool hasOtherProtect = a.target.hiddenActionsOnMe
                    .Any(x => x.secretType == SecretType.Protect);
                if (!hasOtherProtect) a.target.isProtected = false;
            }
        }
        p.hiddenActionsPlacedByMe.Clear();

        // Dọn lá secret đặt lên player này
        foreach (HiddenAction a in p.hiddenActionsOnMe.ToList())
            a.owner.hiddenActionsPlacedByMe.Remove(a);
        p.hiddenActionsOnMe.Clear();
        p.isProtected = false;

        HandleRevealEffects(p, true);
        CheckWinConditions();
    }

    void HandleRevealEffects(Player p, bool killedByDeath)
    {
        switch (p.role.roleType)
        {
            case RoleType.Queen:
                // Queen revealed: Emperor heals 1 HP
                Player emperor = players.FirstOrDefault(ep =>
                    ep.isAlive && ep.role?.roleType == RoleType.Emperor);
                if (emperor != null)
                {
                    emperor.hp += 1;
                    LogEvent($"Hoàng hậu lật bài — Hoàng đế hồi 1 Khí huyết.");
                }
                break;

            case RoleType.Assassin:
                if (killedByDeath)
                {
                    // Assassin killed: check if Guard is alive and revealed
                    Player guard = players.FirstOrDefault(gp =>
                        gp.isAlive && gp.role?.roleType == RoleType.Guard);

                    if (guard != null && guard.isRevealed && !guard.guardHasIntervened)
                    {
                        // Guard intervenes — blocks Assassin's death effect
                        guard.guardHasIntervened = true;
                        LogEvent($"Cấm quân ngăn hiệu ứng Thích khách!");
                    }
                    else
                    {
                        // Assassin's death hurts Emperor
                        Player emp = players.FirstOrDefault(ep =>
                            ep.isAlive && ep.role?.roleType == RoleType.Emperor);
                        if (emp != null)
                        {
                            emp.hp -= 1;
                            LogEvent($"Thích khách bị hạ — Hoàng đế mất 1 Khí huyết!");
                            if (emp.hp <= 0)
                                KillPlayer(emp);
                        }
                    }
                }
                break;

            case RoleType.RedDevil:
                if (!killedByDeath)
                {
                    // RedDevil voluntary reveal: immunity starts next round
                    LogEvent($"Quỷ đỏ công khai — Miễn nhiễm đòn đánh đầu tiên mỗi vòng từ vòng sau.");
                }
                break;
        }
    }

    // ─────────────────────────────────────────────
    // WIN CONDITIONS
    // ─────────────────────────────────────────────

    void CheckWinConditions()
    {
        List<Player> alive = AlivePlayers();

        // Emperor dead → Rebels win
        bool emperorAlive = alive.Any(p => p.role?.roleType == RoleType.Emperor);
        if (!emperorAlive)
        {
            EndGame("Phe Phản thần chiến thắng! Hoàng đế đã bị hạ.");
            return;
        }

        // RedDevil: wins if ≤2 alive and RedDevil is one of them
        Player redDevil = alive.FirstOrDefault(p => p.role?.roleType == RoleType.RedDevil);
        if (redDevil != null && alive.Count <= 2)
        {
            // Farmer cannot share victory with RedDevil
            EndGame("Quỷ đỏ chiến thắng!");
            return;
        }

        // Rebels all dead (Phản thần + Thích khách both belong to Rebel faction)
        bool anyRebelAlive = alive.Any(p => p.role?.faction == Faction.Rebel);

        if (!anyRebelAlive)
        {
            // Emperor side wins
            string farmerResult = "";
            Player farmer = alive.FirstOrDefault(p => p.role?.roleType == RoleType.Farmer);
            if (farmer != null)
                farmerResult = " Nông dân cũng chiến thắng!";

            EndGame("Phe Hoàng đế chiến thắng!" + farmerResult);
            return;
        }

        // Only 1 person left (edge case)
        if (alive.Count <= 1)
        {
            EndGame(alive.Count == 1 ? alive[0].playerName + " chiến thắng!" : "Hòa — tất cả đã bị hạ.");
        }
    }

    void EndGame(string result)
    {
        gamePhase = GamePhase.GameOver;
        winnerText = result;
        LogEvent("══ KẾT THÚC: " + result + " ══");
        uiManager?.ShowGameOver(result);
    }

    // ─────────────────────────────────────────────
    // EVENT CARD — kích hoạt ngay khi bốc
    // ─────────────────────────────────────────────

    /// <summary>
    /// Gọi bởi DeckManager khi bốc trúng lá Event.
    /// Lá được kích hoạt ngay lập tức thay vì vào tay người chơi.
    /// drawer là người bốc (dùng làm "owner" cho các effect cần chủ thể).
    /// </summary>
    public void TriggerEventCard(Player drawer, CardData card)
    {
        if (card.cardType != CardType.Event) return;

        LogEvent($"⚡ {drawer.playerName} bốc trúng sự kiện: [{card.cardName}] — kích hoạt ngay!");

        // Hiện popup thông báo UI
        uiManager?.ShowEventNotification(card);

        // Lưu currentPlayer tạm để CardEffectExecutor dùng đúng owner
        int savedIndex = currentPlayerIndex;
        currentPlayerIndex = players.IndexOf(drawer);
        if (currentPlayerIndex < 0) currentPlayerIndex = savedIndex;

        CardEffectExecutor.Execute(drawer, card);

        currentPlayerIndex = savedIndex;

        CheckWinConditionsPublic();
        RefreshAll();
    }

    // ─────────────────────────────────────────────
    // CARD ACTIONS
    // ─────────────────────────────────────────────

    public void DrawCard()
    {
        if (gamePhase != GamePhase.Playing) return;
        if (!IsCurrentPlayerHuman) return;

        Player p = CurrentPlayer();
        if (p.cardsDrawnThisTurn >= 2)
        {
            LogEvent("Đã bốc tối đa 2 lá trong lượt này.");
            return;
        }
        if (p.hand.Count >= Player.MaxHandSize)
        {
            LogEvent($"Tay bài đầy (tối đa {Player.MaxHandSize} lá).");
            return;
        }
        if (deckManager == null) return;

        int handBefore = p.hand.Count;
        bool drew = deckManager.DealTo(p);
        if (drew)
        {
            p.cardsDrawnThisTurn++;
            bool gotCard = p.hand.Count > handBefore;
            if (gotCard)
                LogEvent($"{p.playerName} bốc 1 lá. (còn {2 - p.cardsDrawnThisTurn} lần bốc/lượt, deck: {deckManager.DrawPileCount})");
        }
        else
        {
            LogEvent("Hết bài trong deck.");
        }
        RefreshAll();
    }

    public void PlayCard(CardData card)
    {
        if (gamePhase != GamePhase.Playing) return;
        Player p = CurrentPlayer();

        if (!p.hand.Contains(card)) return;
        if (p.hasUsedActionThisTurn && card.cardType == CardType.Action)
        {
            LogEvent("Đã dùng hành động trong lượt này.");
            return;
        }

        bool ok = CardEffectExecutor.Execute(p, card);
        if (!ok) return;

        // Remove from hand (items go to equippedItems, handled by executor)
        if (card.cardType != CardType.Item)
            deckManager?.PlayCard(p, card);
        else
        {
            p.hand.Remove(card);
            // Item already added to equippedItems inside executor
        }

        if (card.cardType == CardType.Action)
            p.hasUsedActionThisTurn = true;

        RefreshAll();
    }

    // Called by CardEffectExecutor for effects that need win-check
    public void CheckWinConditionsPublic() => CheckWinConditions();

    // ─────────────────────────────────────────────
    // TARGET SELECTION
    // ─────────────────────────────────────────────

    // Khi đang preview lá HiddenAction, cho phép chọn chính mình làm target
    public bool selfTargetAllowed = false;
    public void SetSelfTargetAllowed(bool allowed) => selfTargetAllowed = allowed;

    public void SelectTarget(int index)
    {
        if (!selfTargetAllowed && index == currentPlayerIndex) return;
        if (!players[index].isAlive) return;

        selectedTargetIndex = index;
        string name = players[index].playerName;
        LogEvent("Chọn mục tiêu: " + (index == currentPlayerIndex ? name + " (bản thân)" : name));
        uiManager?.HighlightTarget(index);
    }

    // ─────────────────────────────────────────────
    // UI
    // ─────────────────────────────────────────────

    void CreatePlayerPanels()
    {
        // Clear old panels
        foreach (Transform child in playerPanelContainer)
            Destroy(child.gameObject);
        playerPanels.Clear();

        // Fallback: try loading prefab from Resources if not assigned in Inspector
        if (playerPanelPrefab == null)
            playerPanelPrefab = Resources.Load<GameObject>("Prefabs/PlayerPanel");

        if (playerPanelPrefab == null)
        {
            Debug.LogError("[GameManager] playerPanelPrefab is not assigned! Run Game > Setup > Create PlayerPanel Prefab.");
            return;
        }

        for (int i = 0; i < players.Count; i++)
        {
            GameObject panelObj = Instantiate(playerPanelPrefab, playerPanelContainer);
            PlayerPanelUI panelUI = panelObj.GetComponent<PlayerPanelUI>();
            panelUI.Setup(players[i], i);
            playerPanels.Add(panelUI);
        }
    }

    public void UpdateAllPlayerPanels()
    {
        // Auto-deselect dead targets
        if (selectedTargetIndex >= 0 && selectedTargetIndex < players.Count
            && !players[selectedTargetIndex].isAlive)
        {
            selectedTargetIndex = -1;
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (i < playerPanels.Count)
                playerPanels[i].Refresh(players[i], i == currentPlayerIndex, i == selectedTargetIndex);
        }
    }

    public void RefreshAll()
    {
        UpdateAllPlayerPanels();
        uiManager?.RefreshCurrentPlayer(CurrentPlayer(), currentRound);
    }

    public void LogEvent(string message)
    {
        Debug.Log("[Game] " + message);
        uiManager?.AppendLog(message);
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns true nếu Cấm quân còn sống trong ván.
    /// Khi còn Cấm quân, Hoàng đế không thể bị tấn công bằng đòn đánh thường.
    /// Nếu ván không có Cấm quân (4 người chơi), Hoàng đế không có shield.
    /// </summary>
    public bool IsEmperorShielded()
    {
        bool guardExistsInGame = players.Any(p => p.role?.roleType == RoleType.Guard);
        if (!guardExistsInGame) return false;

        return players.Any(p => p.isAlive && p.role?.roleType == RoleType.Guard);
    }

    public static string GetRoleName(RoleType rt)
    {
        return rt switch
        {
            RoleType.Emperor => "Hoàng đế",
            RoleType.Queen => "Hoàng hậu",
            RoleType.Guard => "Cấm quân",
            RoleType.Judge => "Quan án",
            RoleType.Rebel => "Phản thần",
            RoleType.Assassin => "Thích khách",
            RoleType.Farmer => "Nông dân",
            RoleType.RedDevil => "Quỷ đỏ",
            _ => rt.ToString()
        };
    }
}
