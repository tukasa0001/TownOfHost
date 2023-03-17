using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Patches.Systems;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities;
using Priority = TOHTOR.Roles.Internals.Attributes.Priority;

namespace TOHTOR.Roles.RoleGroups.Impostors;

public class Mare: Vanilla.Impostor
{
    private bool canKillWithoutSabotage;
    private float normalKillCooldown;
    private bool redNameDuringSabotage;
    private float sabotageSpeedMod;
    private float reducedKillCooldown;
    private SabotageType activationSabo;
    private bool abilityEnabled;
    private bool abilityLightsOnly;

    protected override void Setup(PlayerControl player) => activationSabo = abilityLightsOnly ? SabotageType.Lights : activationSabo;

    [RoleAction(RoleActionType.Attack)]
    public new bool TryKill(PlayerControl target) => CanKill() && base.TryKill(target);

    [RoleAction(RoleActionType.SabotageStarted, priority: Priority.Last)]
    private void MareSabotageCheck(SabotageType sabotageType, ActionHandle handle)
    {
        if (!activationSabo.HasFlag(sabotageType) || handle.IsCanceled) return;
        abilityEnabled = true;
        if (redNameDuringSabotage)
        {
            DynamicName myName = MyPlayer.GetDynamicName();
            Game.GetAlivePlayers().Do(p => myName.RenderFor(p));
        }
        SyncOptions();
    }

    [RoleAction(RoleActionType.SabotageFixed)]
    private void MareSabotageFix()
    {
        abilityEnabled = false;
        if (redNameDuringSabotage)
        {
            DynamicName myName = MyPlayer.GetDynamicName();
            Game.GetAlivePlayers().Do(p => myName.RenderFor(p));
        }
        SyncOptions();
    }

    public override void OnGameStart()
    {
        DynamicName myName = MyPlayer.GetDynamicName();
        DynamicString coloredName = new(() => abilityEnabled && redNameDuringSabotage ? new Color(0.36f, 0f, 0.58f).Colorize("{0}") : "");
        myName.AddRule(GameState.Roaming, UI.Name, coloredName);
    }

    public override bool CanKill() => canKillWithoutSabotage || abilityEnabled;

    // lol this was fun because of the bitwise operators
    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Speed Modifier During Sabotage")
                .Bind(v => sabotageSpeedMod = (float)v)
                .AddFloatRange(0.5f, 3, 0.1f, 10, "x").Build())
            .SubOption(sub => sub
                .Name("Can Kill Without Sabotage")
                .Bind(v => canKillWithoutSabotage = (bool)v)
                .ShowSubOptionPredicate(v => (bool)v)
                .AddOnOffValues()
                .SubOption(sub2 => sub2
                    .Name("Normal Kill Cooldown")
                    .Bind(v => normalKillCooldown = (float)v)
                    .AddFloatRange(10, 120, 5, 4, "s")
                    .Build())
                .Build())
            .SubOption(sub => sub
                .Name("Colored Name During Sabotage")
                .Bind(v => redNameDuringSabotage = (bool)v)
                .AddOnOffValues().Build())
            .SubOption(sub => sub
                .Name("Kill Cooldown During Sabotage")
                .Bind(v => reducedKillCooldown = (float)v)
                .AddFloatRange(0, 60, 5, 3, "s").Build())
            .SubOption(sub => sub
                .Name("Specific Sabotage Settings")
                .ShowSubOptionPredicate(v => (bool)v)
                .BindBool(v => abilityLightsOnly = v)
                .Value(v => v.Text("Lights Only").Value(false).Build())
                .Value(v => v.Text("Individual").Value(true).Build())
                .SubOption(sub2 => sub2
                    .Name("Lights")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Lights : activationSabo & ~SabotageType.Lights)
                    .AddOnOffValues().Build())
                .SubOption(sub2 => sub2
                    .Name("Communications")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Communications : activationSabo & ~SabotageType.Communications)
                    .AddOnOffValues(false).Build())
                .SubOption(sub2 => sub2
                    .Name("Oxygen")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Oxygen : activationSabo & ~SabotageType.Oxygen)
                    .AddOnOffValues(false).Build())
                .SubOption(sub2 => sub2
                    .Name("Reactor")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Reactor : activationSabo & ~SabotageType.Reactor)
                    .AddOnOffValues(false).Build())
                .SubOption(sub2 => sub2
                    .Name("Helicopter")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Helicopter : activationSabo & ~SabotageType.Helicopter)
                    .AddOnOffValues(false).Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, () => abilityEnabled ? reducedKillCooldown : normalKillCooldown)
            .OptionOverride(Override.PlayerSpeedMod, () => sabotageSpeedMod, () => abilityEnabled);
}