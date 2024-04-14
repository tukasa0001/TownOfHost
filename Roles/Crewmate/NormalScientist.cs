using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class NormalScientist : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(NormalScientist),
            player => new NormalScientist(player),
            CustomRoles.NormalScientist,
            () => RoleTypes.Scientist,
            CustomRoleTypes.Crewmate,
            2100,
            SetupOptionItem,
            "科学者",
            "#8cffff"
        );
    public NormalScientist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        vitalCooldown = OptionVitalCooldown.GetFloat();
        vitalBatteryDuration = OptionVitalBatteryDuration.GetFloat();
    }
    private static OptionItem OptionVitalCooldown;
    private static OptionItem OptionVitalBatteryDuration;
    enum OptionName
    {
        VitalCooldown,
        VitalBatteryDuration
    }
    private static float vitalCooldown;
    private static float vitalBatteryDuration;

    private static void SetupOptionItem()
    {
        OptionVitalCooldown = FloatOptionItem.Create(RoleInfo, 3, OptionName.VitalCooldown, new(0f, 180f, 5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionVitalBatteryDuration = FloatOptionItem.Create(RoleInfo, 4, OptionName.VitalBatteryDuration, new(5f, 180f, 5f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ScientistCooldown = vitalCooldown;
        AURoleOptions.ScientistBatteryCharge = vitalBatteryDuration;
    }
}