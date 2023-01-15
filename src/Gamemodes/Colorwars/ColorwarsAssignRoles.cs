#nullable enable
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Gamemodes.FFA;
using TownOfHost.Managers;
using TownOfHost.Roles;
using VentLib;
using VentLib.Extensions;

namespace TownOfHost.Gamemodes.Colorwars;

public static class ColorwarsAssignRoles
{
    public static void AssignRoles(List<PlayerControl> players)
    {
        List<PlayerControl> colorwarPlayers = new(players);
        List<List<PlayerControl>> teams = ColorwarsGamemode.ManualTeams ? CreateManualTeams(colorwarPlayers) : CreateRandomTeams(colorwarPlayers);

        if (ColorwarsGamemode.ConvertColorMode)
        {
            FFAAssignRoles.AssignRoles(players);
            players.Do(p => p.GetCustomRole<SerialKiller>().KillCooldown *= 2);
            return;
        }

        PlayerControl localPlayer = PlayerControl.LocalPlayer;
        foreach (List<PlayerControl> team in teams)
        {
            int[] teamClientIds = team.Select(p => p.GetClientId()).ToArray();
            foreach (PlayerControl player in team)
            {
                if (player.PlayerId == localPlayer.PlayerId) player.SetRole(RoleTypes.Impostor);
                RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((ushort)RoleTypes.Impostor).SendInclusive(teamClientIds);
                RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((ushort)RoleTypes.Crewmate).SendExclusive(teamClientIds);
                Game.AssignRole(player, CustomRoleManager.Static.SerialKiller);
            }
        }

        byte[] hostTeam = teams.FirstOrDefault(team => team.Any(p => p.PlayerId == localPlayer.PlayerId))!.Select(p => p.PlayerId).ToArray();
        players.Where(p => p.PlayerId != localPlayer.PlayerId).Do(p => p.SetRole(hostTeam.Contains(p.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate));
    }

    private static List<List<PlayerControl>> CreateRandomTeams(List<PlayerControl> players)
    {
        List<List<PlayerControl>> teams = new();
        List<byte> colorCodes = new() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        int teamSize = ColorwarsGamemode.TeamSize;
        while (players.Any())
        {
            List<PlayerControl> team = new();
            byte color = teamSize == 1 ? (byte)0 : colorCodes.PopRandom();
            for (int i = 0; i < ColorwarsGamemode.TeamSize && players.Any(); i++)
            {
                PlayerControl player = players.PopRandom();
                team.Add(player);
                if (teamSize != 1)
                    player.RpcSetColor(color);
            }
            teams.Add(team);
        }

        return teams;
    }

    private static List<List<PlayerControl>> CreateManualTeams(List<PlayerControl> players)
    {
        List<List<PlayerControl>> teams = new();
        List<byte> colorCodes = new() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
        ColorwarsGamemode.Teams.Select(t => (t.Color, t.Players.StrJoin())).StrJoin().DebugLog("Teams: ");
        foreach (ColorwarsGamemode.TeamInfo info in ColorwarsGamemode.Teams)
        {
            List<PlayerControl> team = new();
            byte color = info.Color >= 0 ? colorCodes.Pop(info.Color) : colorCodes.PopRandom();
            foreach (byte playerId in info.Players)
            {
                PlayerControl? player = players.FirstOrDefault(p => p.PlayerId == playerId);
                if (player == null) continue;
                team.Add(player);
                player.RpcSetColor(color);
            }
            teams.Add(team);
        }

        return teams;
    }
}