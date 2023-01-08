using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
// ReSharper disable ConvertIfStatementToSwitchStatement

namespace TownOfHost.Victory;

public static class VictoryScreen
{
    public static void ShowWinners(List<PlayerControl> winners, GameOverReason reason)
    {
        bool impostorsWin = IsImpostorsWin(reason);

        List<PlayerControl> allPlayers = Game.GetAllPlayers().ToList();

        winners.Do(winner =>
        {
            if (impostorsWin && !winner.Data.Role.IsImpostor) winner.RpcSetRole(RoleTypes.ImpostorGhost);
            if (!impostorsWin && winner.Data.Role.IsImpostor) winner.RpcSetRole(RoleTypes.CrewmateGhost);
            allPlayers.RemoveAll(p => p.PlayerId == winner.PlayerId);
        });

        allPlayers.Do(loser =>
        {
            if (impostorsWin && loser.Data.Role.IsImpostor) loser.RpcSetRole(RoleTypes.CrewmateGhost);
            if (!impostorsWin && !loser.Data.Role.IsImpostor) loser.RpcSetRole(RoleTypes.ImpostorGhost);
        });

        allPlayers.Select(p => p.GetRawName()).PrettyString().DebugLog("LOSERRS HAHAHAHAHAHHAHAHAH: ");
    }

    private static bool IsImpostorsWin(GameOverReason reason)
    {
        return reason switch
        {
            GameOverReason.HumansByVote => false,
            GameOverReason.HumansByTask => false,
            GameOverReason.ImpostorByVote => true,
            GameOverReason.ImpostorByKill => true,
            GameOverReason.ImpostorBySabotage => true,
            GameOverReason.ImpostorDisconnect => false,
            GameOverReason.HumansDisconnect => true,
            GameOverReason.HideAndSeek_ByTimer => false,
            GameOverReason.HideAndSeek_ByKills => true,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
    }
}