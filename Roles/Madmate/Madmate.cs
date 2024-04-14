using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Madmate;
public sealed class Madmate : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Madmate),
            player => new Madmate(player),
            CustomRoles.Madmate,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            5000,
            SetupOptionItem,
            "マッドメイト",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public Madmate(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public static void SetupOptionItem()
    {
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 10, RoleInfo.RoleName, RoleInfo.Tab);
    }
}
