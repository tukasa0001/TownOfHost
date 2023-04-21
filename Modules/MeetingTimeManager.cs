using System;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
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

            foreach (var role in CustomRoleManager.AllActiveRoles.Values)
            {
                if (role is IMeetingTimeAlterable meetingTimeAlterable)
                {
                    if (role is TimeThief)
                    {
                        // Hyz-sui: 会議時間をいじる役職が増えたら上限&下限設定の置き場所要検討
                        MeetingTimeMin = TimeThief.LowerLimitVotingTime;
                    }

                    if (role is TimeManager)
                    {
                        MeetingTimeMax = TimeManager.MeetingTimeLimit;
                    }

                    if (!role.Player.IsAlive() && meetingTimeAlterable.RevertOnDie)
                    {
                        continue;
                    }

                    var time = meetingTimeAlterable.CalculateMeetingTimeDelta();
                    Logger.Info($"会議時間-{role.Player.GetNameWithRole()}: {time} s", "MeetingTimeManager.OnReportDeadBody");
                    BonusMeetingTime += time;
                }
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