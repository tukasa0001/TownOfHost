using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Nekomata : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Nekomata),
            player => new Nekomata(player),
            CustomRoles.Nekomata,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            40200,
            null,
            "猫又",
            "#00ffff"
        );
    public Nekomata(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}