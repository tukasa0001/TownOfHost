using TownOfHost.Extensions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Roles;

namespace TownOfHost.Managers;

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