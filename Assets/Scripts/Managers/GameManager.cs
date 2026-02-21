using UnityEngine;
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
    public List<RoleData> allRoles;        // All role ScriptableObjects (assign in Inspector)

    [Header("Player Setup")]
    public int playerCount = 4;

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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupGame(playerCount);
    }

    // ─────────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────────

    public void SetupGame(int count)
    {
        players.Clear();
        playerPanels.Clear();
        currentPlayerIndex = 0;
        currentRound = 1;
        selectedTargetIndex = -1;
        gamePhase = GamePhase.Playing;
        winnerText = "";

        uiManager?.HideGameOver();

        // Create players
        for (int i = 0; i < count; i++)
        {
            players.Add(new Player("Player " + (i + 1)));
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
        {
            emperor.isRevealed = true;
        }

        turnsThisRound = 0;

        CreatePlayerPanels();
        RefreshAll();

        LogEvent("Ván chơi bắt đầu. Vòng 1.");
        LogEvent("Lượt: " + CurrentPlayer().playerName);
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

    public void EndTurn()
    {
        if (gamePhase != GamePhase.Playing) return;

        CurrentPlayer().ResetTurnFlags();
        selectedTargetIndex = -1;
        turnsThisRound++;

        int aliveBefore = AlivePlayers().Count;

        // Check if all alive players have taken their turn → end round
        if (turnsThisRound >= aliveBefore)
        {
            EndRound();
        }
        else
        {
            // Advance to next alive player
            AdvanceToNextAlive();
            RefreshAll();
            if (gamePhase == GamePhase.Playing)
                LogEvent("Lượt: " + CurrentPlayer().playerName);
        }
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

        // +1 stamina for all alive players
        foreach (Player p in players)
        {
            if (p.isAlive)
                p.stamina += 1;

            // Reset RedDevil immunity for new round
            p.redDevilImmunityUsedThisRound = false;
        }

        // Check Judge effect (after Round 6)
        CheckJudgeEffect();

        // Set turn to first alive player
        currentPlayerIndex = GetFirstAliveIndex();

        RefreshAll();

        if (gamePhase == GamePhase.Playing)
            LogEvent("Lượt: " + CurrentPlayer().playerName);
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

        attacker.stamina -= 3;
        attacker.hasAttackedThisTurn = true;

        int damage = attacker.attack - target.defense;
        if (damage < 0) damage = 0;

        // RedDevil immunity: ignore first attack each round if revealed
        if (target.isRevealed && target.role?.roleType == RoleType.RedDevil
            && !target.redDevilImmunityUsedThisRound)
        {
            target.redDevilImmunityUsedThisRound = true;
            LogEvent($"{attacker.playerName} tấn công {target.playerName} — Quỷ đỏ miễn nhiễm!");
            RefreshAll();
            return;
        }

        target.hp -= damage;
        LogEvent($"{attacker.playerName} tấn công {target.playerName}: -{damage} HP.");

        if (target.hp <= 0)
        {
            KillPlayer(target);
        }

        RefreshAll();
    }

    // ─────────────────────────────────────────────
    // HIDDEN ACTION
    // ─────────────────────────────────────────────

    public void PlaceHiddenAction()
    {
        if (gamePhase != GamePhase.Playing) return;
        if (selectedTargetIndex == -1) return;

        Player owner = CurrentPlayer();
        Player target = players[selectedTargetIndex];

        if (!owner.isAlive || !target.isAlive) return;
        if (owner.hasUsedActionThisTurn)
        {
            LogEvent("Đã dùng hành động trong lượt này.");
            return;
        }

        HiddenAction action = new HiddenAction(owner, target, currentRound);
        target.hiddenActionsOnMe.Add(action);
        owner.hiddenActionsPlacedByMe.Add(action);
        owner.hasUsedActionThisTurn = true;

        LogEvent($"{owner.playerName} đặt Ám sát ẩn lên {target.playerName}.");
        RefreshAll();
    }

    public void ActivateHiddenAction()
    {
        if (gamePhase != GamePhase.Playing) return;
        if (selectedTargetIndex == -1) return;

        Player activator = CurrentPlayer();
        Player target = players[selectedTargetIndex];

        // Find a hidden action placed by activator on target that is eligible
        HiddenAction action = target.hiddenActionsOnMe.FirstOrDefault(a =>
            a.owner == activator && currentRound > a.placedRound);

        if (action == null)
        {
            LogEvent("Không có Hành động Ẩn hợp lệ để kích hoạt.");
            return;
        }
        if (activator.stamina < 5)
        {
            LogEvent("Không đủ Thể lực (cần 5 để kích hoạt).");
            return;
        }

        activator.stamina -= 5;
        target.hiddenActionsOnMe.Remove(action);
        activator.hiddenActionsPlacedByMe.Remove(action);

        LogEvent($"{activator.playerName} kích hoạt Ám sát ẩn lên {target.playerName}!");
        KillPlayer(target);
        RefreshAll();
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

    void KillPlayer(Player p)
    {
        p.hp = 0;
        p.isAlive = false;
        bool wasRevealed = p.isRevealed;
        p.isRevealed = true;

        LogEvent($"{p.playerName} bị hạ! Vai trò: {GetRoleName(p.role.roleType)}");

        // Clean up hidden actions placed by this player
        foreach (HiddenAction a in p.hiddenActionsPlacedByMe.ToList())
        {
            a.target.hiddenActionsOnMe.Remove(a);
        }
        p.hiddenActionsPlacedByMe.Clear();

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
        uiManager.ShowGameOver(result);
    }

    // ─────────────────────────────────────────────
    // TARGET SELECTION
    // ─────────────────────────────────────────────

    public void SelectTarget(int index)
    {
        if (index == currentPlayerIndex) return;
        if (!players[index].isAlive) return;

        selectedTargetIndex = index;
        LogEvent("Chọn mục tiêu: " + players[index].playerName);
        uiManager.HighlightTarget(index);
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

    void RefreshAll()
    {
        UpdateAllPlayerPanels();
        uiManager.RefreshCurrentPlayer(CurrentPlayer(), currentRound);
    }

    public void LogEvent(string message)
    {
        Debug.Log("[Game] " + message);
        uiManager?.AppendLog(message);
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

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
