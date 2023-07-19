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
    }

    private static OptionItem OptionCanVent;
    enum OptionName
    {
        JClientCanVent,
    }
    private static bool CanVent;
    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, OptionName.JClientCanVent, false, false);
    }
}