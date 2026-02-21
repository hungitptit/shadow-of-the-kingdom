using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the Main Menu screen.
/// Lets player choose Single Player / Multiplayer and player count (4-9).
/// Supports both Inspector-assigned refs and auto-find by GameObject name as fallback.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Mode Buttons")]
    public Button btnSinglePlayer;
    public Button btnMultiPlayer;

    [Header("Player Count")]
    public TextMeshProUGUI playerCountLabel;
    public Button btnCountMinus;
    public Button btnCountPlus;

    [Header("Start")]
    public Button btnStart;

    [Header("Visuals")]
    public Image singlePlayerHighlight;
    public Image multiPlayerHighlight;

    private GameConfig.GameMode selectedMode = GameConfig.GameMode.SinglePlayer;
    private int selectedCount = 4;

    private static readonly Color SEL_COLOR   = new Color(0.2f, 0.55f, 0.2f, 1f);
    private static readonly Color UNSEL_COLOR = new Color(0.18f, 0.14f, 0.25f, 0.9f);

    void Start()
    {
        // Auto-find by name if Inspector refs are missing
        TryAutoFindRefs();

        if (btnSinglePlayer != null)
            btnSinglePlayer.onClick.AddListener(() => SelectMode(GameConfig.GameMode.SinglePlayer));
        if (btnMultiPlayer != null)
            btnMultiPlayer.onClick.AddListener(() => SelectMode(GameConfig.GameMode.MultiPlayer));
        if (btnCountMinus != null)
            btnCountMinus.onClick.AddListener(DecrementCount);
        if (btnCountPlus != null)
            btnCountPlus.onClick.AddListener(IncrementCount);
        if (btnStart != null)
            btnStart.onClick.AddListener(StartGame);

        SelectMode(GameConfig.GameMode.SinglePlayer);
        UpdateCountLabel();
    }

    void TryAutoFindRefs()
    {
        if (btnSinglePlayer == null)
            btnSinglePlayer = FindButton("BtnSinglePlayer");
        if (btnMultiPlayer == null)
            btnMultiPlayer = FindButton("BtnMultiPlayer");
        if (btnCountMinus == null)
            btnCountMinus = FindButton("BtnMinus");
        if (btnCountPlus == null)
            btnCountPlus = FindButton("BtnPlus");
        if (btnStart == null)
            btnStart = FindButton("BtnStart");

        if (playerCountLabel == null)
        {
            GameObject go = GameObject.Find("PlayerCountLabel");
            if (go != null) playerCountLabel = go.GetComponent<TextMeshProUGUI>();
        }

        if (singlePlayerHighlight == null && btnSinglePlayer != null)
            singlePlayerHighlight = btnSinglePlayer.GetComponent<Image>();
        if (multiPlayerHighlight == null && btnMultiPlayer != null)
            multiPlayerHighlight = btnMultiPlayer.GetComponent<Image>();
    }

    static Button FindButton(string goName)
    {
        GameObject go = GameObject.Find(goName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    void SelectMode(GameConfig.GameMode mode)
    {
        selectedMode = mode;

        if (singlePlayerHighlight != null)
            singlePlayerHighlight.color = mode == GameConfig.GameMode.SinglePlayer ? SEL_COLOR : UNSEL_COLOR;
        if (multiPlayerHighlight != null)
            multiPlayerHighlight.color = mode == GameConfig.GameMode.MultiPlayer ? SEL_COLOR : UNSEL_COLOR;
    }

    void DecrementCount()
    {
        if (selectedCount > 4) selectedCount--;
        UpdateCountLabel();
    }

    void IncrementCount()
    {
        if (selectedCount < 9) selectedCount++;
        UpdateCountLabel();
    }

    void UpdateCountLabel()
    {
        if (playerCountLabel != null)
            playerCountLabel.text = selectedCount.ToString();
    }

    void StartGame()
    {
        GameConfig.Mode              = selectedMode;
        GameConfig.PlayerCount       = selectedCount;
        GameConfig.HumanPlayerIndex  = 0;

        SceneManager.LoadScene("GameScene");
    }
}
