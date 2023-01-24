using System;
using System.Linq;
using AmongUs.GameOptions;

namespace TownOfHost
{
    public class MeetingTimeManager
    {
        private static int DiscussionTime;
        private static int VotingTime;
        private static int DefaultDiscussionTime;
        private static int DefaultVotingTime;

        public static void Init()
        {
            DefaultDiscussionTime = Main.RealOptionsData.GetInt(Int32OptionNames.DiscussionTime);
            DefaultVotingTime = Main.RealOptionsData.GetInt(Int32OptionNames.VotingTime);
            Logger.Info($"DefaultDiscussionTime:{DefaultDiscussionTime}, DefaultVotingTime{DefaultVotingTime}", "MeetingTimeManager.Init");
            ResetMeetingTime();
        }
        public static void ApplyGameOptions(IGameOptions opt)
        {
            opt.SetInt(Int32OptionNames.DiscussionTime, DiscussionTime);
            opt.SetInt(Int32OptionNames.VotingTime, VotingTime);
            Logger.Info($"DiscussionTime:{DiscussionTime}, VotingTime{VotingTime}", "MeetingTimeManager.ApplyGameOptions");
        }
        private static void ResetMeetingTime()
        {
            DiscussionTime = DefaultDiscussionTime;
            VotingTime = DefaultVotingTime;
        }
        public static void OnReportDeadBody()
        {
            ResetMeetingTime();
            int BonusMeetingTime = 0;
            int MeetingTimeMin = 0;
            int MeetingTimeMax = 300;

            int TotalMeetingTime = DiscussionTime + VotingTime;
            BonusMeetingTime = Math.Clamp(TotalMeetingTime + BonusMeetingTime, MeetingTimeMin, MeetingTimeMax) - TotalMeetingTime;
            if (DiscussionTime > 0)
            {
                DiscussionTime += BonusMeetingTime;
                if (DiscussionTime < 0)
                {
                    VotingTime += DiscussionTime;
                    DiscussionTime = 0;
                }
            }
            else
            {
                VotingTime += BonusMeetingTime;
            }
            Logger.Info($"DiscussionTime:{DiscussionTime}, VotingTime{VotingTime}", "MeetingTimeManager.OnReportDeadBody");
        }
    }
}