using System;
using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.API;
using TownOfHost.Options;
using TownOfHost.Victory.Conditions;
using VentLib.Logging;
using VentLib.Utilities;

namespace TownOfHost.Victory;

[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
public class CheckEndGamePatch2
{
    private static bool _deferred;

    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (_deferred) return false;
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
            WinReason.Sabotage => GameOverReason.ImpostorBySabotage,
            WinReason.NoWinCondition => GameOverReason.ImpostorDisconnect,
            WinReason.HostForceEnd => GameOverReason.ImpostorDisconnect,
            WinReason.GamemodeSpecificWin => GameOverReason.ImpostorByKill,
            _ => throw new ArgumentOutOfRangeException()
        };


        VictoryScreen.ShowWinners(winDelegate.GetWinners(), reason);

        _deferred = true;
        Async.Schedule(() => DelayedWin(reason), NetUtils.DeriveDelay(0.6f));

        return false;
    }

    private static void DelayedWin(GameOverReason reason)
    {
        _deferred = false;
        VentLogger.Info("Ending Game", "DelayedWin");
        GameManager.Instance.RpcEndGame(reason, false);
        Async.Schedule(() => GameManager.Instance.EndGame(), 0.1f);
    }
}