using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerPanelUI : MonoBehaviour
{
    [Header("Text Fields")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI staminaText;
    public TextMeshProUGUI atkDefText;   // shows ATK/DEF
    public TextMeshProUGUI roleNameText; // shows role name if revealed

    [Header("Visuals")]
    public Button button;
    public Image background;
    public Image roleCardImage;          // small role card thumbnail
    public GameObject deadOverlay;       // dark overlay when player is dead
    public GameObject selectedIndicator; // ring/glow when targeted

    [Header("Secret Cards")]
    public Sprite cardBackSprite;        // mặt sau lá bài (general.png)
    // Badge hiện số lá secret đang đặt trên player này
    public GameObject secretBadge;       // container (Image nền + Text)
    public TextMeshProUGUI secretCountText;

    [Header("Colors")]
    public Color colorDefault = new Color(0.15f, 0.15f, 0.25f, 0.9f);
    public Color colorActive = new Color(0.2f, 0.6f, 0.2f, 1f);
    public Color colorSelected = new Color(0.7f, 0.5f, 0.1f, 1f);
    public Color colorDead = new Color(0.3f, 0.1f, 0.1f, 0.6f);

    private int playerIndex;

    public void Setup(Player player, int index)
    {
        playerIndex = index;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        Refresh(player, false, false);
    }

    public void Refresh(Player player, bool isCurrentTurn, bool isSelected)
    {
        if (player == null) return;

        // Name
        if (nameText != null)
            nameText.text = player.playerName;

        // Stats
        if (hpText != null)
            hpText.text = $"HP {player.hp}";
        if (staminaText != null)
            staminaText.text = $"ST {player.stamina}";
        if (atkDefText != null)
            atkDefText.text = $"ATK {player.attack} / DEF {player.defense}";

        // Role (only show name if revealed)
        if (roleNameText != null)
        {
            if (player.isRevealed && player.role != null)
                roleNameText.text = GameManager.GetRoleName(player.role.roleType);
            else
                roleNameText.text = "???";
        }

        // Role card image
        if (roleCardImage != null)
        {
            if (player.isRevealed && player.role?.cardImage != null)
            {
                roleCardImage.sprite = player.role.cardImage;
                roleCardImage.gameObject.SetActive(true);
            }
            else
            {
                roleCardImage.gameObject.SetActive(false);
            }
        }

        // Dead overlay
        if (deadOverlay != null)
            deadOverlay.SetActive(!player.isAlive);

        // Selected indicator
        if (selectedIndicator != null)
            selectedIndicator.SetActive(isSelected);

        // Background color
        if (background != null)
        {
            if (!player.isAlive)
                background.color = colorDead;
            else if (isCurrentTurn)
                background.color = colorActive;
            else if (isSelected)
                background.color = colorSelected;
            else
                background.color = colorDefault;
        }

        // Disable button for dead/self
        if (button != null)
            button.interactable = player.isAlive;

        // Secret card badge — hiện số lá đang đặt trên player này
        RefreshSecretBadge(player);
    }

    void RefreshSecretBadge(Player player)
    {
        int count = player.hiddenActionsOnMe.Count;
        if (secretBadge != null)
            secretBadge.SetActive(count > 0);
        if (secretCountText != null)
            secretCountText.text = count > 1 ? count.ToString() : "🂠";
    }

    void OnClick()
    {
        GameManager.Instance.SelectTarget(playerIndex);
    }

    // Legacy highlight method for backwards compat
    public void Highlight(bool active)
    {
        if (background == null) return;
        background.color = active ? colorActive : colorDefault;
    }

    /// <summary>
    /// Dùng trong Card Preview Panel: override OnClick để gọi callback thay vì SelectTarget toàn cục.
    /// Khi được chọn, tô màu selected; các panel khác trong cùng container reset về default.
    /// </summary>
    public void SetupAsTargetOption(Player player, int index, System.Action<int> onSelected)
    {
        playerIndex = index;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // Highlight tất cả panel cùng parent
                if (transform.parent != null)
                {
                    foreach (Transform sibling in transform.parent)
                    {
                        var ppUI = sibling.GetComponent<PlayerPanelUI>();
                        if (ppUI != null && ppUI.background != null)
                            ppUI.background.color = ppUI.colorDefault;
                    }
                }

                if (background != null)
                    background.color = colorSelected;

                GameManager.Instance?.SelectTarget(index);
                onSelected?.Invoke(index);
            });
        }

        Refresh(player, false, false);
    }
}
