using System.Linq;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Neutral;
public sealed class JClient : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JClient),
            player => new JClient(player),
            CustomRoles.JClient,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            50800,
            SetupOptionItem,
            "クライアント",
            "#00b4eb",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public JClient(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute)
    {
        CanVent = OptionCanVent.GetBool();
        VentCooldown = OptionVentCooldown.GetFloat();
        VentMaxTime = OptionVentMaxTime.GetFloat();
        CanAlsoBeExposedToJackal = OptionCanAlsoBeExposedToJackal.GetBool();
        AfterJackalDead = (AfterJackalDeadMode)OptionAfterJackalDead.GetValue();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    private static OptionItem OptionCanVent;
    private static OptionItem OptionVentCooldown;
    private static OptionItem OptionVentMaxTime;
    private static OptionItem OptionCanAlsoBeExposedToJackal;
    private static OptionItem OptionAfterJackalDead;
    private static Options.OverrideTasksData Tasks;
    enum OptionName
    {
        JClientHasImpostorVision,
        JClientCanVent,
        JClientVentCooldown,
        JClientVentMaxTime,
        JClientCanAlsoBeExposedToJackal,
        JClientAfterJackalDead
    }

    private static bool CanVent;
    public static float VentCooldown;
    public static float VentMaxTime;
    private static bool CanAlsoBeExposedToJackal;
    private static AfterJackalDeadMode AfterJackalDead;
    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, OptionName.JClientCanVent, false, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.JClientVentCooldown, new(0f, 180f, 5f), 0f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionVentMaxTime = FloatOptionItem.Create(RoleInfo, 12, OptionName.JClientVentMaxTime, new(0f, 180f, 5f), 0f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanAlsoBeExposedToJackal = BooleanOptionItem.Create(RoleInfo, 13, OptionName.JClientCanAlsoBeExposedToJackal, false, false);
        OptionAfterJackalDead = StringOptionItem.Create(RoleInfo, 14, OptionName.JClientAfterJackalDead, AfterJackalDeadModeText, 0, false);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 30, RoleInfo.RoleName, RoleInfo.Tab);
    }

    public enum AfterJackalDeadMode
    {
        None,
        Following
    };
    private static readonly string[] AfterJackalDeadModeText =
    {
    "JClientAfterJackalDeadMode.None",
    "JClientAfterJackalDeadMode.Following",
    };
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = JClient.VentCooldown;
        AURoleOptions.EngineerInVentMaxTime = JClient.VentMaxTime;
    }

    public bool KnowsJackal() => IsTaskFinished;

    public override bool OnCompleteTask()
    {
        if (KnowsJackal())
        {
            foreach (var jackal in Main.AllPlayerControls.Where(player => player.Is(CustomRoles.Jackal)).ToArray())
            {
                NameColorManager.Add(Player.PlayerId, jackal.PlayerId, jackal.GetRoleColorCode());
            }
        }

        return true;
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!CanAlsoBeExposedToJackal ||
            !seer.Is(CustomRoles.Jackal) || seen.GetRoleClass() is not JClient jclient ||
            !jclient.KnowsJackal())
        {
            return string.Empty;
        }

        return Utils.ColorString(Utils.GetRoleColor(CustomRoles.JClient), "★");
    }
    public override void AfterMeetingTasks()
    {
        //ジャッカル死亡時のクライアント状態変化
        if (AfterJackalDead == AfterJackalDeadMode.None) return;

        var jackal = Main.AllPlayerControls.ToArray().Where(pc => pc.Is(CustomRoles.Jackal)).FirstOrDefault();
        if (jackal != null && !jackal.Data.IsDead  &&
            !Main.AfterMeetingDeathPlayers.ContainsKey(jackal.PlayerId)) return;

        Logger.Info($"jackal:dead, mode:{AfterJackalDead}", "JClientDeadMode");

        if (AfterJackalDead == AfterJackalDeadMode.Following)
        {
            if (!Player.Data.IsDead && MyTaskState.IsTaskFinished)
            {
                Main.AfterMeetingDeathPlayers.TryAdd(Player.PlayerId, CustomDeathReason.FollowingSuicide);
                Logger.Info($"followingDead set:{Player.name}", "JClientDeadMode");
            }
        }
    }
}
