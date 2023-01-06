using System;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.ReduxOptions;
using UnityEngine;
using TownOfHost;
using TownOfHost.Managers;

namespace TownOfHost.Roles;

public class Veteran : Crewmate
{
    [DynElement(UI.Cooldown)]
    private Cooldown veteranCooldown;
    private Cooldown veteranDuration;

    private int totalAlerts;
    private int remainingAlerts;
    private bool canKillCrewmates;
    private bool CanKillWhileTransported;

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        remainingAlerts = totalAlerts;
    }

    [DynElement(UI.Counter)]
    private string VeteranAlertCounter() => RoleUtils.Counter(remainingAlerts, totalAlerts);

    [DynElement(UI.Misc)]
    private string GetAlertedString() => veteranDuration.IsReady() ? "" : Utils.ColorString(Color.red, "♣");

    [RoleAction(RoleActionType.OnPet)]
    public void AssumeAlert()
    {
        if (remainingAlerts <= 0 || veteranCooldown.NotReady()) return;
        VeteranAlertCounter().DebugLog("Veteran Alert Counter: ");
        veteranCooldown.Start();
        veteranDuration.Start();
        remainingAlerts--;
        MyPlayer.GetDynamicName().Render();
    }

    public bool TryKill(PlayerControl other, bool transported = false)
    {
        veteranDuration.DebugLog("Veteran Duration");
        if (veteranDuration.IsReady()) return false;
        if ((!transported || CanKillWhileTransported) && InteractionResult.Halt == this.CheckInteractions(other.GetCustomRole(), other)) return false;

        other.RpcMurderPlayer(other);
        return true;
    }

    [RoleInteraction(Faction.Crewmates)]
    public InteractionResult CrewmateAttacked() => canKillCrewmates ? InteractionResult.Proceed : InteractionResult.Halt;

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream).Color(RoleColor)
            .AddSubOption(sub => sub.Name("Number of Alerts")
                .Bind(v => totalAlerts = (int)v)
                .AddIntRangeValues(1, 10, 1, 9).Build())
            .AddSubOption(sub => sub.Name("Alert Cooldown")
                .Bind(v => veteranCooldown.Duration = (float)v)
                .AddFloatRangeValues(2.5f, 120, 2.5f, 5, "s")
                .Build())
            .AddSubOption(sub => sub.Name("Alert Duration")
                .Bind(v => veteranDuration.Duration = (float)v)
                .AddFloatRangeValues(1, 20, 0.5f, 5, "s").Build())
            .AddSubOption(sub => sub.Name("Can Kill Crewmates")
                .Bind(v => canKillCrewmates = (bool)v)
                .AddOnOffValues().Build())
            .AddSubOption(sub => sub.Name("Can Kill While Transported")
                .Bind(v => CanKillWhileTransported = (bool)v)
                .AddOnOffValues().Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Crewmate)
            .RoleColor(new Color(0.6f, 0.5f, 0.25f));
}