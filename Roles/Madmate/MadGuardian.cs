using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Options;

namespace TownOfHostForE.Roles.Madmate;
public sealed class MadGuardian : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadGuardian),
            player => new MadGuardian(player),
            CustomRoles.MadGuardian,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            5100,
            SetupOptionItem,
            "マッドガーディアン",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadGuardian(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        FieldCanSeeKillFlash = MadmateCanSeeKillFlash.GetBool();
        CanSeeWhoTriedToKill = OptionCanSeeWhoTriedToKill.GetBool();
    }

    private static OptionItem OptionCanSeeWhoTriedToKill;
    public static OverrideTasksData Tasks;
    enum OptionName
    {
        MadGuardianCanSeeWhoTriedToKill
    }
    private static bool FieldCanSeeKillFlash;
    private static bool CanSeeWhoTriedToKill;

    private static void SetupOptionItem()
    {
        OptionCanSeeWhoTriedToKill = BooleanOptionItem.Create(RoleInfo, 10, OptionName.MadGuardianCanSeeWhoTriedToKill, false, false);
        //ID10120~10123を使用
        Tasks = OverrideTasksData.Create(RoleInfo, 20);
        SetUpAddOnOptions(RoleInfo.ConfigId + 30, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        //MadGuardianを切れるかの判定処理
        if (!IsTaskFinished) return true;

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
    public bool CheckKillFlash(MurderInfo info) => FieldCanSeeKillFlash;
}