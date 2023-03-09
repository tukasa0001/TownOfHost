#nullable enable
using System.Collections.Generic;
using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.GUI;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Roles;

public class BountyHunter: Impostor
{
    private PlayerControl? bhTarget;
    private Cooldown acquireNewTarget;

    private float bountyKillCoolDown;
    private float punishKillCoolDown;

    [DynElement(UI.Misc)]
    private string ShowTarget() => bhTarget == null ? "" : Color.red.Colorize("Target: ") + Color.white.Colorize(bhTarget.GetDynamicName().RawName);

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target)
    {
        SendKillCooldown(bhTarget?.PlayerId == target.PlayerId);
        bool success = base.TryKill(target);
        if (success)
            BountyHunterAcquireTarget();
        return success;
    }

    [RoleAction(RoleActionType.FixedUpdate)]
    private void BountyHunterTargetUpdate()
    {
        if (acquireNewTarget.NotReady()) return;
        BountyHunterAcquireTarget();
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void BountyHunterTargetOnRoundStart() => BountyHunterAcquireTarget();

    private void BountyHunterAcquireTarget()
    {
        List<PlayerControl> eligiblePlayers = Game.GetAlivePlayers()
            .Where(p => !p.GetCustomRole().Factions.IsImpostor())
            .ToList();
        if (eligiblePlayers.Count == 0) return;

        // Small function to assign a NEW random target unless there's only one eligible target alive
        PlayerControl newTarget = eligiblePlayers.PopRandom();
        while (eligiblePlayers.Count > 1 && bhTarget?.PlayerId == newTarget.PlayerId)
            newTarget = eligiblePlayers.PopRandom();

        bhTarget = newTarget;
        acquireNewTarget.Start();
    }

    private void SendKillCooldown(bool decreased)
    {
        float cooldown = decreased ? bountyKillCoolDown : punishKillCoolDown;
        cooldown.DebugLog("Sending Cooldown: ");
        GameOptionOverride[] modifiedCooldown = { new(Override.KillCooldown, cooldown) };
        DesyncOptions.SendModifiedOptions(modifiedCooldown, MyPlayer);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Color(RoleColor)
            .SubOption(sub => sub
                .Name("Time Until New Target")
                .Bind(v => acquireNewTarget.Duration = (float)v)
                .AddFloatRange(5f, 120, 5, 11)
                .Build())
            .SubOption(sub => sub
                .Name("Kill Cooldown After Killing Target")
                .Bind(v => bountyKillCoolDown = (float)v)
                .AddFloatRange(0, 30, 0.5f, 6)
                .Build())
            .SubOption(sub => sub
                .Name("Kill Cooldown After Killing Other")
                .Bind(v => punishKillCoolDown = (float)v)
                .AddFloatRange(30, 180, 2.5f, 15)
                .Build());
}