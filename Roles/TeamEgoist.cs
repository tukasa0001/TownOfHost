using System.Collections.Generic;
using System.Linq;

namespace TownOfHost
{
    public static class TeamEgoist
    {
        public static List<byte> playerIdList = new();

        public static void Add(byte teamEgo)
        {
            playerIdList.Add(teamEgo);
        }
        public static bool CompleteWinCondition(byte id) => Main.currentWinner == CustomWinner.Impostor && !PlayerState.isDead[id] && !PlayerControl.AllPlayerControls.ToArray().Any(p => p.Is(RoleType.Impostor) && !PlayerState.isDead[p.PlayerId]);
        public static void SoloWin(List<PlayerControl> winner)
        {
            if (Main.currentWinner == CustomWinner.Egoist && CustomRoles.Egoist.IsEnable()) //横取り勝利
            {
                winner.Clear();
                foreach (var id in playerIdList)
                {
                    var teamEgo = Utils.GetPlayerById(id);
                    if (teamEgo == null) continue;
                    if (teamEgo.Is(CustomRoles.Lovers)) continue; //リア充は無視
                    else if ((teamEgo.Is(CustomRoles.Egoist) && !PlayerState.isDead[id]) ||
                        teamEgo.Is(CustomRoles.EgoSchrodingerCat))
                    {
                        winner.Add(teamEgo);
                    }
                }
            }
        }
    }
}