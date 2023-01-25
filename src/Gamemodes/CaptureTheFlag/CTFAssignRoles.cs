using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Gamemodes.Colorwars;
using TownOfHost.Managers;
using TownOfHost.Roles;
using VentLib.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TownOfHost.Gamemodes.CaptureTheFlag;

public class CTFAssignRoles
{
    public static void AssignRoles(List<PlayerControl> players)
    {
        List<PlayerControl> mutablePlayers = new(players);
        List<List<PlayerControl>> teams = new() { new List<PlayerControl>(), new List<PlayerControl>() };

        int playerCount = mutablePlayers.Count;
        for (int i = 0; i < playerCount; i++)
        {
            PlayerControl player = mutablePlayers.PopRandom();
            byte color = (byte)(i % 2);
            player.RpcSetColor(color);
            teams[color].Add(player);
            Async.Schedule(() => Utils.Teleport(player.NetTransform, CTFGamemode.SpawnLocations[color]), NetUtils.DeriveDelay(0.5f));
        }

        AssignTeams(teams, players);
    }

    public static void AssignTeams(List<List<PlayerControl>> teams, List<PlayerControl> players)
    {
        PlayerControl localPlayer = PlayerControl.LocalPlayer;
        foreach (List<PlayerControl> team in teams)
        {
            int[] teamClientIds = team.Select(p => p.GetClientId()).ToArray();
            foreach (PlayerControl player in team)
            {
                if (player.PlayerId == localPlayer.PlayerId) player.SetRole(RoleTypes.Impostor);
                RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((ushort)RoleTypes.Impostor).SendInclusive(teamClientIds);
                RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((ushort)RoleTypes.Crewmate).SendExclusive(teamClientIds);
                Game.AssignRole(player, CTFGamemode.Striker);
            }
        }

        byte[] hostTeam = teams.FirstOrDefault(team => team.Any(p => p.PlayerId == localPlayer.PlayerId))!.Select(p => p.PlayerId).ToArray();
        players.Where(p => p.PlayerId != localPlayer.PlayerId).Do(p => p.SetRole(hostTeam.Contains(p.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate));
    }
}