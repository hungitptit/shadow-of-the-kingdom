public class HiddenAction
{
    public Player owner;
    public Player target;
    public int placedRound;

    public HiddenAction(Player owner, Player target, int round)
    {
        this.owner = owner;
        this.target = target;
        this.placedRound = round;
    }
}
