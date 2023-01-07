using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Gamemodes;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;
using TownOfHost.RPC;
using VentFramework;

namespace TownOfHost.Managers;

// Managers should be non-static this one is just because im lazy :)
// Entry points = OnJoin & OnLeave
public static class Game
{
    public static Dictionary<byte, PlayerPlus> players = new();

    [ModRPC((uint) ModCalls.SetCustomRole, RpcActors.Host, RpcActors.NonHosts, MethodInvocation.ExecuteBefore)]
    public static void AssignRole(PlayerControl player, CustomRole role, bool sendToClient = false)
    {
        CustomRoleManager.PlayersCustomRolesRedux[player.PlayerId] = role.Instantiate(player);
        if (sendToClient) role.Assign();
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

    public static DynamicName GetDynamicName(this PlayerControl playerControl) => players[playerControl.PlayerId].DynamicName;
    public static PlayerPlus GetPlayerPlus(this PlayerControl playerControl) => players[playerControl.PlayerId];

    public static void RenderAllNames() => players.Values.Select(p => p.DynamicName).Do(name => name.Render());
    public static void RenderAllForAll(GameState? state = null) => players.Values.Select(p => p.DynamicName).Do(name => players.Values.Do(p => name.RenderFor(p.MyPlayer, state)));
    //public static void RenderAllForAll(GameState? state = null) => GetAllPlayers().Select(p => p.GetDynamicName()).Do(name => GetAllPlayers().Do(pp => name.RenderFor(pp, state)));
    public static IEnumerable<PlayerControl> GetAllPlayers() => PlayerControl.AllPlayerControls.ToArray();
    public static IEnumerable<PlayerControl> GetAlivePlayers() => GetAllPlayers().Where(p => !p.Data.IsDead && !p.Data.Disconnected);
    public static List<PlayerControl> GetAliveImpostors() => GetAlivePlayers().Where(p => p.GetCustomRole().Factions.IsImpostor()).ToList();
    public static PlayerControl GetHost() => GetAllPlayers().FirstOrDefault(p => p.NetId == RpcV2.GetHostNetId());

    public static List<PlayerControl> FindPlayersWithRole(bool aliveOnly = false, params CustomRole[] roles) =>
        GetAllPlayers()
            .Where(p => p.IsAlive() || !aliveOnly)
            .Where(p => roles.Any(r => r.Is(p.GetCustomRole()) || p.GetSubroles().Any(s => s.Is(r))))
            .ToList();


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
            List<Tuple<MethodInfo, RoleAction, AbstractBaseRole>> actionList = GetAllPlayers().SelectMany(p => p.GetCustomRole().GetActions(action)).ToList();
            actionList.AddRange(GetAllPlayers().SelectMany(p => p.GetSubroles().SelectMany(r => r.GetActions(action))));
            actionList.Sort((a1, a2) => a1.Item2.Priority.CompareTo(a2.Item2.Priority));
            foreach (Tuple<MethodInfo, RoleAction, AbstractBaseRole> actionTuple in actionList)
            {
                bool inBlockList = actionTuple.Item3.MyPlayer != null && CustomRoleManager.RoleBlockedPlayers.Contains(actionTuple.Item3.MyPlayer.PlayerId);
                if (StaticOptions.LogAllActions)
                {
                    Logger.Blue($"{actionTuple.Item3.MyPlayer.GetNameWithRole()} => {actionTuple.Item2}", "ActionLog");
                    Logger.Blue($"Parameters: {parameters.PrettyString()} :: Blocked? {actionTuple.Item2.Blockable && inBlockList}", "ActionLog");
                }

                if (!actionTuple.Item2.Blockable || !inBlockList)
                    actionTuple.Item1.InvokeAligned(actionTuple.Item3, parameters);
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
        players.Clear();
        GetAllPlayers().Do(p => players.Add(p.PlayerId, new PlayerPlus(p)));
        _winDelegate = new WinDelegate();
        CurrentGamemode.SetupWinConditions(_winDelegate);
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