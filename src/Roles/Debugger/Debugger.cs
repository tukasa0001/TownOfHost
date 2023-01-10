using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using TownOfHost.Victory.Conditions;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace TownOfHost.Roles;

public class Debugger: CustomRole
{
    private RoleTypes baseRole;
    private bool customSyncOptions;
    private HideAndSeekTimerBar timerBar;
    private int counter = 1;

    private Component progressTracker;


    [RoleAction(RoleActionType.OnPet)]
    private void OnPet()
    {
        Logger.Info("OnPet Called", "DebuggerCall");
        LogStats();

        counter++;
    }

    private void CustomWinTest()
    {
        ManualWin manualWin = new(new List<PlayerControl> { MyPlayer }, WinReason.RoleSpecificWin);
        manualWin.Activate();
    }

    private void RangeTest()
    {
        Vector2 location = MyPlayer.GetTruePosition();
        foreach (PlayerControl player in Game.GetAlivePlayers().Where(p => p.PlayerId != MyPlayer.PlayerId))
            Logger.Info($"Distance from {MyPlayer.GetRawName()} to {player.GetRawName()} :: {Vector2.Distance(location, player.GetTruePosition())}", "DebuggerDistance");
    }

    private void LogStats()
    {
        Logger.Info($"{MyPlayer.GetNameWithRole()} | Dead? {MyPlayer.Data.IsDead} | AURole: {MyPlayer.Data.Role.name} | Custom Role: {MyPlayer.GetCustomRole().RoleName.RemoveHtmlTags()} | Subrole: {MyPlayer.GetSubrole()?.RoleName}", "DebuggerStats");
        Logger.Info($"Stats | Total Players: {Game.GetAllPlayers().Count()} | Alive Players: {Game.GetAlivePlayers().Count()} | Impostors: {GameStats.CountAliveImpostors()}", "DebuggerStats");
        Logger.Info("-=-=-=-=-=-=-=-=-=-=-=-= Other Players =-=-=-=-=-=-=-=-=-=-=-=-", "DebuggerStats");
        foreach (PlayerControl player in Game.GetAllPlayers().Where(p => p.PlayerId != MyPlayer.PlayerId))
            Logger.Info($"{player.GetNameWithRole()} | Dead? {player.Data.IsDead} | AURole: {player.Data.Role.name} | Custom Role: {player.GetCustomRole().RoleName.RemoveHtmlTags()} | Subrole: {player.GetSubrole()?.RoleName}", "DebuggerStats");

        Logger.Info("-=-=-=-=-=-=-=-=-=- Role Blocked Players -=-=-=-=-=-=-=-=-=-", "DebuggerStats");
        foreach (byte playerId in CustomRoleManager.RoleBlockedPlayers.Distinct())
        {
            int count = CustomRoleManager.RoleBlockedPlayers.Count(b => b == playerId);
            Logger.Info($"{Utils.GetPlayerById(playerId).GetNameWithRole()}: {count}", "DebuggerStats");
        }

        Logger.Info("-=-=-=-=-=-=-=-= End Of Debugger =-=-=-=-=-=-=-=-", "DebuggerStats");
    }


    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Name("<b><color=#FF0000>D</color><color=#FFBF00>e</color><color=#7FFF00>b</color><color=#00FF3F>u</color><color=#00FEFF>g</color><color=#003FFF>g</color><color=#7F00FF>e</color><color=#FF00BF>r</color></b>")
            .AddSubOption(sub => sub
                .Name("Base Role")
                .Bind(v => baseRole = (RoleTypes)Convert.ToUInt16(v))
                .AddValue(v => v.Text("Crewmate").Value(0).Build())
                .AddValue(v => v.Text("Impostor").Value(1).Build())
                .AddValue(v => v.Text("Scientist").Value(2).Build())
                .AddValue(v => v.Text("Engineer").Value(3).Build())
                .AddValue(v => v.Text("GuardianAngel").Value(4).Build())
                .AddValue(v => v.Text("Shapeshifter").Value(5).Build())
                .AddValue(v => v.Text("CrewmateGhost").Value(6).Build())
                .AddValue(v => v.Text("ImpostorGhost").Value(7).Build())
                .Build())
            .AddSubOption(sub => sub
                .Name("Use Custom Sync Options")
                .BindBool(v => customSyncOptions = v)
                .AddOnOffValues(false)
                .Build());


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleName(
                "<b><color=#FF0000>D</color><color=#FFBF00>e</color><color=#7FFF00>b</color><color=#00FF3F>u</color><color=#00FEFF>g</color><color=#003FFF>g</color><color=#7F00FF>e</color><color=#FF00BF>r</color></b>")
            .RoleColor(new Color(0.84f, 1f, 0.64f))
            .VanillaRole(baseRole);

}