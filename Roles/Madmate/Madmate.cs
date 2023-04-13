using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;
public sealed class Madmate : RoleBase, IKillFlashSeeable
{
    public static SimpleRoleInfo RoleInfo =
        new(
            typeof(Madmate),
            player => new Madmate(player),
            CustomRoles.Madmate,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            10000,
            null
        );
    public Madmate(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
    }

    private static bool canSeeKillFlash;

    public bool CanSeeKillFlash(MurderInfo info) => canSeeKillFlash;
}
