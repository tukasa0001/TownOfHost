using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.GUI;
using TOHTOR.Managers.History.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Interactions.Interfaces;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Options.Game;

namespace TOHTOR.Roles.RoleGroups.Crew;

public class Veteran : Crewmate
{
    [DynElement(UI.Cooldown)]
    private Cooldown veteranCooldown;
    private Cooldown veteranDuration;

    private int totalAlerts;
    private int remainingAlerts;
    private bool canKillCrewmates;
    private bool canKillWhileTransported;
    private bool canKillRangedAttackers;

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

    [RoleAction(RoleActionType.Interaction)]
    private void VeteranInteraction(PlayerControl actor, Interaction interaction, ActionHandle handle)
    {
        if (veteranDuration.IsReady()) return;

        switch (interaction)
        {
            case Transporter.TransportInteraction when !canKillWhileTransported:
            case IRangedInteraction when !canKillRangedAttackers:
            case IDelayedInteraction:
                return;
        }

        if (actor.GetCustomRole().Factions.IsAllied(this.Factions) && !canKillCrewmates) return;
        handle.Cancel();
        Game.GameHistory.AddEvent(new VettedEvent(MyPlayer, actor));
        MyPlayer.InteractWith(actor, new SimpleInteraction(new FatalIntent(interaction is IRangedInteraction), this));
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream).Color(RoleColor)
            .SubOption(sub => sub.Name("Number of Alerts")
                .Bind(v => totalAlerts = (int)v)
                .AddIntRange(1, 10, 1, 9).Build())
            .SubOption(sub => sub.Name("Alert Cooldown")
                .Bind(v => veteranCooldown.Duration = (float)v)
                .AddFloatRange(2.5f, 120, 2.5f, 5, "s")
                .Build())
            .SubOption(sub => sub.Name("Alert Duration")
                .Bind(v => veteranDuration.Duration = (float)v)
                .AddFloatRange(1, 20, 0.5f, 5, "s").Build())
            .SubOption(sub => sub.Name("Kill Crewmates")
                .Bind(v => canKillCrewmates = (bool)v)
                .AddOnOffValues().Build())
            .SubOption(sub => sub.Name("Kill While Transported")
                .Bind(v => canKillWhileTransported = (bool)v)
                .AddOnOffValues().Build())
            .SubOption(sub => sub.Name("Kill Ranged Attackers")
                .BindBool(v => canKillRangedAttackers = v)
                .AddOnOffValues().Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Crewmate)
            .RoleColor(new Color(0.6f, 0.5f, 0.25f));





    private class VettedEvent : KillEvent, IRoleEvent
    {
        public VettedEvent(PlayerControl killer, PlayerControl victim) : base(killer, victim)
        {
        }
    }
}