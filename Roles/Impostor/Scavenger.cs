using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Crewmate;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Impostor;
public sealed class Scavenger : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Scavenger),
            player => new Scavenger(player),
            CustomRoles.Scavenger,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            20500,
            SetUpOptionItem,
            "スカベンジャー"
        );
    public Scavenger(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        IgnoreBait = OptionIgnoreBait.GetBool();
    }
    private static OptionItem OptionIgnoreBait;
    enum OptionName
    {
        ScavengerIgnoreBait
    }
    public static bool IgnoreBait;
    private static void SetUpOptionItem()
    {
        OptionIgnoreBait = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ScavengerIgnoreBait, false, false);
    }

    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (!IgnoreBait && target.Is(CustomRoles.Bait))
        {
            Logger.Info($"{target.GetNameWithRole()}：ベイトキルなので通報される", "Scavenger");
        }
        else //ベイトじゃない又はベイト無効など
        {
            if (target.Is(CustomRoles.Bait)) Bait.BaitKillPlayer = null; //ベイトマーク取り消し
            ReportDeadBodyPatch.CanReportByDeadBody[target.PlayerId] = false;
            Logger.Info($"{target.GetNameWithRole()}：通報できない死体", "Scavenger");
        }
        return;
    }
}