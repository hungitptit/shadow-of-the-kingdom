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
}
