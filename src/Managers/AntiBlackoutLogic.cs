using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles.Legacy;
using VentLib.Logging;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Managers;

public static class AntiBlackoutLogic
{


    public static void PatchedDataLogic()
    {
        VentLogger.Debug("Patching GameData", "AntiBlackout");
        int aliveCrew = GameStates.CountRealCrew();
        int aliveImpostors = GameStates.CountRealImpostors();

        if (AntiBlackout.FakeExiled != null)
        {
            if (AntiBlackout.FakeExiled.GetCustomRole().RealRole.IsImpostor()) aliveImpostors--;
            else aliveCrew--;
        }

        VentLogger.Debug($"Real Crew: {aliveCrew} | Real Impostors: {aliveImpostors}", "AntiBlackout");
        //if (aliveCrew > aliveImpostors) AntiBlackoutManager.RestoreIsDead();
        GameData.PlayerInfo[] allPlayers = GameData.Instance.AllPlayers.ToArray();

        foreach (PlayerControl player in Game.GetAllPlayers().Sorted(p => p.IsHost()))
        {
            int localImpostors = aliveImpostors;
            /*CustomRole playerRole = player.GetCustomRole();
            if (playerRole.IsDesyncRole() && playerRole.RealRole.IsImpostor())
                localImpostors = Math.Max(localImpostors, playerRole.Factions.GetAllies().Count);*/
            ReviveEveryone();
            VentLogger.Trace($"Patching for {player.GetRawName()}");
            foreach (var info in allPlayers.Where(p => AntiBlackout.FakeExiled != p).Sorted(p => p.Object.IsHost()))
            {
                if (localImpostors < aliveCrew) continue;
                if (player.PlayerId == info.PlayerId) continue;

                if (info.Object.GetCustomRole().RealRole.IsCrewmate() || !info.Object.GetCustomRole().IsAllied(player)) continue;
                if (info.Object.IsHost())
                {
                    VentLogger.Trace($"Set {info.Object.GetRawName()} => isDead = true");
                    info.IsDead = true;
                }
                else
                {
                    VentLogger.Trace($"Set {info.Object.GetRawName()} => Disconnected = true");
                    info.Disconnected = true;
                }
                VentLogger.Trace($"Local Impostors {localImpostors} => {localImpostors - 1}");
                localImpostors--;
            }
            AntiBlackout.SendGameData(player.GetClientId());
        }
    }

    private static void ReviveEveryone() {
        foreach (var info in GameData.Instance.AllPlayers)
        {
            info.IsDead = false;
            info.Disconnected = false;
        }
    }

    public static bool IsFakeable(GameData.PlayerInfo? checkedPlayer)
    {
        if (checkedPlayer == null || checkedPlayer.Object == null) return false;
        int aliveCrew = GameStates.CountRealCrew();
        int aliveImpostors = GameStates.CountRealImpostors();

        if (checkedPlayer.Object.GetCustomRole().RealRole.IsImpostor()) aliveImpostors -= 1;
        else aliveCrew -= 1;

        return aliveCrew > aliveImpostors && aliveImpostors != 0;
    }
}