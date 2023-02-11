using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Gamemodes;
using TownOfHost.GUI;
using TownOfHost.Managers.History;
using TownOfHost.Options;
using TownOfHost.Player;
using TownOfHost.Roles;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using TownOfHost.RPC;
using TownOfHost.Victory;
using VentLib.Logging;
using VentLib.RPC;
using VentLib.RPC.Attributes;
using VentLib.Utilities.Extensions;

namespace TownOfHost.API;

public static class Game
{
    public static DateTime StartTime;
    public static Dictionary<byte, PlayerPlus> Players = new();
    public static GameHistory GameHistory = null!;
    public static GameStates GameStates = null!;
    public static RandomSpawn RandomSpawn = null!;

    [ModRPC((uint) ModCalls.SetCustomRole, RpcActors.Host, RpcActors.NonHosts, MethodInvocation.ExecuteBefore)]
    public static void AssignRole(PlayerControl player, CustomRole role, bool sendToClient = false)
    {
        CustomRole assigned = CustomRoleManager.PlayersCustomRolesRedux[player.PlayerId] = role.Instantiate(player);
        if (sendToClient) assigned.Assign();
    }

    [ModRPC((uint) ModCalls.SetSubrole, RpcActors.Host, RpcActors.NonHosts, MethodInvocation.ExecuteBefore)]
    public static void AssignSubrole(PlayerControl player, Subrole role, bool sendToClient = false)
    {
        Dictionary<byte, List<Subrole>> playerSubroles = CustomRoleManager.PlayerSubroles;
        byte playerId = player.PlayerId;

        if (!playerSubroles.ContainsKey(playerId)) playerSubroles[playerId] = new List<Subrole>();
        playerSubroles[playerId].Add((Subrole)role.Instantiate(player));
        if (sendToClient) role.Assign();
    }

    public static DynamicName GetDynamicName(this PlayerControl playerControl) => Players[playerControl.PlayerId].DynamicName;
    public static PlayerPlus GetPlayerPlus(this PlayerControl playerControl) => Players[playerControl.PlayerId];

    public static void RenderAllNames() => Players.Values.Select(p => p.DynamicName).Do(name => name.Render());
    public static void RenderAllForAll(GameState? state = null, bool force = false) => Players.Values.Select(p => p.DynamicName).Do(name => Players.Values.Do(p => name.RenderFor(p.MyPlayer, state, force)));
    public static IEnumerable<PlayerControl> GetAllPlayers() => PlayerControl.AllPlayerControls.ToArray();
    public static IEnumerable<PlayerControl> GetAlivePlayers() => GetAllPlayers().Where(p => !p.Data.IsDead && !p.Data.Disconnected);
    public static IEnumerable<PlayerControl> GetDeadPlayers(bool disconnected = false) => GetAllPlayers().Where(p => p.Data.IsDead || (disconnected && p.Data.Disconnected));
    public static List<PlayerControl> GetAliveImpostors() => GetAlivePlayers().Where(p => p.GetCustomRole().Factions.IsImpostor()).ToList();
    public static PlayerControl GetHost() => GetAllPlayers().FirstOrDefault(p => p.NetId == RpcV2.GetHostNetId());

    public static IEnumerable<PlayerControl> FindAlivePlayersWithRole(params CustomRole[] roles) =>
        GetAllPlayers()
            .Where(p => roles.Any(r => r.Is(p.GetCustomRole()) || p.GetSubroles().Any(s => s.Is(r))));


    public static void SyncAll() => GetAllPlayers().Do(p => p.GetCustomRole().SyncOptions());
    public static void TriggerForAll(RoleActionType action, ref ActionHandle handle, params object[] parameters)
    {
        if (action == RoleActionType.FixedUpdate)
            foreach (PlayerControl player in GetAllPlayers()) player.Trigger(action, ref handle, parameters);
        // Using a new Trigger algorithm to deal with ordering of triggers
        else
        {
            handle.ActionType = action;
            parameters = parameters.AddToArray(handle);
            List<(RoleAction, AbstractBaseRole)> actionList = GetAllPlayers().SelectMany(p => p.GetCustomRole().GetActions(action)).ToList();
            actionList.AddRange(GetAllPlayers().SelectMany(p => p.GetSubroles().SelectMany(r => r.GetActions(action))));
            actionList.Sort((a1, a2) => a1.Item1.Priority.CompareTo(a2.Item1.Priority));
            foreach ((RoleAction roleAction, AbstractBaseRole role) in actionList)
            {
                bool inBlockList = role.MyPlayer != null && CustomRoleManager.RoleBlockedPlayers.Contains(role.MyPlayer.PlayerId);
                if (StaticOptions.LogAllActions)
                {
                    VentLogger.Trace($"{role.MyPlayer.GetNameWithRole()} => {roleAction}", "ActionLog");
                    VentLogger.Trace($"Parameters: {parameters.StrJoin()} :: Blocked? {roleAction.Blockable && inBlockList}", "ActionLog");
                }

                if (!roleAction.Blockable || !inBlockList)
                    roleAction.Execute(role, parameters);
            }

        }
    }

    public static IGamemode CurrentGamemode => TOHPlugin.GamemodeManager.CurrentGamemode;

    //public static void ResetNames() => players.Values.Select(p => p.DynamicName).Do(name => name.ClearComponents());
    public static GameState State = GameState.InLobby;
    private static WinDelegate _winDelegate;

    public static WinDelegate GetWinDelegate() => _winDelegate;

    public static void Setup()
    {
        RandomSpawn = new RandomSpawn();
        StartTime = DateTime.Now;
        GameHistory = new();
        GameStates = new();
        Players.Clear();
        GetAllPlayers().Do(p => Players.Add(p.PlayerId, new PlayerPlus(p)));
        _winDelegate = new WinDelegate();
        CurrentGamemode.SetupWinConditions(_winDelegate);
    }

    public static void Cleanup()
    {
        Players.Clear();
        CustomRoleManager.PlayersCustomRolesRedux.Clear();
    }
}

public enum GameState
{
    None,
    InIntro,
    InMeeting,
    InLobby,
    Roaming // When in Rome do as the Romans do
}