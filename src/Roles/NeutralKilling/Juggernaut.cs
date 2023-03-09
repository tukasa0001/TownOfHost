using TOHTOR.Extensions;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Options.Game;

namespace TOHTOR.Roles;

public class Juggernaut : NeutralKillingBase
{
    private bool canVent;
    private bool impostorVision;
    private float decreaseBy;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        bool flag = base.TryKill(target);
        if (flag && !MyPlayer.Data.IsDead)
        {
            if (KillCooldown - decreaseBy >= 1f)
            {
                KillCooldown -= decreaseBy;
            }
            else {
                KillCooldown = 1f;
            }
        }
        return flag;
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Kill Cooldown")
                .Bind(v => KillCooldown = (float)v)
                .AddFloatRange(0, 180, 2.5f, 12, "s")
                .Build())
            .SubOption(sub => sub
                .Name("Decrease Amount Each Kill")
                .Bind(v => decreaseBy = (float)v)
                .AddFloatRange(0, 30, 2.5f, 1)
                .Build())
            .SubOption(sub => sub
                .Name("Can Vent")
                .Bind(v => canVent = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .Name("Can Sabotage")
                .Bind(v => canSabotage = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
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