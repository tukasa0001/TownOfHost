using AmongUs.GameOptions;
using TownOfHost.Modules;
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
    private static readonly LogHandler logger = Logger.Handler(nameof(NekoKabocha));

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        // 普通のキルじゃない．もしくはキルを行わない時はreturn
        if (info.IsAccident || info.IsSuicide || !info.CanKill || !info.DoKill)
        {
            return;
        }
        // 殺してきた人を殺し返す
        logger.Info("ネコカボチャの仕返し");
        var killer = info.AttemptKiller;
        killer.SetRealKiller(Player);
        Player.RpcMurderPlayer(killer);
    }
}
