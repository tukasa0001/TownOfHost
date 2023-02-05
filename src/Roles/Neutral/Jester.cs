using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Options;
using VentLib.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using TownOfHost.Victory.Conditions;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;

public class Jester : CustomRole
{
    private bool canUseVents;
    private bool impostorVision;

    [RoleAction(RoleActionType.SelfExiled)]
    public void JesterWin()
    {
        ManualWin jesterWin = new(MyPlayer, WinReason.SoloWinner, 999);
        jesterWin.Activate();
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .SubOption(opt =>
                opt.Name("Has Impostor Vision").Bind(v => impostorVision = (bool)v).AddOnOffValues().Build())
            .SubOption(opt => opt.Name("Can Use Vents")
                .Bind(v => canUseVents = (bool)v)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
            .VanillaRole(canUseVents ? RoleTypes.Engineer : RoleTypes.Crewmate)
            .SpecialType(SpecialType.Neutral)
            .CanVent(canUseVents)
            .RoleColor(new Color(0.93f, 0.38f, 0.65f))
            .OptionOverride(Override.CrewLightMod,
                () => GameOptionsManager.Instance.CurrentGameOptions.AsNormalOptions()!.ImpostorLightMod,
                () => impostorVision)
            .OptionOverride(Override.EngVentDuration, 100f)
            .OptionOverride(Override.EngVentCooldown, 0.1f);
    }
}