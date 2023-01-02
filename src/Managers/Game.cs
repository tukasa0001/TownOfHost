using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;
using TownOfHost.RPC;

namespace TownOfHost;

// Managers should be non-static this one is just because im lazy :)
// Entry points = OnJoin & OnLeave
public static class Game
{
    public static Dictionary<byte, PlayerPlus> players = new();

    public static DynamicName GetDynamicName(this PlayerControl playerControl) => players[playerControl.PlayerId].DynamicName;
    public static PlayerPlus GetPlayerPlus(this PlayerControl playerControl) => players[playerControl.PlayerId];

    public static void RenderAllNames() => players.Values.Select(p => p.DynamicName).Do(name => name.Render());
    public static IEnumerable<PlayerControl> GetAllPlayers() => PlayerControl.AllPlayerControls.ToArray();
    public static IEnumerable<PlayerControl> GetAlivePlayers() => GetAllPlayers().Where(p => !p.Data.IsDead && !p.Data.Disconnected);
    public static PlayerControl GetHost() => GetAllPlayers().FirstOrDefault(p => p.NetId == RpcV2.GetHostNetId());

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
                if (StaticOptions.logAllActions)
                {
                    string logStart = $"[Info   :TownOfHost] [{DateTime.Now:hh:mm:ss}][ActionLog]";
                    ConsoleManager.SetConsoleColor(ConsoleColor.Cyan);
                    ConsoleManager.ConsoleStream?.Write($"{logStart}{actionTuple.Item3.MyPlayer.GetNameWithRole()} => {actionTuple.Item2}\n");
                    ConsoleManager.ConsoleStream?.Write($"{logStart}Parameters: {parameters.PrettyString()} :: Blocked? {actionTuple.Item2.Blockable && inBlockList}\n");
                    ConsoleManager.SetConsoleColor(ConsoleColor.Gray);
                }

                if (!actionTuple.Item2.Blockable || !inBlockList)
                    actionTuple.Item1.InvokeAligned(actionTuple.Item3, parameters);
            }

        }
    }

    //public static void ResetNames() => players.Values.Select(p => p.DynamicName).Do(name => name.ClearComponents());

    public static int CountAliveImpostors() => GetAlivePlayers().Count(p => p.GetCustomRole().Factions.IsImpostor());

    public static GameState State = GameState.InLobby;

    public static void Setup()
    {
        players.Clear();
        GetAllPlayers().Do(p => players.Add(p.PlayerId, new PlayerPlus(p)));
    }
}

public enum GameState
{
    None,
    InIntro,
    InMeeting,
    InLobby,
    InRoam // When in Rome do as the Romans do
}