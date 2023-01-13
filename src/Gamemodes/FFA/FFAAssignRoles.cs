using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Roles;
using VentLib;

namespace TownOfHost.Gamemodes.FFA;

// ReSharper disable once InconsistentNaming
public static class FFAAssignRoles
{
    public static void AssignRoles(List<PlayerControl> players)
    {
        PlayerControl localPlayer = PlayerControl.LocalPlayer;
        localPlayer.SetRole(RoleTypes.Impostor);

        foreach (PlayerControl player in players)
        {
            RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((ushort)RoleTypes.Impostor).Send(player.GetClientId());
            RpcV2.Immediate(player.NetId, (byte)RpcCalls.SetRole).Write((ushort)RoleTypes.Crewmate).SendExclusive(player.GetClientId());
            Game.AssignRole(player, CustomRoleManager.Static.SerialKiller);
        }

        players.Where(p => p.PlayerId != localPlayer.PlayerId).Do(p => p.SetRole(RoleTypes.Crewmate));
    }
}