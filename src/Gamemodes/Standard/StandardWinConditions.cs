using System.Collections.Generic;
using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.Managers;
using TOHTOR.Roles;
using TOHTOR.Roles.Legacy;
using TOHTOR.Roles.Subrole;
using TOHTOR.Victory.Conditions;

namespace TOHTOR.Gamemodes.Standard;

public static class StandardWinConditions
{
    public class SoloRoleWin : IWinCondition
    {
        public bool IsConditionMet(out List<PlayerControl> winners)
        {
            winners = null;
            List<PlayerControl> allPlayers = Game.GetAllPlayers().ToList();
            if (allPlayers.Count != 1) return false;

            PlayerControl lastPlayer = allPlayers[0];
            return lastPlayer.GetCustomRole().Factions.IsSolo();
        }

        public WinReason GetWinReason() => Victory.Conditions.WinReason.FactionLastStanding;
    }

    public class SoloKillingWin : IWinCondition
    {
        public bool IsConditionMet(out List<PlayerControl> winners)
        {
            winners = null;
            List<PlayerControl> alivePlayers = Game.GetAlivePlayers().ToList();
            if (alivePlayers.Count > 2 || GameStates.CountAliveImpostors() > 0) return false;

            // Maybe add a setting for crewmate killing to be able to duel neutral killing :thinking:
            List<PlayerControl> soloKilling = alivePlayers.Where(p => p.GetCustomRole().Factions.IsSolo() && p.GetCustomRole().IsNeutralKilling()).ToList();
            if (soloKilling.Count != 1) return false;
            winners = new List<PlayerControl> { soloKilling[0] };
            return true;
        }

        public WinReason GetWinReason() => Victory.Conditions.WinReason.FactionLastStanding;
    }

    public class LoversWin : IWinCondition
    {
        public bool IsConditionMet(out List<PlayerControl> winners)
        {
            winners = null;
            if (Game.GetAlivePlayers().Count() > 3) return false;
            List<PlayerControl> lovers = Game.FindAlivePlayersWithRole(CustomRoleManager.Special.Lovers).ToList();
            if (lovers.Count != 2) return false;
            Lovers loversRole = lovers[0].GetSubrole<Lovers>()!;
            winners = lovers;
            return loversRole.Partner != null && loversRole.Partner.PlayerId == lovers[1].PlayerId;
        }

        public WinReason GetWinReason() => Victory.Conditions.WinReason.RoleSpecificWin;

        public int Priority() => 100;
    }

}