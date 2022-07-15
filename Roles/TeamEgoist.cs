using System.Collections.Generic;

namespace TownOfHost
{
    public static class TeamEgoist
    {
        public static List<byte> playerIdList = new();

        public static void Add(byte teamEgo)
        {
            playerIdList.Add(teamEgo);
        }
        public static void SoloWin(List<PlayerControl> winner)
        {
            if (Main.currentWinner == CustomWinner.Egoist && CustomRoles.Egoist.IsEnable()) //横取り勝利
            {
                winner = new();
                foreach (var id in playerIdList)
                {
                    var teamEgo = Utils.GetPlayerById(id);
                    if (teamEgo != null && ((teamEgo.Is(CustomRoles.Egoist) && !PlayerState.isDead[id]) ||
                        teamEgo.Is(CustomRoles.EgoSchrodingerCat)))
                    {
                        winner.Add(teamEgo);
                    }
                }
            }
        }
    }
}