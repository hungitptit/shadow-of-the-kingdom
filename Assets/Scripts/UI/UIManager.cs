using UnityEngine;
using UnityEngine.SceneManagement;
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
    public Button drawCardButton;

    [Header("Card Hand Area")]
    public Transform handContainer;        // HorizontalLayoutGroup parent for card slots
    public GameObject cardUIPrefab;        // CardUI prefab
    public TextMeshProUGUI deckCountText;  // "Deck: XX"
    public Sprite cardBackSprite;          // Mặt sau lá bài (roles/general.png)

    [Header("Peek Overlay")]
    public GameObject peekOverlay;         // Panel shown by ActionFortune
    public Transform peekContainer;        // 3 card slots inside peek overlay

    [Header("Target Info")]
    public TextMeshProUGUI targetInfoText;

    [Header("Navigation Buttons")]
    public Button mainMenuButton;

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
        if (drawCardButton != null)
            drawCardButton.onClick.AddListener(() => GameManager.Instance.DrawCard());

        // Navigation buttons — wired by name if not assigned in Inspector
        WireNavButtons();

        // Wire peek close button — find by name since it's inside the overlay
        Button peekCloseBtn = null;
        if (peekOverlay != null)
        {
            peekOverlay.SetActive(false);
            var peekCloseBtnGO = peekOverlay.transform.Find("PeekCloseButton");
            if (peekCloseBtnGO != null)
                peekCloseBtn = peekCloseBtnGO.GetComponent<Button>();
        }
        // Fallback: search whole scene
        if (peekCloseBtn == null)
        {
            var go = GameObject.Find("PeekCloseButton");
            if (go != null) peekCloseBtn = go.GetComponent<Button>();
        }
        if (peekCloseBtn != null)
        {
            peekCloseBtn.onClick.RemoveAllListeners();
            peekCloseBtn.onClick.AddListener(HidePeekOverlay);
        }
    }

    void WireNavButtons()
    {
        if (mainMenuButton == null)
        {
            var go = GameObject.Find("TopBarMainMenuButton");
            if (go != null) mainMenuButton = go.GetComponent<Button>();
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        // GameOver panel Main Menu button
        var goMainMenuGO = GameObject.Find("GameOverMainMenuButton");
        if (goMainMenuGO != null)
        {
            var btn = goMainMenuGO.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(GoToMainMenu);
        }
    }

    void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
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

        // Role display: show if publicly revealed OR if it's the human's own card
        bool canSeeRole = player.isRevealed || player.isSelfKnown;

        if (roleText != null)
        {
            if (canSeeRole && player.role != null)
                roleText.text = GameManager.GetRoleName(player.role.roleType) +
                                (player.isSelfKnown && !player.isRevealed ? " (bí mật)" : "");
            else
                roleText.text = "Vai trò: Ẩn";
        }

        if (roleImage != null)
        {
            if (canSeeRole && player.role?.cardImage != null)
            {
                roleImage.sprite = player.role.cardImage;
                roleImage.gameObject.SetActive(true);
            }
            else
            {
                roleImage.gameObject.SetActive(false);
            }
        }

        // Buttons only interactable when it's the human's turn
        bool isPlaying = GameManager.Instance.gamePhase == GamePhase.Playing;
        bool isHumanTurn = GameManager.Instance.IsCurrentPlayerHuman;

        if (attackButton != null)
            attackButton.interactable = isPlaying && isHumanTurn && !player.hasAttackedThisTurn && player.stamina >= 3;
        if (placeHiddenButton != null)
            placeHiddenButton.interactable = isPlaying && isHumanTurn && !player.hasUsedActionThisTurn;
        if (revealRoleButton != null)
            revealRoleButton.interactable = isPlaying && isHumanTurn && !player.isRevealed;
        if (endTurnButton != null)
            endTurnButton.interactable = isPlaying && isHumanTurn;
        if (drawCardButton != null)
            drawCardButton.interactable = isPlaying && isHumanTurn && !player.hasDrawnThisTurn
                                          && player.hand.Count < Player.MaxHandSize;

        // Refresh hand display
        RefreshHand(player);

        // Deck count
        if (deckCountText != null && DeckManager.Instance != null)
            deckCountText.text = $"Deck: {DeckManager.Instance.DrawPileCount}";
    }

    void RefreshHand(Player player)
    {
        if (handContainer == null || cardUIPrefab == null) return;

        // Clear existing
        foreach (Transform child in handContainer)
            Destroy(child.gameObject);

        // Người chơi human luôn thấy mặt trước bài của mình.
        // Current player là AI hoặc người khác → hiện mặt sau.
        bool isHumanOwner = GameManager.Instance.IsCurrentPlayerHuman;

        foreach (CardData card in player.hand)
        {
            GameObject go = Object.Instantiate(cardUIPrefab, handContainer);
            CardUI ui = go.GetComponent<CardUI>();
            if (ui == null) continue;

            if (isHumanOwner)
                ui.Setup(card);
            else
                ui.SetupFaceDown(GetCardBack());
        }
    }

    Sprite GetCardBack()
    {
        if (cardBackSprite != null) return cardBackSprite;
        // Fallback: tải từ Resources nếu chưa assign trong Inspector
        cardBackSprite = Resources.Load<Sprite>("Sprites/roles/general");
        return cardBackSprite;
    }

    public void ShowPeekCards(List<CardData> cards)
    {
        if (peekOverlay == null) return;
        peekOverlay.SetActive(true);

        if (peekContainer == null) return;
        foreach (Transform child in peekContainer) Destroy(child.gameObject);

        foreach (CardData c in cards)
        {
            if (cardUIPrefab == null) break;
            GameObject go = Object.Instantiate(cardUIPrefab, peekContainer);
            CardUI ui = go.GetComponent<CardUI>();
            ui?.Setup(c);
        }
    }

    public void HidePeekOverlay()
    {
        if (peekOverlay != null) peekOverlay.SetActive(false);
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
