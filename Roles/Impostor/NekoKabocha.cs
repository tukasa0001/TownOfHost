using AmongUs.GameOptions;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class NekoKabocha : RoleBase, IImpostor, INekomata
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
        impostorsGetRevengedOnKill = optionImpostorsGetRevengedOnKill.GetBool();
        revengeOnExile = optionRevengeOnExile.GetBool();
        canImpostorsGetRevengedOnExile = optionCanImpostorsGetRevengedOnExile.GetBool();
    }

    #region カスタムオプション
    /// <summary>インポスターにキルされたときに仕返しするかどうか</summary>
    private static BooleanOptionItem optionImpostorsGetRevengedOnKill;
    private static BooleanOptionItem optionRevengeOnExile;
    private static BooleanOptionItem optionCanImpostorsGetRevengedOnExile;
    private static void SetupOptionItems()
    {
        optionImpostorsGetRevengedOnKill = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NekoKabochaImpostorsGetRevengedOnKill, false, false);
        optionRevengeOnExile = BooleanOptionItem.Create(RoleInfo, 20, OptionName.NekoKabochaRevengeOnExile, false, false);
        optionCanImpostorsGetRevengedOnExile = BooleanOptionItem.Create(RoleInfo, 21, OptionName.NekoKabochaCanImpostorsGetRevengedOnExile, false, false, optionRevengeOnExile);
    }
    private enum OptionName { NekoKabochaImpostorsGetRevengedOnKill, NekoKabochaRevengeOnExile, NekoKabochaCanImpostorsGetRevengedOnExile, }
    #endregion

    private static bool impostorsGetRevengedOnKill;
    private static bool revengeOnExile;
    private static bool canImpostorsGetRevengedOnExile;
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
        if (!impostorsGetRevengedOnKill && killer.Is(CustomRoleTypes.Impostor))
        {
            logger.Info("キラーがインポスターであるため仕返ししません");
            return;
        }
        killer.SetRealKiller(Player);
        Player.RpcMurderPlayer(killer);
    }
    public bool DoRevenge(CustomDeathReason deathReason) => revengeOnExile;
    public bool IsCandidate(PlayerControl player) => !player.Is(CustomRoleTypes.Impostor) || canImpostorsGetRevengedOnExile;
}
