using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Doctor : RoleBase, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Doctor),
            player => new Doctor(player),
            CustomRoles.Doctor,
            () => OptionHasVital.GetBool() ? RoleTypes.Scientist : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            30700,
            SetupOptionItem,
            "ドクター",
            "#80ffdd"
        );
    public Doctor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskCompletedBatteryCharge = OptionTaskCompletedBatteryCharge.GetFloat();
        HasVital = OptionHasVital.GetBool();
    }
    private static OptionItem OptionHasVital;
    private static OptionItem OptionTaskCompletedBatteryCharge;
    enum OptionName
    {
        DoctorTaskCompletedBatteryCharge,
        DoctorHasVital
    }
    private static float TaskCompletedBatteryCharge;
    private static bool HasVital;

    private static void SetupOptionItem()
    {
        OptionHasVital = BooleanOptionItem.Create(RoleInfo, 11, OptionName.DoctorHasVital, true, false);
        OptionTaskCompletedBatteryCharge = FloatOptionItem.Create(RoleInfo, 10, OptionName.DoctorTaskCompletedBatteryCharge, new(0f, 10f, 1f), 5f, false, OptionHasVital)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ScientistCooldown = 0f;
        AURoleOptions.ScientistBatteryCharge = TaskCompletedBatteryCharge;
    }
}