using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Current Player Info")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;
    public TextMeshProUGUI roleText;
    public TextMeshProUGUI roundText;
    public Image roleImage;

    [Header("Game Log")]
    public TextMeshProUGUI logText;
    private List<string> logLines = new List<string>();
    private const int MaxLogLines = 12;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;

    [Header("Action Buttons")]
    public Button attackButton;
    public Button endTurnButton;
    public Button placeHiddenButton;
    public Button activateHiddenButton;
    public Button revealRoleButton;

    [Header("Target Info")]
    public TextMeshProUGUI targetInfoText;

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Wire buttons
        if (attackButton != null)
            attackButton.onClick.AddListener(() => GameManager.Instance.AttackSelected());
        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(() => GameManager.Instance.EndTurn());
        if (placeHiddenButton != null)
            placeHiddenButton.onClick.AddListener(() => GameManager.Instance.PlaceHiddenAction());
        if (activateHiddenButton != null)
            activateHiddenButton.onClick.AddListener(() => GameManager.Instance.ActivateHiddenAction());
        if (revealRoleButton != null)
            revealRoleButton.onClick.AddListener(() => GameManager.Instance.RevealCurrentPlayerRole());
    }

    public void RefreshCurrentPlayer(Player player, int round)
    {
        if (player == null) return;

        if (roundText != null)
            roundText.text = "Vòng " + round;

        if (playerNameText != null)
            playerNameText.text = player.playerName;
        if (hpText != null)
            hpText.text = "Khí huyết: " + player.hp;
        if (staminaText != null)
            staminaText.text = "Thể lực: " + player.stamina;
        if (attackText != null)
            attackText.text = "Tấn công: " + player.attack;
        if (defenseText != null)
            defenseText.text = "Phòng thủ: " + player.defense;

        if (roleText != null)
        {
            if (player.isRevealed && player.role != null)
                roleText.text = GameManager.GetRoleName(player.role.roleType);
            else
                roleText.text = "Vai trò: Ẩn";
        }

        if (roleImage != null)
        {
            if (player.isRevealed && player.role?.cardImage != null)
            {
                roleImage.sprite = player.role.cardImage;
                roleImage.gameObject.SetActive(true);
            }
            else
            {
                roleImage.gameObject.SetActive(false);
            }
        }

        // Update button interactability
        bool isPlaying = GameManager.Instance.gamePhase == GamePhase.Playing;
        if (attackButton != null)
            attackButton.interactable = isPlaying && !player.hasAttackedThisTurn && player.stamina >= 3;
        if (placeHiddenButton != null)
            placeHiddenButton.interactable = isPlaying && !player.hasUsedActionThisTurn;
        if (revealRoleButton != null)
            revealRoleButton.interactable = isPlaying && !player.isRevealed;
        if (endTurnButton != null)
            endTurnButton.interactable = isPlaying;
    }

    public void HighlightTarget(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= GameManager.Instance.players.Count) return;
        Player target = GameManager.Instance.players[targetIndex];

        if (targetInfoText != null)
        {
            targetInfoText.text = $"Mục tiêu: {target.playerName}\n" +
                                  $"HP: {target.hp}  Phòng thủ: {target.defense}";
        }
    }

    public void AppendLog(string message)
    {
        logLines.Add(message);
        if (logLines.Count > MaxLogLines)
            logLines.RemoveAt(0);

        if (logText != null)
            logText.text = string.Join("\n", logLines);
    }

    public void ShowGameOver(string result)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverText != null)
                gameOverText.text = result;
        }
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        logLines.Clear();
        if (logText != null)
            logText.text = "";

        if (targetInfoText != null)
            targetInfoText.text = "Chưa chọn mục tiêu";
    }
}
