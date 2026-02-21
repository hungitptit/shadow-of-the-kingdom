using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI playerInfoText;
    public Image roleImage;

    public void RefreshUI(Player player)
    {
        string roleText = "Hidden";

        if (player.isRevealed)
        {
            roleText = player.role.roleType.ToString();
            roleImage.sprite = player.role.cardImage;
            roleImage.gameObject.SetActive(true);
        }
        else
        {
            roleImage.gameObject.SetActive(false);
        }

        playerInfoText.text =
            "Current: " + player.playerName +
            "\nHP: " + player.hp +
            "\nStamina: " + player.stamina +
            "\nRole: " + roleText;
    }
}
