using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;
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
    public Button activateSecretButton;  // Kích hoạt Ám sát bí mật (5 ST)
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

    [Header("Card Preview Panel")]
    public GameObject      cardPreviewPanel;
    public Image           cpArtworkImage;
    public TextMeshProUGUI cpCardNameText;
    public TextMeshProUGUI cpDescText;
    public TextMeshProUGUI cpCostText;
    public Image           cpTypeBar;
    public TextMeshProUGUI cpTypeLabel;
    public Button          cpConfirmButton;
    public Button          cpCancelButton;
    public GameObject      cpTargetSection; // ẩn/hiện toàn bộ vùng chọn target
    public Transform       cpTargetList;    // HorizontalLayoutGroup chứa nút target

    // Lá bài đang chờ xác nhận
    CardData _pendingCard;

    [Header("Protect Confirm Overlay")]
    public GameObject protectConfirmPanel;
    public Button     protectYesButton;
    public Button     protectNoButton;

    [Header("Event Notification Overlay")]
    public GameObject eventNotificationPanel;
    public Image           eventCardArtImage;
    public TextMeshProUGUI eventCardNameText;
    public TextMeshProUGUI eventCardDescText;

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
        if (activateSecretButton != null)
            activateSecretButton.onClick.AddListener(() => GameManager.Instance.ActivateSecretCard());
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

        // Kích hoạt ám sát: phải có lá ám sát đặt từ vòng trước, đủ 5 ST
        if (activateSecretButton != null)
        {
            bool hasAssassin = GameManager.Instance.selectedTargetIndex >= 0 &&
                GameManager.Instance.selectedTargetIndex < GameManager.Instance.players.Count &&
                GameManager.Instance.players[GameManager.Instance.selectedTargetIndex]
                    .hiddenActionsOnMe.Exists(a =>
                        a.owner == player &&
                        a.secretType == SecretType.Assassinate &&
                        GameManager.Instance.currentRound > a.placedRound);
            activateSecretButton.interactable = isPlaying && isHumanTurn && player.stamina >= 5 && hasAssassin;
        }

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

    // ── Event Notification ───────────────────────────────────────

    Coroutine _eventNotifCoroutine;

    /// <summary>
    /// Hiện popup thông báo lá Event bốc được, tự ẩn sau <paramref name="duration"/> giây.
    /// </summary>
    public void ShowEventNotification(CardData card, float duration = 2.5f)
    {
        if (eventNotificationPanel == null) return;

        if (eventCardNameText != null)
            eventCardNameText.text = card.cardName;

        if (eventCardDescText != null)
            eventCardDescText.text = card.description;

        if (eventCardArtImage != null)
        {
            eventCardArtImage.sprite = card.artwork;
            eventCardArtImage.gameObject.SetActive(card.artwork != null);
        }

        eventNotificationPanel.SetActive(true);

        if (_eventNotifCoroutine != null)
            StopCoroutine(_eventNotifCoroutine);
        _eventNotifCoroutine = StartCoroutine(HideEventNotifAfter(duration));
    }

    IEnumerator HideEventNotifAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (eventNotificationPanel != null)
            eventNotificationPanel.SetActive(false);
        _eventNotifCoroutine = null;
    }

    // ── Card Preview ─────────────────────────────────────────────

    static readonly Color ColorItem   = new Color(0.2f, 0.7f, 0.3f);
    static readonly Color ColorAction = new Color(0.2f, 0.4f, 0.9f);
    static readonly Color ColorHidden = new Color(0.5f, 0.1f, 0.7f);
    static readonly Color ColorEvent  = new Color(0.9f, 0.5f, 0.1f);

    static Color TypeColorFor(CardType t) => t switch
    {
        CardType.Item         => ColorItem,
        CardType.Action       => ColorAction,
        CardType.HiddenAction => ColorHidden,
        CardType.Event        => ColorEvent,
        _                    => Color.gray
    };

    static string TypeLabelFor(CardType t) => t switch
    {
        CardType.Item         => "Vật phẩm",
        CardType.Action       => "Hành động",
        CardType.HiddenAction => "Hành động bí mật",
        CardType.Event        => "Sự kiện",
        _                    => ""
    };

    /// <summary>
    /// Mở panel phóng to lá bài.
    /// - Nếu lá cần target: hiện danh sách nút người chơi ngay trong panel để chọn.
    /// - Lá HiddenAction cho phép chọn chính mình.
    /// </summary>
    public void ShowCardPreview(CardData card)
    {
        if (cardPreviewPanel == null)
        {
            GameManager.Instance?.PlayCard(card);
            return;
        }

        _pendingCard = card;

        // Điền thông tin lá bài
        if (cpCardNameText != null) cpCardNameText.text = card.cardName;
        if (cpDescText     != null) cpDescText.text     = card.description;

        Color typeColor = TypeColorFor(card.cardType);
        if (cpTypeBar   != null) cpTypeBar.color  = typeColor;
        if (cpTypeLabel != null) cpTypeLabel.text  = TypeLabelFor(card.cardType);

        if (cpArtworkImage != null)
        {
            cpArtworkImage.sprite = card.artwork;
            cpArtworkImage.gameObject.SetActive(card.artwork != null);
        }

        if (cpCostText != null)
        {
            bool hasCost = card.staminaCost > 0;
            cpCostText.transform.parent.gameObject.SetActive(hasCost);
            if (hasCost) cpCostText.text = card.staminaCost + " ST";
        }

        bool needsTarget = CardEffectExecutor.NeedsTarget(card);
        bool allowSelf   = card.cardType == CardType.HiddenAction;

        // Hiện / ẩn vùng chọn target
        if (cpTargetSection != null)
            cpTargetSection.SetActive(needsTarget);

        if (needsTarget)
            PopulateTargetList(card, allowSelf);

        // Reset target cũ để tránh chọn nhầm
        if (GameManager.Instance != null)
        {
            GameManager.Instance.selectedTargetIndex = -1;
            GameManager.Instance.SetSelfTargetAllowed(allowSelf);
        }

        cardPreviewPanel.SetActive(true);

        cpConfirmButton.onClick.RemoveAllListeners();
        cpCancelButton.onClick.RemoveAllListeners();
        cpConfirmButton.onClick.AddListener(ConfirmCardPlay);
        cpCancelButton.onClick.AddListener(CancelCardPreview);
    }

    void PopulateTargetList(CardData card, bool allowSelf)
    {
        if (cpTargetList == null) return;

        foreach (Transform child in cpTargetList)
            Destroy(child.gameObject);

        var gm = GameManager.Instance;
        if (gm == null || gm.playerPanelPrefab == null) return;

        bool isRevive = card.effectType == CardEffectType.ActionRevive;

        for (int i = 0; i < gm.players.Count; i++)
        {
            Player p = gm.players[i];
            int capturedIndex = i;
            bool isSelf = (i == gm.currentPlayerIndex);

            // Revive: chỉ hiện người chết; còn lại: chỉ hiện người sống
            if (isRevive  &&  p.isAlive)  continue;
            if (!isRevive && !p.isAlive)  continue;

            // Bỏ chính mình nếu không được phép
            if (isSelf && !allowSelf) continue;

            // Instantiate PlayerPanel prefab
            GameObject go = Instantiate(gm.playerPanelPrefab, cpTargetList);

            // Đặt tên để dễ debug
            go.name = "TargetOption_" + p.playerName;

            // Đảm bảo LayoutElement flexibleWidth = 1 để dãn đều
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth  = 1f;
            le.minWidth       = 60f;
            le.minHeight      = 80f;
            le.preferredWidth = -1;  // để HLG quyết định

            var ppUI = go.GetComponent<PlayerPanelUI>();
            if (ppUI != null)
            {
                // Tô màu khác nếu là bản thân để phân biệt
                if (isSelf && ppUI.background != null)
                    ppUI.colorDefault = new Color(0.15f, 0.30f, 0.50f, 0.95f);

                ppUI.SetupAsTargetOption(p, capturedIndex, _ => { });
            }
        }
    }

    void ConfirmCardPlay()
    {
        if (_pendingCard == null) return;
        var card = _pendingCard;
        _pendingCard = null;
        cardPreviewPanel.SetActive(false);
        GameManager.Instance?.SetSelfTargetAllowed(false);
        GameManager.Instance?.PlayCard(card);
    }

    public void CancelCardPreview()
    {
        _pendingCard = null;
        GameManager.Instance?.SetSelfTargetAllowed(false);
        if (GameManager.Instance != null)
            GameManager.Instance.selectedTargetIndex = -1;
        if (cardPreviewPanel != null) cardPreviewPanel.SetActive(false);
    }

    // ── Protect Confirm ──────────────────────────────────────────

    /// <summary>
    /// Hiện popup hỏi human target có muốn kích hoạt Bảo vệ bí mật không.
    /// Gọi callback(true) nếu chọn Kích hoạt, callback(false) nếu Bỏ qua.
    /// </summary>
    public void AskProtectConfirm(System.Action<bool> callback)
    {
        if (protectConfirmPanel == null)
        {
            // Không có UI → tự động kích hoạt
            callback(true);
            return;
        }

        protectConfirmPanel.SetActive(true);

        protectYesButton.onClick.RemoveAllListeners();
        protectNoButton.onClick.RemoveAllListeners();

        protectYesButton.onClick.AddListener(() =>
        {
            protectConfirmPanel.SetActive(false);
            callback(true);
        });

        protectNoButton.onClick.AddListener(() =>
        {
            protectConfirmPanel.SetActive(false);
            callback(false);
        });
    }
}
