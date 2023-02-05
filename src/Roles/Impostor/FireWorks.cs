using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.GUI;
using TownOfHost.Managers;
using VentLib.Options;
using TownOfHost.Patches.Systems;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Logging;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class FireWorks: Morphling
{
    [DynElement(UI.Cooldown)]
    private Cooldown fireworkCooldown;
    private int totalFireworkCount;
    private int fireworksPerRound;
    private float fireworkRadius;
    private float fireworkDelay;
    private bool warnPlayers;
    private bool mustBeLastImpostor;

    private List<Vector2> fireworkLocations;
    private int fireworksThisRound;
    private int currentFireworkCount;
    private bool CanPlantBomb => (fireworksThisRound < fireworksPerRound || fireworksPerRound == -1) && totalFireworkCount > 0;
    private bool WarnPlayers => warnPlayers && fireworkDelay > 0.26f;
    private DateTime lastCheck = DateTime.Now;

    private List<PlayerControl> playersInRadius;
    private bool exploding;

    [DynElement(UI.Counter)]
    private string FireworkCounter() => RoleUtils.Counter(currentFireworkCount, totalFireworkCount);

    protected override void Setup(PlayerControl player)
    {
        currentFireworkCount = totalFireworkCount;
        fireworkLocations = new List<Vector2>();
        playersInRadius = new List<PlayerControl>();
    }

    [RoleAction(RoleActionType.AttemptKill)]
    public new void TryKill(PlayerControl target) => base.TryKill(target);

    [RoleAction(RoleActionType.OnPet)]
    private void FireworksPlantBomb()
    {
        if (fireworkCooldown.NotReady() || !CanPlantBomb) return;
        fireworkLocations.Add(MyPlayer.GetTruePosition());
        currentFireworkCount--;
        fireworksThisRound++;
        fireworkCooldown.Start();
    }

    [RoleAction(RoleActionType.Shapeshift)]
    private void Detonate(ActionHandle handle)
    {
        handle.Cancel();
        if (exploding || mustBeLastImpostor && GameStates.CountAliveImpostors() > 1) return;
        if (!WarnPlayers)
        {
            VentLogger.Old($"FireWorks Explosion Activated, Time Until Explosion: {fireworkDelay}. Not Warning Players", "FireWorksDebug");
            fireworkLocations.Do(pos => Async.Schedule(() => KillPlayersInRadius(pos), fireworkDelay));
            fireworkLocations.Clear();
        }
        else
        {
            VentLogger.Old($"FireWorks Explosion Activated, Time Until Explosion: {fireworkDelay}", "FireWorksDebug");
            exploding = true;
            Async.Schedule(() => exploding = false, fireworkDelay);
        }
    }

    // This warns players within radius, but because players can move in/out of radius we need to have it as fixed update
    [RoleAction(RoleActionType.FixedUpdate)]
    private void DelayedDetonate()
    {
        // Cheating a bit, but basically IF exploding is false and there are players in radius it means the delay duration is over
        // and those players should die
        if (!exploding || (!exploding && playersInRadius.Count == 0)) return;
        double elapsed = (DateTime.Now - lastCheck).TotalSeconds;
        if (elapsed < 0.1f) return;
        lastCheck = DateTime.Now;
        List<PlayerControl> allPlayersInAllRadii = fireworkLocations.SelectMany(pos => RoleUtils.GetPlayersWithinDistance(pos, fireworkRadius)).Distinct().ToList();

        new List<PlayerControl>(playersInRadius)
            .Where(radiiPlayer => allPlayersInAllRadii.All(p => p.PlayerId != radiiPlayer.PlayerId))
            .Do(radiiPlayer => {
                if (SabotagePatch.CurrentSabotage is not SabotageType.Reactor)
                    RoleUtils.EndReactorsForPlayer(radiiPlayer);
                playersInRadius.RemoveAll(p => p.PlayerId == radiiPlayer.PlayerId);
            });

        allPlayersInAllRadii.Distinct().Where(p => playersInRadius.All(pr => pr.PlayerId != p.PlayerId)).Do(radiiPlayer =>
        {
            if (SabotagePatch.CurrentSabotage is not SabotageType.Reactor)
                RoleUtils.PlayReactorsForPlayer(radiiPlayer);
            playersInRadius.Add(radiiPlayer);
        });

        if (exploding) return;
        playersInRadius.Distinct().Do(radiiPlayer => {
            if (SabotagePatch.CurrentSabotage is not SabotageType.Reactor)
                RoleUtils.EndReactorsForPlayer(radiiPlayer);
            radiiPlayer.RpcMurderPlayer(radiiPlayer);
        });
        playersInRadius.Clear();
        fireworkLocations.Clear();
    }

    private void KillPlayersInRadius(Vector2 location)
    {
        RoleUtils.GetPlayersWithinDistance(location, fireworkRadius).Do(p => p.RpcMurderPlayer(p));
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void ResetFireworkCount() => fireworksThisRound = 0;

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Total Firework Count")
                .BindInt(v => totalFireworkCount = v)
                .AddIntRange(1, 40, 1, 2).Build())
            .SubOption(sub => sub
                .Name("Fireworks Per Round")
                .BindInt(v => fireworksPerRound = v)
                .Value(v => v.Text("No Limit").Value(-1).Build())
                .AddIntRange(1, 20)
                .ShowSubOptionPredicate(v => (int)v > 1 || (int)v == -1)
                .SubOption(sub2 => sub2
                    .Name("FireWorks Ability Cooldown")
                    .BindFloat(v => fireworkCooldown.Duration = v)
                    .AddFloatRange(0, 120, 2.5f,8, "s").Build())
                .Build())
            .SubOption(sub => sub
                .Name("Firework Explosion Radius")
                .BindFloat(v => fireworkRadius = v)
                .AddFloatRange(0.5f, 3f, 0.1f, 3).Build())
            .SubOption(sub => sub
                .Name("Firework Delay")
                .BindFloat(v => fireworkDelay = v)
                .AddFloatRange(0f, 10f, 0.25f, 4, "s")
                .ShowSubOptionPredicate(v => (float)v > 0.26f)
                .SubOption(sub2 => sub2
                    .Name("Warn Players Before Explosion")
                    .BindBool(v => warnPlayers = v)
                    .AddOnOffValues().Build())
                .Build())
            .SubOption(sub => sub
                .Name("Must Be Last Impostor")
                .BindBool(v => mustBeLastImpostor = v)
                .AddOnOffValues().Build());
}