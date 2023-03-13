using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Services.Authentication.Internal;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Neutral
{
    public static class Collectors
    {
        private static readonly int Id = 51000;
        public static OptionItem CollectorsCollectAmount;
        public static Dictionary<byte, byte> CollectorsVoteFor = new();
        public static Dictionary<byte, int> CollectVote = new();
        public static Dictionary<byte, int> NewVote = new();
        public static Dictionary
        public static void SetupCustomOption() 
        {
            Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Collectors);
            CollectorsCollectAmount = IntegerOptionItem.Create(Id + 13, "CollectorsCollectAmount", new(1, 225, 1), 30, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Collectors])
                .SetValueFormat(OptionFormat.Votes);
        }
        public static string GetProgressText(byte playerId)
        {
            int VoteAmount = CollectVote[playerId];
            AllVote = VoteAmount + AllVote;
            int CollectNum = CollectorsCollectAmount.GetInt();
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Revolutionist).ShadeColor(0.25f), $"({AllVote}/{CollectNum})");
        }
        public static bool CollectDone()
        {
            var pc = Main.AllAlivePlayerControls;
            foreach (var player in pc)
            {
                if (player.Is(CustomRoles.Collectors))
                {
                    var pcid = player.PlayerId;
                    int VoteAmount = CollectVote[pcid];
                    AllVote = VoteAmount + AllVote;
                    int CollectNum = CollectorsCollectAmount.GetInt();
                    if (AllVote == CollectNum) return true;
                }
            }
            return false;
        }
        public static void CollectorsVotes(PlayerControl target , PlayerVoteArea ps)//集票者投票给谁
        {
            if (CheckForEndVotingPatch.CheckRole(ps.TargetPlayerId, CustomRoles.Collectors)) 
            {
                if (!CollectorsVoteFor.ContainsKey(target.PlayerId))
                    CollectorsVoteFor.Add(target.PlayerId, ps.TargetPlayerId);
            }     
        }
        public static void CollectAmount(Dictionary<byte, int> VotingData, MeetingHud __instance)//得到集票者收集到的票
        {
            CollectVote = new();
            int VoteAmount = 0;
            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                PlayerControl pc = Utils.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;
                foreach (var data in VotingData)
                    if (CollectorsVoteFor.ContainsKey(data.Key) && pc.PlayerId == CollectorsVoteFor[data.Key] && pc.Is(CustomRoles.Collectors))
                    {
                        VoteAmount = data.Value;
                        CollectVote.Add(pc.PlayerId, VoteAmount);
                    }
            }
        }
    }
}
