using System.Collections.Generic;
using AmongUs.GameOptions;
using Il2CppSystem;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.GUI;
using TownOfHost.Options;
using TownOfHost.Roles.Neutral;
using UnityEngine;

namespace TownOfHost.Roles;

public class Sheriff : Crewmate
{
    public bool sheriffHasTasks;

    [DynElement(UI.Cooldown)]
    private Cooldown shootCooldown;
    private int totalShots;
    private bool oneShotPerRound;
    private bool canKillCrewmates;
    private bool isSheriffDesync;


    private bool shotThisRound;
    private int shotsRemaining;


    protected override void Setup(PlayerControl player) => shotsRemaining = totalShots;
    public bool HasShots() => !(oneShotPerRound && shotThisRound) && shotsRemaining >= 0;


    [DynElement(UI.Counter)]
    public string RemainingShotCounter() => $"({Utils.ColorString(Color.yellow, $"{shotsRemaining}/{totalShots}")})";

    // ACTIONS

    [RoleAction(RoleActionType.RoundStart)]
    public bool RefreshShotThisRound() => shotThisRound = false;

    [RoleAction(RoleActionType.OnPet)]
    public bool TryKillWithPet(ActionHandle handle)
    {
        handle.Cancel();
        if (isSheriffDesync || !shootCooldown.IsReady() || !HasShots()) return false;
        List<PlayerControl> closestPlayers = MyPlayer.GetPlayersInAbilityRangeSorted(false);
        if (closestPlayers.Count == 0) return false;
        PlayerControl target = closestPlayers[0];
        return TryKill(target, handle);
    }


    [RoleAction(RoleActionType.AttemptKill)]
    public bool TryKill(PlayerControl target, ActionHandle handle)
    {
        handle.Cancel();
        if (!shootCooldown.IsReady() || !HasShots()) return false;
        shotsRemaining--;
        shootCooldown.Start();

        InteractionResult result = CheckInteractions(target.GetCustomRole(), target, true);
        if (result == InteractionResult.Halt) return false;
        MyPlayer.RpcMurderPlayer(target);
        return true;
    }

    [RoleInteraction(Faction.Crewmates)]
    private InteractionResult Suicide(PlayerControl target, bool checkCrewmates)
    {
        MyPlayer.RpcMurderPlayer(MyPlayer);
        if (checkCrewmates && canKillCrewmates)
            MyPlayer.RpcMurderPlayer(target);
        return InteractionResult.Halt;
    }


    // INTERACTIONS


    [RoleInteraction(typeof(Veteran))]
    public InteractionResult VeteranInteraction(PlayerControl vet) => vet.GetCustomRole<Veteran>().TryKill(MyPlayer) ? InteractionResult.Halt : InteractionResult.Proceed;


    // Kill targets that are toggable via the options
    [RoleInteraction(typeof(Jester))]
    [RoleInteraction(typeof(Glitch))]
    public InteractionResult OptionalSheriffKills(PlayerControl target)
    {
        if (target.GetCustomRole().CanBeKilledBySheriff()) return InteractionResult.Proceed;
        // If here we can't kill the target so RIP Sheriff
        this.Suicide(target, false);
        return InteractionResult.Halt;
    }

    // Medusa
    [RoleInteraction(typeof(NotImplementedException))]
    public InteractionResult MedusaInteraction(PlayerControl medusa)
    {
        //if (!Main.IsGazing) return InteractionResult.Proceed;

        medusa.RpcMurderPlayer(MyPlayer);
        new DTask(() =>
        {
            TOHPlugin.unreportableBodies.Add(MyPlayer.PlayerId);
        }, StaticOptions.StoneReport, "Medusa Stone Gazing");
        return InteractionResult.Halt;
    }

    // OPTIONS

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Color(RoleColor)
            .AddSubOption(sub => sub
                .Name("Can Kill Crewmates")
                .Bind(v => canKillCrewmates = (bool)v)
                .AddOnOffValues(false)
                .Build())
            .AddSubOption(sub => sub
                .Name("Kill Cooldown")
                .Bind(v => this.shootCooldown.Duration = Convert.ToSingle((int)v))
                .AddValues(3, 10, 15, 20, 25, 30)
                .Build())
            .AddSubOption(sub => sub
                .Name("Total Shots")
                .Bind(v => this.totalShots = (int)v)
                .AddValues(1..5, 4)
                .Build())
            .AddSubOption(sub => sub
                .Name("One Shot Per Round")
                .Bind(v => this.oneShotPerRound = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(sub => sub
                .Name("Sheriff Action Button")
                .Bind(v => isSheriffDesync = (bool)v)
                .AddValue(v => v.Text("Kill Button (legacy)").Value(true).Color(Color.green).Build())
                .AddValue(v => v.Text("Pet Button").Value(false).Color(Color.cyan).Build())
                .ShowSubOptionsWhen(v => !(bool)v)
                .AddSubOption(sub2 => sub2
                    .Name("Sheriff Has Tasks")
                    .Bind(v => this.sheriffHasTasks = (bool)v)
                    .AddOnOffValues()
                    .Build())
                .Build());

    // Sheriff is not longer a desync role for simplicity sake && so that they can do tasks
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Crewmate)
            .DesyncRole(!isSheriffDesync ? null : RoleTypes.Impostor)
            .Factions(Faction.Crewmates)
            .RoleColor(new Color(0.97f, 0.8f, 0.27f));
}