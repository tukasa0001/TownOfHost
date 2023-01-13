using TownOfHost.Extensions;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

public class Trapper : Crewmate
{
    private float trappedDuration;

    [RoleAction(RoleActionType.MyDeath)]
    private void TrapperDeath(PlayerControl killer)
    {
        GameOptionOverride[] overrides = { new(Override.PlayerSpeedMod, 0f) };
        killer.GetCustomRole().SyncOptions(overrides);

        DTask.Schedule(() => killer.GetCustomRole().SyncOptions(), trappedDuration);
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Trapped Duration")
                .Bind(v => trappedDuration = (float)v)
                .AddFloatRangeValues(1, 10, 0.5f, 8, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor("#5a8fd0");
}