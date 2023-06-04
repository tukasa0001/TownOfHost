using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;

namespace TownOfHost.Roles.Neutral;
public sealed class SchrodingerCat : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SchrodingerCat),
            player => new SchrodingerCat(player),
            CustomRoles.SchrodingerCat,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50400,
            SetupOptionItem,
            "sc",
            "#696969",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public SchrodingerCat(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanWinTheCrewmateBeforeChange = OptionCanWinTheCrewmateBeforeChange.GetBool();
        ChangeTeamWhenExile = OptionChangeTeamWhenExile.GetBool();
        CanSeeKillableTeammate = OptionCanSeeKillableTeammate.GetBool();
    }
    static OptionItem OptionCanWinTheCrewmateBeforeChange;
    static OptionItem OptionChangeTeamWhenExile;
    static OptionItem OptionCanSeeKillableTeammate;

    enum OptionName
    {
        CanBeforeSchrodingerCatWinTheCrewmate,
        SchrodingerCatExiledTeamChanges,
        SchrodingerCatCanSeeKillableTeammate,
    }
    static bool CanWinTheCrewmateBeforeChange;
    static bool ChangeTeamWhenExile;
    static bool CanSeeKillableTeammate;

    public static void SetupOptionItem()
    {
        OptionCanWinTheCrewmateBeforeChange = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanBeforeSchrodingerCatWinTheCrewmate, false, false);
        OptionChangeTeamWhenExile = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SchrodingerCatExiledTeamChanges, false, false);
        OptionCanSeeKillableTeammate = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SchrodingerCatCanSeeKillableTeammate, false, false);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (Is(info.AttemptTarget))
        {
            (var killer, var target) = info.AttemptTuple;

            //自殺ならスルー
            if (info.IsSuicide) return true;
            //既に変化していたらスルー
            if (!target.Is(CustomRoles.SchrodingerCat)) return true;

            //シュレディンガーの猫が切られた場合の役職変化スタート
            killer.RpcGuardAndKill(target);
            info.CanKill = false;
            switch (killer.GetCustomRole())
            {
                case CustomRoles.BountyHunter:
                    var bountyHunter = (BountyHunter)killer.GetRoleClass();
                    if (bountyHunter.GetTarget() == target)
                        bountyHunter.ResetTarget();//ターゲットの選びなおし
                    break;
                case CustomRoles.SerialKiller:
                    var serialKiller = (SerialKiller)killer.GetRoleClass();
                    serialKiller.SuicideTimer = null;
                    break;
                case CustomRoles.Sheriff:
                    target.RpcSetCustomRole(CustomRoles.CSchrodingerCat);
                    break;
                case CustomRoles.Egoist:
                    target.RpcSetCustomRole(CustomRoles.EgoSchrodingerCat);
                    break;
                case CustomRoles.Jackal:
                    target.RpcSetCustomRole(CustomRoles.JSchrodingerCat);
                    break;
            }
            if (killer.Is(CustomRoleTypes.Impostor))
                target.RpcSetCustomRole(CustomRoles.MSchrodingerCat);

            if (CanSeeKillableTeammate)
            {
                var roleType = killer.GetCustomRole().GetCustomRoleTypes();
                System.Func<PlayerControl, bool> isTarget = roleType switch
                {
                    CustomRoleTypes.Impostor => (pc) => pc.GetCustomRole().GetCustomRoleTypes() == roleType,
                    _ => (pc) => pc.GetCustomRole() == killer.GetCustomRole()
                };
                ;
                var killerTeam = Main.AllPlayerControls.Where(pc => isTarget(pc));
                foreach (var member in killerTeam)
                {
                    NameColorManager.Add(member.PlayerId, target.PlayerId, RoleInfo.RoleColorCode);
                    NameColorManager.Add(target.PlayerId, member.PlayerId);
                }
            }
            else
            {
                NameColorManager.Add(killer.PlayerId, target.PlayerId, RoleInfo.RoleColorCode);
                NameColorManager.Add(target.PlayerId, killer.PlayerId);
            }
            Utils.NotifyRoles();
            Utils.MarkEveryoneDirtySettings();
            //シュレディンガーの猫の役職変化処理終了
            //ニュートラルのキル能力持ちが追加されたら、その陣営を味方するシュレディンガーの猫の役職を作って上と同じ書き方で書いてください
            return false;

        }
        return true;
    }
    public static void ChangeTeam(PlayerControl player)
    {
        if (!(ChangeTeamWhenExile && player.Is(CustomRoles.SchrodingerCat))) return;

        var rand = IRandom.Instance;
        List<CustomRoles> Rand = new()
            {
                CustomRoles.CSchrodingerCat,
                CustomRoles.MSchrodingerCat
            };
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            if (pc.Is(CustomRoles.Egoist) && !Rand.Contains(CustomRoles.EgoSchrodingerCat))
                Rand.Add(CustomRoles.EgoSchrodingerCat);

            if (pc.Is(CustomRoles.Jackal) && !Rand.Contains(CustomRoles.JSchrodingerCat))
                Rand.Add(CustomRoles.JSchrodingerCat);
        }
        var Role = Rand[rand.Next(Rand.Count)];
        player.RpcSetCustomRole(Role);
    }
    public bool CheckWin(out AdditionalWinners winnerType)
    {
        winnerType = AdditionalWinners.SchrodingerCat;
        return CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && CanWinTheCrewmateBeforeChange;
    }
}
