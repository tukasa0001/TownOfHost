using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.API;
using TownOfHost.Extensions;
using TownOfHost.RPC;
using VentLib.Logging;
using VentLib.Utilities.Extensions;

// ReSharper disable ConvertIfStatementToSwitchStatement

namespace TownOfHost.Victory;

public static class VictoryScreen
{
    public static void ShowWinners(List<PlayerControl> winners, GameOverReason reason)
    {
        List<string> winnerString = Game.GameHistory.LastWinners = winners.Select(w => w.GetNameWithRole()).ToList();
        VentLogger.Info($"Setting Up Win Screen | Winners: {winnerString.StrJoin()}");

        bool impostorsWin = IsImpostorsWin(reason);

        List<PlayerControl> losers = Game.GetAllPlayers().ToList();

        winners.Do(winner =>
        {
            if (impostorsWin && !winner.Data.Role.IsImpostor) winner.CRpcSetRole(RoleTypes.ImpostorGhost);
            if (!impostorsWin && winner.Data.Role.IsImpostor) winner.CRpcSetRole(RoleTypes.CrewmateGhost);
            losers.RemoveAll(p => p.PlayerId == winner.PlayerId);
        });

        if (winners.Any(p => p.IsHost())) winners.Do(p => p.SetRole(impostorsWin ? RoleTypes.ImpostorGhost : RoleTypes.CrewmateGhost));

        losers.Do(loser =>
        {
            if (impostorsWin && loser.Data.Role.IsImpostor) loser.CRpcSetRole(RoleTypes.CrewmateGhost);
            if (!impostorsWin && !loser.Data.Role.IsImpostor) loser.CRpcSetRole(RoleTypes.ImpostorGhost);
        });

        if (winners.Any(p => p.IsHost())) losers.Do(p => p.SetRole(impostorsWin ? RoleTypes.CrewmateGhost : RoleTypes.ImpostorGhost));
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