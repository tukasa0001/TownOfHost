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
        impostorsGetRevenged = optionImpostorsGetRevenged.GetBool();
        madmatesGetRevenged = optionMadmatesGetRevenged.GetBool();
        revengeOnExile = optionRevengeOnExile.GetBool();
    }

    #region カスタムオプション
    /// <summary>インポスターに仕返し/道連れするかどうか</summary>
    private static BooleanOptionItem optionImpostorsGetRevenged;
    /// <summary>マッドに仕返し/道連れするかどうか</summary>
    private static BooleanOptionItem optionMadmatesGetRevenged;
    private static BooleanOptionItem optionRevengeOnExile;
    private static void SetupOptionItems()
    {
        optionImpostorsGetRevenged = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NekoKabochaImpostorsGetRevenged, false, false);
        optionMadmatesGetRevenged = BooleanOptionItem.Create(RoleInfo, 20, OptionName.NekoKabochaMadmatesGetRevenged, false, false);
        optionRevengeOnExile = BooleanOptionItem.Create(RoleInfo, 30, OptionName.NekoKabochaRevengeOnExile, false, false);
    }
    private enum OptionName { NekoKabochaImpostorsGetRevenged, NekoKabochaMadmatesGetRevenged, NekoKabochaRevengeOnExile, }
    #endregion

    private static bool impostorsGetRevenged;
    private static bool madmatesGetRevenged;
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
        if (!IsCandidate(killer))
        {
            logger.Info("キラーは仕返し対象ではないので仕返しされません");
            return;
        }
        killer.SetRealKiller(Player);
        PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Revenge;
        Player.RpcMurderPlayer(killer);
    }
    public bool DoRevenge(CustomDeathReason deathReason) => revengeOnExile && deathReason == CustomDeathReason.Vote;
    public bool IsCandidate(PlayerControl player)
    {
        return player.GetCustomRole().GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => impostorsGetRevenged,
            CustomRoleTypes.Madmate => madmatesGetRevenged,
            _ => true,
        };
    }
}
