using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;
using UnityEngine;

namespace TownOfHost.Roles;

public class Jackal: NeutralKillingBase
{
    private bool canVent;
    private bool impostorVision;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target) => base.TryKill(target);

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Kill Cooldown")
                .Bind(v => KillCooldown = (float)v)
                .AddFloatRangeValues(0, 180, 2.5f, 12, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Can Vent")
                .Bind(v => canVent = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(sub => sub
                .Name("Can Sabotage")
                .Bind(v => canSabotage = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(sub => sub
                .Name("Impostor Vision")
                .Bind(v => impostorVision = (bool)v)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(0f, 0.71f, 0.92f))
            .CanVent(canVent)
            .OptionOverride(Override.ImpostorLightMod, () => DesyncOptions.OriginalHostOptions.AsNormalOptions()!.CrewLightMod, () => !impostorVision);
}