using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Roles;

namespace TOHTOR.Player;

public class PlayerPlus
{
    public PlayerControl MyPlayer;
    public PlayerState State;
    public DynamicName DynamicName;
    public CustomRole Role;

    public PlayerPlus(PlayerControl player)
    {
        this.MyPlayer = player;
        this.State = PlayerState.Alive;
        this.DynamicName = DynamicName.For(player);
        this.Role = player.GetCustomRole();
    }
}