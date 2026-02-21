using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Attached to each card prefab in the player's hand.
/// Handles display and click-to-play logic.
/// </summary>
[RequireComponent(typeof(Button))]
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visuals")]
    public Image artworkImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public Image typeColorBar;          // top bar tinted by card type
    public GameObject selectedGlow;     // bright outline when hovered
    public Image cardBackground;

    // Card type tint colors
    static readonly Color ColorItem    = new Color(0.2f, 0.7f, 0.3f);   // green
    static readonly Color ColorAction  = new Color(0.2f, 0.4f, 0.9f);   // blue
    static readonly Color ColorHidden  = new Color(0.5f, 0.1f, 0.7f);   // purple
    static readonly Color ColorEvent   = new Color(0.9f, 0.5f, 0.1f);   // orange

    CardData _card;

    static string TypeLabel(CardType t) => t switch
    {
        CardType.Item         => "Vật phẩm",
        CardType.Action       => "Hành động",
        CardType.HiddenAction => "Hành động ẩn",
        CardType.Event        => "Sự kiện",
        _                    => ""
    };

    static Color TypeColor(CardType t) => t switch
    {
        CardType.Item         => ColorItem,
        CardType.Action       => ColorAction,
        CardType.HiddenAction => ColorHidden,
        CardType.Event        => ColorEvent,
        _                    => Color.gray
    };

    public void Setup(CardData card)
    {
        _card = card;

        if (cardNameText != null)    cardNameText.text    = card.cardName;
        if (descriptionText != null) descriptionText.text = card.description;
        if (artworkImage != null && card.artwork != null)
        {
            artworkImage.sprite = card.artwork;
            artworkImage.color  = Color.white;
        }

        // Stamina cost badge — hide for Items
        if (costText != null)
        {
            bool isItem = card.cardType == CardType.Item;
            costText.transform.parent.gameObject.SetActive(!isItem);
            if (!isItem) costText.text = card.staminaCost.ToString();
        }

        // Type color bar tint
        if (typeColorBar != null)
            typeColorBar.color = TypeColor(card.cardType);

        // Type label text (bottom-left child named "TypeLabel")
        var typeLabelTMP = transform.Find("TypeLabel")?.GetComponent<TextMeshProUGUI>();
        if (typeLabelTMP != null)
        {
            typeLabelTMP.text  = TypeLabel(card.cardType);
            typeLabelTMP.color = TypeColor(card.cardType);
        }

        // Wire button
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }

        if (selectedGlow != null) selectedGlow.SetActive(false);
    }

    /// <summary>
    /// Hiển thị mặt sau lá bài (dùng cho bài của người chơi khác).
    /// Ảnh mặt sau phủ toàn bộ card, ẩn tất cả thông tin, không cho click.
    /// </summary>
    public void SetupFaceDown(Sprite cardBackSprite)
    {
        _card = null;

        // Ẩn type bar (GameObject)
        if (typeColorBar != null)
            typeColorBar.gameObject.SetActive(false);

        // Ẩn cost badge (GameObject cha)
        if (costText != null)
            costText.transform.parent.gameObject.SetActive(false);

        // Ẩn description
        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);

        // Ẩn type label
        var typeLabelGO = transform.Find("TypeLabel");
        if (typeLabelGO != null)
            typeLabelGO.gameObject.SetActive(false);

        // Stretch artwork phủ toàn bộ card
        if (artworkImage != null)
        {
            artworkImage.sprite        = cardBackSprite;
            artworkImage.color         = Color.white;
            artworkImage.preserveAspect = false;

            var rt = artworkImage.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Không cho click, không glow
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.interactable = false;
        }

        if (selectedGlow != null) selectedGlow.SetActive(false);
    }

    void OnClick()
    {
        if (_card == null) return;
        GameManager.Instance?.PlayCard(_card);
    }

    public void OnPointerEnter(PointerEventData data)
    {
        if (selectedGlow != null) selectedGlow.SetActive(true);
    }

    public void OnPointerExit(PointerEventData data)
    {
        if (selectedGlow != null) selectedGlow.SetActive(false);
    }
}
