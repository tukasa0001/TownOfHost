using TownOfHost.Extensions;
using TownOfHost.Options;
using AmongUs.GameOptions;

namespace TownOfHost.Roles;

public class Vulture : CustomRole
{
    private int bodyCount = 0;
    // option
    private int bodyAmount;
    private bool canUseVents;
    private bool impostorVision;
    private bool canSwitchMode;
    private bool isEatMode = true;

    [RoleAction(RoleActionType.SelfReportBody)]
    public void EatBody(PlayerControl target, ActionHandle handle)
    {
        if (!isEatMode | TOHPlugin.unreportableBodies.Contains(target.Data.PlayerId)) return;
        TOHPlugin.unreportableBodies.Add(target.Data.PlayerId);
        bodyCount++;
        if (bodyCount >= bodyAmount)
        {
            // they won
        }
        else
        {
            handle.Cancel();
        }
    }

    [RoleAction(RoleActionType.OnPet)]
    public void Switch()
    {
        if (!canSwitchMode) return;
        isEatMode = !isEatMode;
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .Tab(DefaultTabs.NeutralTab)
            .AddSubOption(sub => sub
                .Name("Bodies")
                .Bind(v => bodyAmount = (int)v)
                .AddIntRangeValues(1, 10, 1, 2)
                .Build())
            .AddSubOption(opt =>
                opt.Name("Has Impostor Vision")
                .Bind(v => impostorVision = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(opt =>
                opt.Name("Can Switch between Eat and Report")
                .Bind(v => canSwitchMode = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(opt => opt.Name("Can Use Vents")
                .Bind(v => canUseVents = (bool)v)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor("#a36727")
        .VanillaRole(canUseVents ? RoleTypes.Engineer : RoleTypes.Crewmate)
        .SpecialType(SpecialType.Neutral)
        .OptionOverride(Override.CrewLightMod,
            () => GameOptionsManager.Instance.CurrentGameOptions.AsNormalOptions()!.ImpostorLightMod, () => impostorVision);
}