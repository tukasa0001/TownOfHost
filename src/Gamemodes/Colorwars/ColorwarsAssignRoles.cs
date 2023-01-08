#nullable enable
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Roles;
using VentFramework;

namespace TownOfHost.Gamemodes.Colorwars;

public class ColorwarsAssignRoles
{
    private static readonly List<byte> ColorCodes = new() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

    public static void AssignRoles(List<PlayerControl> players)
    {
        List<PlayerControl> colorwarPlayers = new(players);

        List<List<PlayerControl>> teams = new();

        while (colorwarPlayers.Any())
        {
            List<PlayerControl> team = new();
            byte color = ColorCodes.PopRandom();
            for (int i = 0; i < ColorwarsGamemode.TeamSize && colorwarPlayers.Any(); i++)
            {
                PlayerControl player = colorwarPlayers.PopRandom();
                team.Add(player);
                player.RpcSetColor(color);
            }
            teams.Add(team);
        }

        PlayerControl localPlayer = PlayerControl.LocalPlayer;

        foreach (List<PlayerControl> team in teams)
        {
            int[] teamClientIds = team.Select(p => p.GetClientId()).ToArray();
            foreach (PlayerControl player in team)
            {
                if (player.PlayerId == localPlayer.PlayerId) player.SetRole(RoleTypes.Impostor);
                RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((byte)RoleTypes.Impostor).SendToFollowing(teamClientIds);
                RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((byte)RoleTypes.Crewmate).SendToAll(teamClientIds);
                Game.AssignRole(player, CustomRoleManager.Static.Impostor);
            }
        }

        byte[] hostTeam = teams.FirstOrDefault(team => team.Any(p => p.PlayerId == localPlayer.PlayerId))!.Select(p => p.PlayerId).ToArray();
        players.Where(p => p.PlayerId != localPlayer.PlayerId).Do(p => p.SetRole(hostTeam.Contains(p.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate));
    }

}