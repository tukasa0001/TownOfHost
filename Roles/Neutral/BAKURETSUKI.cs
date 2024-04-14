using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Neutral;
public sealed class BAKURETSUKI : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(BAKURETSUKI),
            player => new BAKURETSUKI(player),
            CustomRoles.BAKURETSUKI,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            221100,
            null,
            "頭のおかしい爆裂船員",
            "#a0522d",
            true,
            countType: CountTypes.SB
        );
    public BAKURETSUKI(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
}