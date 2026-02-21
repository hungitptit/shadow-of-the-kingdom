using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerPanelUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI staminaText;
    public Button button;
    public Image background;

    private int playerIndex;

    public void Setup(Player player, int index)
    {
        playerIndex = index;

        nameText.text = player.playerName;
        hpText.text = "HP: " + player.hp;
        staminaText.text = "ST: " + player.stamina;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        GameManager.Instance.SelectTarget(playerIndex);
    }

    // Highlight the player panel
    public void Highlight(bool active)
    {
        if (background == null) return;

        background.color = active ? Color.green : Color.white;
    }
}
