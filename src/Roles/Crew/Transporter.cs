using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Options;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities.Extensions;

namespace TownOfHost.Roles;

public class Transporter : CustomRole
{
    private int totalTransports;
    private int transportsRemaining;
    [DynElement(UI.Cooldown)]
    private Cooldown transportCooldown;

    [DynElement(UI.Counter)]
    private string RemainingTransportCounter() => $"({Utils.ColorString(RoleColor, $"{transportsRemaining}/{totalTransports}")})";

    protected override void Setup(PlayerControl player) => transportsRemaining = totalTransports;

    [RoleAction(RoleActionType.OnPet)]
    public void Transport()
    {
        if (this.transportsRemaining == 0 || !transportCooldown.IsReady()) return;
        List<PlayerControl> eligibleTargets = PlayerControl.AllPlayerControls
            .ToArray()
            .Where(player => !(player == null || player.Data.Disconnected || !player.CanMove || player.Data.IsDead))
            .ToList();

        if (eligibleTargets.Count < 2) return;
        transportCooldown.Start();

        PlayerControl target1 = eligibleTargets.PopRandom();
        PlayerControl target2 = eligibleTargets.PopRandom();

        this.transportsRemaining--;
        if (target1.inVent) target1.MyPhysics.ExitAllVents();
        if (target2.inVent) target2.MyPhysics.ExitAllVents();

        target1.MyPhysics.ResetMoveState();
        target2.MyPhysics.ResetMoveState();

        Vector2 player1Position = target1.GetTruePosition();
        Vector2 player2Position = target2.GetTruePosition();

        Utils.Teleport(target1.NetTransform, new Vector2(player2Position.x, player2Position.y + 0.3636f));
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

    [RoleInteraction(typeof(Veteran))]
    public InteractionResult VeteranTransportedInteraction(PlayerControl veteran, PlayerControl other)
    {
        veteran.GetCustomRole<Veteran>().TryKill(other, true);
        return InteractionResult.Proceed;
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub.Name("Number of Transports")
                .Bind(v => this.totalTransports = (int)v)
                .AddValues(4, 5, 10, 15, 20, 25).Build())
            .AddSubOption(sub => sub.Name("Transport Cooldown")
                .Bind(v => this.transportCooldown.Duration = Convert.ToSingle((int)v))
                .AddValues(4, 10, 15, 20, 25, 30).Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Crewmate)
            .RoleColor("#00EEFF");
}