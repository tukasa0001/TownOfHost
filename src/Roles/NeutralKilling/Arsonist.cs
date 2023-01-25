using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class Arsonist : NeutralKillingBase
{

    private bool strictDousing;
    private bool IsDousing => dousingDuration.NotReady();
    private float douseCooldown;
    private Cooldown dousingDuration;
    private HashSet<byte> dousedPlayers;
    private int knownAlivePlayers;

    private PlayerControl myTarget;
    private DateTime lastCheck = DateTime.Now;

    protected override void Setup(PlayerControl player) => dousedPlayers = new HashSet<byte>();

    [DynElement(UI.Counter)]
    private string DouseCounter() => RoleUtils.Counter(dousedPlayers.Count, knownAlivePlayers - 1);

    [DynElement(UI.Cooldown)]
    private string DousingView() => dousingDuration.IsReady() ? "" : RoleColor.Colorize(dousingDuration + "s");

    [DynElement(UI.Misc)]
    private string DisplayWin() => dousedPlayers.Count >= knownAlivePlayers - 1 ? RoleColor.Colorize("Press Ignite to Win") : "";

    [RoleAction(RoleActionType.OnPet)]
    private void KillDoused() => dousedPlayers.Select(p => Utils.GetPlayerById(p)).Where(p => !p.Data.IsDead).Do(p => p.RpcMurderPlayer(p));

    [RoleAction(RoleActionType.AttemptKill)]
    private void StartDousePlayer(PlayerControl target, ActionHandle handle)
    {
        handle.Cancel();
        MyPlayer.RpcGuardAndKill(MyPlayer);
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return;
        bool canDouse = target.GetCustomRole().CanBeKilled();
        if (canDouse)
        {
            myTarget = target;
            dousingDuration.StartThenRun(EndDousePlayer);
            SyncOptions();
        }
    }

    [RoleAction(RoleActionType.FixedUpdate)]
    private void ArsonistStrictDousing()
    {
        if (!strictDousing || !IsDousing) return;
        double elapsed = (DateTime.Now - lastCheck).TotalSeconds;
        if (elapsed < ModConstants.RoleFixedUpdateCooldown) return;
        lastCheck = DateTime.Now;

        List<PlayerControl> closestPlayers = MyPlayer.GetPlayersInAbilityRangeSorted(false);
        if (closestPlayers.Count > 0 || closestPlayers.Any(p => p.PlayerId == myTarget.PlayerId)) return;
        myTarget = null;
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void UpdatePlayerCounts()
    {
        knownAlivePlayers = Game.GetAlivePlayers().Count();
        dousedPlayers.RemoveWhere(p =>
        {
            PlayerControl player = Utils.GetPlayerById(p);
            return player.Data.IsDead || player.Data.Disconnected;
        });
    }

    private void EndDousePlayer()
    {
        List<PlayerControl> closestPlayers = MyPlayer.GetPlayersInAbilityRangeSorted(false);
        MyPlayer.RpcGuardAndKill(MyPlayer);
        SyncOptions();
        if (myTarget == null || closestPlayers.Count == 0 || closestPlayers.All(p => p.PlayerId != myTarget.PlayerId)) return;
        dousedPlayers.Add(myTarget.PlayerId);
        SuccessfulDouseEffects(myTarget);
        myTarget = null;
    }

    private void SuccessfulDouseEffects(PlayerControl target)
    {
        DynamicName targetName = target.GetDynamicName();
        targetName.AddRule(GameState.Roaming, UI.Counter, new DynamicString(RoleColor.Colorize("★")), MyPlayer.PlayerId);
        targetName.AddRule(GameState.InMeeting, UI.Name, new DynamicString(RoleColor.Colorize("{0} ★")), MyPlayer.PlayerId);
        targetName.RenderFor(MyPlayer);

        GameOptionOverride[] overrides = { new(Override.ImpostorLightMod, 0f) };
        SyncOptions(overrides);
        Async.Schedule(SyncOptions, 0.3f);
    }

    [RoleInteraction(typeof(Veteran))]
    private InteractionResult ArsoVetInteraction(PlayerControl veteran) => VeteranInteraction(veteran);

    [RoleInteraction(typeof(Pestilence))]
    [RoleInteraction(typeof(Phantom))]
    private InteractionResult NotDousable() => InteractionResult.Halt;

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Color(RoleColor)
            .AddSubOption(sub => sub
                .Name("Time Until Douse Complete")
                .Bind(v => dousingDuration.Duration = (float)v)
                .AddFloatRangeValues(0.5f, 5, 0.25f, 3, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Strict Dousing")
                .Bind(v => strictDousing = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(sub => sub
                .Name("Douse Cooldown")
                .Bind(v => douseCooldown = (float)v)
                .AddFloatRangeValues(5, 60, 2.5f, 4, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(1f, 0.4f, 0.2f))
            .CanVent(false)
            .OptionOverride(Override.PlayerSpeedMod, 0f, () => IsDousing)
            .OptionOverride(Override.KillCooldown, () => douseCooldown * 2);
}