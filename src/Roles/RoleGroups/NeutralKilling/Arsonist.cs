using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Managers.History.Events;
using TOHTOR.Roles.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Crew;
using TOHTOR.Roles.RoleGroups.Neutral;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Roles.RoleGroups.NeutralKilling;

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
    private void KillDoused() => dousedPlayers.Filter(p => Utils.PlayerById(p)).Where(p => p.IsAlive()).Do(p =>
    {
        FatalIntent intent = new(true, () => new IncineratedDeathEvent(p, MyPlayer));
        IndirectInteraction interaction = new(intent, this);
        MyPlayer.InteractWith(p, interaction);
    });

    [RoleAction(RoleActionType.Attack)]
    private void StartDousePlayer(PlayerControl target)
    {
        if (MyPlayer.InteractWith(target, SimpleInteraction.HostileInteraction.Create(this)) is InteractionResult.Halt) return;
        MyPlayer.RpcGuardAndKill(target);
        myTarget = target;
        dousingDuration.StartThenRun(EndDousePlayer);
        SyncOptions();
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
        dousedPlayers.RemoveWhere(p => Utils.PlayerById(p).Transform(pp => !pp.IsAlive(), () => true));
    }

    private void EndDousePlayer()
    {
        List<PlayerControl> closestPlayers = MyPlayer.GetPlayersInAbilityRangeSorted();
        MyPlayer.RpcGuardAndKill(MyPlayer);
        SyncOptions();
        if (myTarget == null || closestPlayers.Count == 0 || closestPlayers.All(p => p.PlayerId != myTarget.PlayerId)) return;
        dousedPlayers.Add(myTarget.PlayerId);
        SuccessfulDouseEffects(myTarget);
        myTarget = null!;
    }

    private void SuccessfulDouseEffects(PlayerControl target)
    {
        DynamicName targetName = target.GetDynamicName();
        targetName.AddRule(GameState.Roaming, UI.Counter, new DynamicString(RoleColor.Colorize("★")), MyPlayer.PlayerId);
        targetName.AddRule(GameState.InMeeting, UI.Name, new DynamicString(RoleColor.Colorize("{0} ★")), MyPlayer.PlayerId);
        targetName.RenderFor(MyPlayer);

        GameOptionOverride[] overrides = { new(Override.ImpostorLightMod, 0f) };
        SyncOptions(overrides);
        Game.GameHistory.AddEvent(new PlayerDousedEvent(MyPlayer, target));
        Async.Schedule(SyncOptions, 0.3f);
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Color(RoleColor)
            .SubOption(sub => sub
                .Name("Time Until Douse Complete")
                .Bind(v => dousingDuration.Duration = (float)v)
                .AddFloatRange(0.5f, 5, 0.25f, 3, "s")
                .Build())
            .SubOption(sub => sub
                .Name("Strict Dousing")
                .Bind(v => strictDousing = (bool)v)
                .AddOnOffValues()
                .Build())
            .SubOption(sub => sub
                .Name("Douse Cooldown")
                .Bind(v => douseCooldown = (float)v)
                .AddFloatRange(5, 60, 2.5f, 4, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .RoleColor(new Color(1f, 0.4f, 0.2f))
            .CanVent(false)
            .OptionOverride(Override.PlayerSpeedMod, 0f, () => IsDousing)
            .OptionOverride(Override.KillCooldown, () => douseCooldown * 2);


    class PlayerDousedEvent : TargetedAbilityEvent, IRoleEvent
    {
        public PlayerDousedEvent(PlayerControl source, PlayerControl target, bool successful = true) : base(source, target, successful)
        {
        }

        public override string Message() => $"{Game.GetName(Player())} doused {Game.GetName(Target())}.";
    }

    class IncineratedDeathEvent : DeathEvent
    {
        public IncineratedDeathEvent(PlayerControl deadPlayer, PlayerControl? killer) : base(deadPlayer, killer)
        {
        }

        public override string SimpleName() => ModConstants.DeathNames.Incinerated;
    }
}