using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class NekoKabocha : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(NekoKabocha),
            player => new NekoKabocha(player),
            CustomRoles.NekoKabocha,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3300,
            SetupOptionItems,
            "nk",
            introSound: () => PlayerControl.LocalPlayer.KillSfx
        );
    public NekoKabocha(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        revengeOnExile = optionRevengeOnExile.GetBool();
    }

    #region カスタムオプション
    private static BooleanOptionItem optionRevengeOnExile;
    private static void SetupOptionItems()
    {
        optionRevengeOnExile = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NekoKabochaRevengeOnExile, false, false);
    }
    private enum OptionName { NekoKabochaRevengeOnExile, }
    #endregion

    private static bool revengeOnExile;
}
