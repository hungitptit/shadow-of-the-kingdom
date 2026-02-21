using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public List<RoleData> roles;
    public List<Player> players = new List<Player>();
    private List<PlayerPanelUI> playerPanels = new List<PlayerPanelUI>();

    public int currentPlayerIndex = 0;
    public int currentRound = 1;
    public int selectedTargetIndex = -1;
    public Transform playerPanelContainer;
    public GameObject playerPanelPrefab;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SetupGame();
        CreatePlayerPanels();
        UpdateAllPlayerPanels();
    }

    void SetupGame()
    {
        players.Add(new Player("Player 1"));
        players.Add(new Player("Player 2"));
        players.Add(new Player("Player 3"));
        players.Add(new Player("Player 4"));

        for (int i = 0; i < players.Count; i++)
        {
            players[i].role = roles[i];
        }
    }

    public Player CurrentPlayer()
    {
        return players[currentPlayerIndex];
    }

    // Attack a player and deal damage to them
    public void AttackSelected()
    {
        if (selectedTargetIndex == -1) return;

        Player attacker = CurrentPlayer();
        Player target = players[selectedTargetIndex];

        if (!attacker.isAlive || !target.isAlive) return;
        if (attacker.stamina < 3) return;

        attacker.stamina -= 3;

        int damage = attacker.attack - target.defense;
        if (damage < 0) damage = 0;

        target.hp -= damage;

        if (target.hp <= 0)
        {
            target.isAlive = false;
            target.isRevealed = true;
        }

        UpdateAllPlayerPanels();
    }


    // End the turn and move to the next player
    public void EndTurn()
    {
        currentPlayerIndex++;

        // If the current player is the last player, end the round
        if (currentPlayerIndex >= players.Count)
        {
            currentPlayerIndex = 0;
            EndRound();
        }

        UpdateAllPlayerPanels();
    }

    // End the round and reset the stamina of the players
    void EndRound()
    {
        currentRound++;

        foreach (Player p in players)
        {
            if (p.isAlive)
            {
                p.stamina += 1;
            }
            foreach (HiddenAction action in p.hiddenActionsOnMe.ToArray())
            {
                if (currentRound > action.placedRound)
                {
                    if (action.owner.stamina >= 5)
                    {
                        action.owner.stamina -= 5;
                        p.hp = 0;
                        p.isAlive = false;
                        p.isRevealed = true;
                        p.hiddenActionsOnMe.Remove(action);
                    }
                }
            }
        }

        Debug.Log("Round: " + currentRound);
    }


    public UIManager uiManager;

    void UpdateUI()
    {
        uiManager.RefreshUI(CurrentPlayer());
    }

    public void SelectTarget(int index)
    {
        if (index == currentPlayerIndex) return;
        selectedTargetIndex = index;
        Debug.Log("Selected target: " + players[index].playerName);
    }
    void CreatePlayerPanels()
    {
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
        for (int i = 0; i < players.Count; i++)
        {
            Player p = players[i];
            PlayerPanelUI panel = playerPanels[i];

            panel.nameText.text = p.playerName;
            panel.hpText.text = "HP: " + p.hp;
            panel.staminaText.text = "ST: " + p.stamina;

            bool isCurrent = (i == currentPlayerIndex);
            panel.Highlight(isCurrent);
        }
    }
    
    // Place a hidden action on a player
    public void PlaceHidden()
    {
        if (selectedTargetIndex == -1) return;

        Player owner = CurrentPlayer();
        Player target = players[selectedTargetIndex];

        HiddenAction action = new HiddenAction(owner, target, currentRound);
        target.hiddenActionsOnMe.Add(action);

        Debug.Log("Hidden placed on " + target.playerName);
    }
    }
