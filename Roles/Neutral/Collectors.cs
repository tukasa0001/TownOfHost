using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Services.Authentication.Internal;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Neutral
{
    internal static class Collectors
    {
        private static readonly int Id = 05053175;
        public static OptionItem CollectorsCollectAmount;
        public static List<byte> CollectorsVoteFor = new();


        public static void SetupCustomOption() 
        {Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Collectors);
            CollectorsCollectAmount = IntegerOptionItem.Create(Id + 13, "CollectorsCollectAmount", new(5, 225, 1), 30, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Collectors])
                .SetValueFormat(OptionFormat.Votes);
        }
        public static string GetProgressText(byte playerId)
        {
            int VoteAmount = 0;
            int CollectNum = CollectorsCollectAmount.GetInt();
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Revolutionist), $"({VoteAmount}/{CollectNum})");
        }
        public static void CollectorsVotes(PlayerControl target , PlayerVoteArea ps)//集票者投票给谁
        {
            if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Collectors))
                if (!CollectorsVoteFor.Contains(target.PlayerId))
                    CollectorsVoteFor.Add(target.PlayerId);
        }
        public static int CollectAmount(Dictionary<byte, int> VotingData,byte playerId)//得到集票者收集到的票
        {
            int VoteAmount = 0;
            foreach (var data in VotingData)
            {
                if (CollectorsVoteFor.Contains(data.Key)) 
                {
                    VoteAmount = data.Value;
                }
            }
            return VoteAmount;
        }
        public static (int, int) GetDrawPlayerCount(byte playerId)
        {
            int draw = 0;
            int CollectNum = CollectorsCollectAmount.GetInt();
            foreach (var pc in Main.AllPlayerControls)
            {
                if (Main.isDraw.TryGetValue((playerId, pc.PlayerId), out var isDraw) && isDraw)
                {
                    
                    draw++;
                }
            }
            return (draw, CollectNum);
        }
    }
}
