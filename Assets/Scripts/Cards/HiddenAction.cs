using UnityEngine;

public enum SecretType { Assassinate, Protect }

public class HiddenAction
{
    public Player owner;
    public Player target;
    public int placedRound;
    public SecretType secretType;
    public Sprite artwork;      // ảnh mặt trước lá secret (hiện khi kích hoạt)

    public HiddenAction(Player owner, Player target, int round,
                        SecretType type, Sprite art = null)
    {
        this.owner      = owner;
        this.target     = target;
        this.placedRound = round;
        this.secretType  = type;
        this.artwork     = art;
    }
}
