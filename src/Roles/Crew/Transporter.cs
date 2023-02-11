using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHost.API;
using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Options;
using TownOfHost.Roles.Internals;
using VentLib.Options;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Logging;
using VentLib.Utilities;

namespace TownOfHost.Roles;

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

        this.CheckInteractions(target1.GetCustomRole(), target1, target2);
        this.CheckInteractions(target2.GetCustomRole(), target2, target1);

        target1.moveable = true;
        target2.moveable = true;
        target1.Collider.enabled = true;
        target2.Collider.enabled = true;
        target1.NetTransform.enabled = true;
        target2.NetTransform.enabled = true;
    }

    private void Deselect(PlayerControl target)
    {
        transportList.RemoveAll(p => p.PlayerId == target.PlayerId);
        target.GetDynamicName().RemoveRule(GameState.Roaming, UI.Misc, MyPlayer.PlayerId);
    }

    [RoleInteraction(typeof(Veteran))]
    public InteractionResult VeteranTransportedInteraction(PlayerControl veteran, PlayerControl other)
    {
        veteran.GetCustomRole<Veteran>().TryKill(other, true);
        return InteractionResult.Proceed;
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
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
}