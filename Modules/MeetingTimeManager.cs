using System;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost.Modules
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
        }
        private static void ResetMeetingTime()
        {
            DiscussionTime = DefaultDiscussionTime;
            VotingTime = DefaultVotingTime;
        }
        public static void OnReportDeadBody()
        {
            if (Options.AllAliveMeeting.GetBool() && Utils.IsAllAlive)
            {
                DiscussionTime = 0;
                VotingTime = Options.AllAliveMeetingTime.GetInt();
                Logger.Info($"DiscussionTime:{DiscussionTime}, VotingTime{VotingTime}", "MeetingTimeManager.OnReportDeadBody");
                return;
            }

            ResetMeetingTime();
            int BonusMeetingTime = 0;
            int MeetingTimeMin = 0;
            int MeetingTimeMax = 300;

            if (TimeThief.IsEnable)
            {
                MeetingTimeMin = TimeThief.LowerLimitVotingTime.GetInt();
                BonusMeetingTime += TimeThief.TotalDecreasedMeetingTime();
            }
            if (TimeManager.IsEnable)
            {
                MeetingTimeMax = TimeManager.MeetingTimeLimit.GetInt();
                BonusMeetingTime += TimeManager.TotalIncreasedMeetingTime();
            }

            int TotalMeetingTime = DiscussionTime + VotingTime;
            //時間の下限、上限で刈り込み
            BonusMeetingTime = Math.Clamp(TotalMeetingTime + BonusMeetingTime, MeetingTimeMin, MeetingTimeMax) - TotalMeetingTime;
            if (BonusMeetingTime >= 0)
                VotingTime += BonusMeetingTime; //投票時間を延長
            else
            {
                DiscussionTime += BonusMeetingTime; //会議時間を優先的に短縮
                if (DiscussionTime < 0) //会議時間だけでは賄えない場合
                {
                    VotingTime += DiscussionTime; //足りない分投票時間を短縮
                    DiscussionTime = 0;
                }
            }
            Logger.Info($"DiscussionTime:{DiscussionTime}, VotingTime{VotingTime}", "MeetingTimeManager.OnReportDeadBody");
        }
    }
}