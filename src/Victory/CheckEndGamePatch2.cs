using System;
using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;
using TownOfHost.Victory.Conditions;
using VentLib.Logging;

namespace TownOfHost.Victory;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
public class CheckEndGamePatch2
{
    private static bool deferred;

    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (deferred) return false;
        WinDelegate winDelegate = Game.GetWinDelegate();
        if (StaticOptions.NoGameEnd)
            winDelegate.CancelGameWin();

        bool isGameWin = winDelegate.IsGameOver();
        if (!isGameWin) return false;


        List<PlayerControl> winners = winDelegate.GetWinners();
        bool impostorsWon = winners.Count == 0 || winners[0].Data.Role.IsImpostor;

        GameOverReason reason = winDelegate.GetWinReason() switch
        {
            WinReason.FactionLastStanding => impostorsWon ? GameOverReason.ImpostorByKill : GameOverReason.HumansByVote,
            WinReason.RoleSpecificWin => impostorsWon ? GameOverReason.ImpostorByKill : GameOverReason.HumansByVote,
            WinReason.TasksComplete => GameOverReason.HumansByTask,
            WinReason.NoWinCondition => GameOverReason.ImpostorDisconnect,
            WinReason.HostForceEnd => GameOverReason.ImpostorDisconnect,
            WinReason.GamemodeSpecificWin => GameOverReason.ImpostorByKill,
            _ => throw new ArgumentOutOfRangeException()
        };


        VictoryScreen.ShowWinners(winDelegate.GetWinners(), reason);

        deferred = true;
        DTask.Schedule(() => DelayedWin(reason), GameStats.DeriveDelay());

        return false;
    }

    private static void DelayedWin(GameOverReason reason)
    {
        deferred = false;
        VentLogger.Old("Sending Game Over", "DelayedWin");
        GameManager.Instance.RpcEndGame(reason, false);
        DTask.Schedule(() => GameManager.Instance.EndGame(), 0.1f);
    }
}