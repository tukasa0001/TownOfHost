using TownOfHost.Extensions;
using VentLib.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class Trapper : Crewmate
{
    private float trappedDuration;

    [RoleAction(RoleActionType.MyDeath)]
    private void TrapperDeath(PlayerControl killer)
    {
        GameOptionOverride[] overrides = { new(Override.PlayerSpeedMod, 0f) };
        killer.GetCustomRole().SyncOptions(overrides);

        Async.Schedule(() => killer.GetCustomRole().SyncOptions(), trappedDuration);
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Trapped Duration")
                .Bind(v => trappedDuration = (float)v)
                .AddFloatRange(1, 10, 0.5f, 8, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor("#5a8fd0");
}