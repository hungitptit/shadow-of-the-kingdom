/// <summary>
/// Static config passed between MainMenu → GameScene.
/// Survives scene loads because it's a plain static class.
/// </summary>
public static class GameConfig
{
    public enum GameMode { SinglePlayer, MultiPlayer }

    public static GameMode Mode       = GameMode.SinglePlayer;
    public static int      PlayerCount = 4;

    // Index of the human player (always 0 in single-player)
    public static int HumanPlayerIndex = 0;
}
