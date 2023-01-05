using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Patches.Systems;
using TownOfHost.ReduxOptions;
using UnityEngine;

namespace TownOfHost.Roles;

public class Mare: Impostor
{
    private bool canKillWithoutSabotage;
    private float normalKillCooldown;
    private bool redNameDuringSabotage;
    private float sabotageSpeedMod;
    private float reducedKillCooldown;
    private SabotageType activationSabo;
    private bool abilityEnabled;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target) => CanKill() && base.TryKill(target);

    [RoleAction(RoleActionType.SabotageStarted, Priority.Last)]
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
    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Speed Modifier During Lights")
                .Bind(v => sabotageSpeedMod = (float)v)
                .AddFloatRangeValues(0.5f, 3, 0.1f, 10, "x").Build())
            .AddSubOption(sub => sub
                .Name("Can Kill Without Sabotage")
                .Bind(v => canKillWithoutSabotage = (bool)v)
                .ShowSubOptionsWhen(v => (bool)v)
                .AddOnOffValues()
                .AddSubOption(sub2 => sub2
                    .Name("Normal Kill Cooldown")
                    .Bind(v => normalKillCooldown = (float)v)
                    .AddFloatRangeValues(10, 120, 5, 4, "s")
                    .Build())
                .Build())
            .AddSubOption(sub => sub
                .Name("Colored Name During Lights")
                .Bind(v => redNameDuringSabotage = (bool)v)
                .AddOnOffValues().Build())
            .AddSubOption(sub => sub
                .Name("Kill Cooldown During Lights")
                .Bind(v => reducedKillCooldown = (float)v)
                .AddFloatRangeValues(0, 60, 5, 3, "s").Build())
            .AddSubOption(sub => sub
                .Name("Individual Sabotage Interaction Settings")
                .ShowSubOptionsWhen(v => (bool)v)
                .AddOnOffValues(false)
                .AddSubOption(sub2 => sub2
                    .Name("Lights")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Lights : activationSabo & ~SabotageType.Lights)
                    .AddOnOffValues().Build())
                .AddSubOption(sub2 => sub2
                    .Name("Communications")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Communications : activationSabo & ~SabotageType.Communications)
                    .AddOnOffValues(false).Build())
                .AddSubOption(sub2 => sub2
                    .Name("Oxygen")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Oxygen : activationSabo & ~SabotageType.Oxygen)
                    .AddOnOffValues(false).Build())
                .AddSubOption(sub2 => sub2
                    .Name("Reactor")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Reactor : activationSabo & ~SabotageType.Reactor)
                    .AddOnOffValues(false).Build())
                .AddSubOption(sub2 => sub2
                    .Name("Helicopter")
                    .Bind(v => activationSabo = (bool)v ? activationSabo | SabotageType.Helicopter : activationSabo & ~SabotageType.Helicopter)
                    .AddOnOffValues(false).Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, () => abilityEnabled ? reducedKillCooldown : normalKillCooldown)
            .OptionOverride(Override.PlayerSpeedMod, () => sabotageSpeedMod, () => abilityEnabled);
}