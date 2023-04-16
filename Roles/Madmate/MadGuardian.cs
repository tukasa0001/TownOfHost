using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Madmate;
public sealed class MadGuardian : RoleBase, IKillFlashSeeable
{
    public static SimpleRoleInfo RoleInfo =
        new(
            typeof(MadGuardian),
            player => new MadGuardian(player),
            CustomRoles.MadGuardian,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            10100,
            SetupOptionItem
        );
    public MadGuardian(PlayerControl player)
    : base(
        RoleInfo,
        player,
        HasTask.ForRecompute
    )
    {
        canSeeKillFlash = MadmateCanSeeKillFlash.GetBool();
        CanSeeWhoTriedToKill = OptionCanSeeWhoTriedToKill.GetBool();
    }

    private static OptionItem OptionCanSeeWhoTriedToKill;
    public static OverrideTasksData Tasks;
    enum OptionName
    {
        MadGuardianCanSeeWhoTriedToKill
    }
    private static bool canSeeKillFlash;
    private static bool CanSeeWhoTriedToKill;

    private static void SetupOptionItem()
    {
        OptionCanSeeWhoTriedToKill = BooleanOptionItem.Create(RoleInfo, 10, OptionName.MadGuardianCanSeeWhoTriedToKill, false, false);
        //ID10120~10123を使用
        Tasks = OverrideTasksData.Create(RoleInfo, 20);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        //MadGuardianを切れるかの判定処理
        if (IsTaskFinished)
        {
            info.CanKill = false;
            if (!NameColorManager.TryGetData(killer, target, out var value) || value != RoleInfo.RoleColorCode)
            {
                NameColorManager.Add(killer.PlayerId, target.PlayerId);
                if (CanSeeWhoTriedToKill)
                    NameColorManager.Add(target.PlayerId, killer.PlayerId, RoleInfo.RoleColorCode);
                Utils.NotifyRoles();
            }
            return false;
        }
        return true;
    }
    public bool CanSeeKillFlash(MurderInfo info) => canSeeKillFlash;
}