using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral;
public sealed class JClient : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JClient),
            player => new JClient(player),
            CustomRoles.JClient,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            51000,
            SetupOptionItem,
            "jcl",
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
    }

    private static OptionItem OptionCanVent;
    private static OptionItem OptionVentCooldown;
    private static OptionItem OptionVentMaxTime;
    enum OptionName
    {
        JClientCanVent,
        JClientVentCooldown,
        JClientVentMaxTime
    }

    private static bool CanVent;
    private static float VentCooldown;
    private static float VentMaxTime;
    private static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, OptionName.JClientCanVent, false, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.JClientVentCooldown, new(0f, 180f, 5f), 0f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionVentMaxTime = FloatOptionItem.Create(RoleInfo, 12, OptionName.JClientVentMaxTime, new(0f, 180f, 5f), 0f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = JClient.VentCooldown;
        AURoleOptions.EngineerInVentMaxTime = JClient.VentMaxTime;
    }
    private bool KnowsJackal() => IsTaskFinished;
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
}