using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Managers.History.Events;
using TOHTOR.Options;
using TOHTOR.Roles.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Interactions.Interfaces;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using UnityEngine;
using VentLib.Logging;
using VentLib.Options.Game;
using VentLib.Utilities;

namespace TOHTOR.Roles.RoleGroups.Crew;

public class Transporter : Morphling
{
    private int totalTransports;
    private int transportsRemaining;

    [DynElement(UI.Cooldown)]
    private Cooldown transportCooldown;

    private List<PlayerControl> transportList = new();

    [DynElement(UI.Counter)]
    private string RemainingTransportCounter() => RoleUtils.Counter(transportsRemaining, totalTransports);

    protected override void Setup(PlayerControl player)
    {
        transportList = new List<PlayerControl>();
        VentLogger.Fatal($"Total Transports: {totalTransports}");
        transportsRemaining = totalTransports;
        shapeshiftCooldown = 0.1f;
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void ResetTransportTargets() => transportList.Clear();

    [RoleAction(RoleActionType.Shapeshift)]
    public void TransportSelect(PlayerControl target, ActionHandle handle)
    {
        handle.Cancel();
        if (this.transportsRemaining == 0 || !transportCooldown.IsReady()) return;
        transportList.Add(target);
        target.GetDynamicName().AddRule(GameState.Roaming, UI.Misc, new DynamicString(Color.red.Colorize("Selected")), MyPlayer.PlayerId);
        Async.Schedule(() => Deselect(target), 8f);

        VentLogger.Trace($"{MyPlayer.GetNameWithRole()} => Selected ({target.GetNameWithRole()})", "Transporter");
        if (transportList.Count < 2) return;

        PlayerControl target1 = transportList[0];
        PlayerControl target2 = transportList[1];
        transportList.Clear();
        Deselect(target1);
        Deselect(target2);

        if (target1.PlayerId == target2.PlayerId) return;

        transportCooldown.Start();

        this.transportsRemaining--;
        if (target1.inVent) target1.MyPhysics.ExitAllVents();
        if (target2.inVent) target2.MyPhysics.ExitAllVents();

        target1.MyPhysics.ResetMoveState();
        target2.MyPhysics.ResetMoveState();

        Vector2 player1Position = target1.GetTruePosition();
        Vector2 player2Position = target2.GetTruePosition();

        if (target1.IsAlive())
            Utils.Teleport(target1.NetTransform, new Vector2(player2Position.x, player2Position.y + 0.3636f));
        if (target2.IsAlive())
            Utils.Teleport(target2.NetTransform, new Vector2(player1Position.x, player1Position.y + 0.3636f));

        target1.InteractWith(target2, new TransportInteraction(target1));
        target2.InteractWith(target1, new TransportInteraction(target2));

        target1.moveable = true;
        target2.moveable = true;
        target1.Collider.enabled = true;
        target2.Collider.enabled = true;
        target1.NetTransform.enabled = true;
        target2.NetTransform.enabled = true;

        Game.GameHistory.AddEvent(new TransportedEvent(MyPlayer, target1, target2));
    }

    private void Deselect(PlayerControl target)
    {
        transportList.RemoveAll(p => p.PlayerId == target.PlayerId);
        target.GetDynamicName().RemoveRule(GameState.Roaming, UI.Misc, MyPlayer.PlayerId);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.CrewmateTab)
            .SubOption(sub => sub.Name("Number of Transports")
                .Bind(v => this.totalTransports = (int)v)
                .Values(4, 5, 10, 15, 20, 25).Build())
            .SubOption(sub => sub.Name("Transport Cooldown")
                .Bind(v => this.transportCooldown.Duration = Convert.ToSingle((int)v))
                .Values(4, 10, 15, 20, 25, 30).Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .VanillaRole(RoleTypes.Crewmate)
            .DesyncRole(RoleTypes.Shapeshifter)
            .RoleColor("#00EEFF");


    private class TransportedEvent : AbilityEvent, IMultiTargetEvent
    {
        private PlayerControl target1;
        private PlayerControl target2;

        public TransportedEvent(PlayerControl user, PlayerControl target1, PlayerControl target2) : base(user)
        {
            this.target1 = target1;
            this.target2 = target2;
        }

        public List<PlayerControl> Targets() => new() { target1, target2 };

        public PlayerControl Target1() => target1;

        public PlayerControl Target2() => target2;

        public override string Message() => $"{Game.GetName(target1)} and {Game.GetName(target2)} were transported by {Game.GetName(Player())}.";
    }

    public class TransportInteraction : SimpleInteraction {
        public TransportInteraction(PlayerControl actor) : base(new NeutralIntent(), actor.GetCustomRole()) { }
    }
}