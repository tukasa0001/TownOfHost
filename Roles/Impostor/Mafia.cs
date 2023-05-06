using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Impostor;
public sealed class Mafia : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Mafia),
            player => new Mafia(player),
            CustomRoles.Mafia,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            1600,
            null,
            "mf"
        );
    public Mafia(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public override bool CanUseKillButton()
    {
        if (PlayerState.AllPlayerStates == null) return false;
        //マフィアを除いた生きているインポスターの人数  Number of Living Impostors excluding mafia
        int livingImpostorsNum = 0;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var role = pc.GetCustomRole();
            if (role != CustomRoles.Mafia && role.IsImpostor()) livingImpostorsNum++;
        }

        return livingImpostorsNum <= 0;
    }
}