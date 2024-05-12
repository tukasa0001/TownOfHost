using System;
using Sentry.Internal.Extensions;
using TownOfHostForE.Modules;
using TownOfHostForE.GameMode;

namespace TownOfHostForE.Roles.Core.Class
{
    public class BalancerManager : RoleBase
    {
        public BalancerManager(
        SimpleRoleInfo roleInfo,
        PlayerControl player,
        Func<HasTask> hasTasks = null,
        bool? hasAbility = null
        )
        : base(
            roleInfo,
            player,
            hasTasks,
            hasAbility)
        {
            chanceCount = 1;
            killaway = 1;
            hidePlayer = false;
            balancerPri = false;
        }

        protected bool ready = false;
        //発動上限
        protected int chanceCount = 1;
        //キル数の倍数
        protected int killaway = 1;
        //発言者を隠す
        protected static bool hidePlayer;
        //同数の場合天秤を優先する。
        protected bool balancerPri;


        public override bool CheckVoteAsVoter(PlayerControl votedFor)
        {
            //スキップの場合
            if (votedFor.IsNull()) return true;

            //投票先が自分以外か
            if (votedFor.PlayerId != Player.PlayerId) return true;

            //自投票
            if (ready == false && chanceCount > 0)
            {
                ready = true;
                return false;
            }

            return true;
        }

        public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
        {

            var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
            //投票者が自分か
            if (voterId != Player.PlayerId) return base.ModifyVote(voterId, sourceVotedForId, isIntentional);

            //準備出来てないならそのまま投票
            if (!ready) return base.ModifyVote(voterId, sourceVotedForId, isIntentional);

            //この地点で止めておく
            ready = false;

            //準備出来てるのに自投票してるなら通常投票
            if (sourceVotedForId == Player.PlayerId) return base.ModifyVote(voterId, sourceVotedForId, isIntentional);

            //天秤効果

            //キラーinfo取得
            var killerInfo = Utils.GetPlayerInfoById(voterId);
            //相手info取得
            var targetInfo = Utils.GetPlayerInfoById(sourceVotedForId);
            //キラー取得
            var killer = Utils.GetPlayerById(voterId);
            //相手取得
            var target = Utils.GetPlayerById(sourceVotedForId);

            //投票先を無効に。
            numVotes = MeetingVoteManager.NoVote;
            if (hidePlayer)
            {
                Utils.SendMessage($"{targetInfo.PlayerName}、自害しろ",title:"天秤");
            }
            else
            {
                SendBalancerMessage($"{targetInfo.PlayerName}、自害しろ", killer, killerInfo.PlayerName);
            }

            //判定
            if (CheckBalancerSkills(voterId, sourceVotedForId, (byte)killaway, balancerPri))
            {
                new LateTask(() =>
                {
                    SendBalancerMessage("ありえない...!! この私が...", target,targetInfo.PlayerName);

                    new LateTask(() =>
                    {
                        killer.MurderPlayer(target);
                        //その死体は恐らく会議後残るが、面白いので通報できないようにしておく
                        ReportDeadBodyPatch.CanReportByDeadBody[target.PlayerId] = false;

                    }, 0.5f, "Balancer Kill");
                }, 1f, "Balancer Kill");
            }
            else
            {
                new LateTask(() =>
                {
                    if (hidePlayer)
                    {
                        Utils.SendMessage("＜(´⌯ω⌯`)＞", title: "天秤");
                    }
                    else
                    {
                        SendBalancerMessage($"＜(´⌯ω⌯`)＞", killer, killerInfo.PlayerName);
                    }

                    new LateTask(() =>
                    {
                        killer.MurderPlayer(killer);
                        //その死体は恐らく会議後残るが、面白いので通報できないようにしておく
                        ReportDeadBodyPatch.CanReportByDeadBody[killer.PlayerId] = false;
                    }, 0.5f, "Balancer Kill");

                }, 1f, "Balancer Kill");
            }

            chanceCount--;

            return (votedForId, numVotes, doVote);
        }

        private static void SendBalancerMessage(string text, PlayerControl player, string name)
        {
            if (Options.GetWordLimitMode() != WordLimit.regulation.None) WordLimit.nowSafeWords.Add(text);

            foreach (var sendTo in Main.AllPlayerControls)
            {
                Main.SuffixMessagesToSend.Add((text, sendTo.PlayerId, name, player));
            }
        }

        private static bool CheckBalancerSkills(byte balncerId, byte targetId, byte killBonus, bool equal)
        {
            var balancer = Utils.GetPlayerById(balncerId);
            var target = Utils.GetPlayerById(targetId);

            int balancerCount = GetCount(balancer, killBonus);
            int targetCount = GetCount(target, killBonus);


            return equal ? balancerCount >= targetCount : balancerCount > targetCount;
        }

        private static int GetCount(PlayerControl pc, byte killBonus)
        {
            try
            {
                //戻り値初期
                int returnByte = 0;

                var cRole = pc.GetCustomRole();
                //何れかのキル役職
                if (pc.IsNeutralKiller() ||
                    pc.IsAnimalsKiller() ||
                    cRole.GetCustomRoleTypes() == Core.CustomRoleTypes.Impostor ||
                    pc.IsCrewKiller())
                {
                    Logger.Info($"キルカウント", "debug");
                    returnByte = Main.killCount.ContainsKey(pc.PlayerId) ? Main.killCount[pc.PlayerId] * (int)killBonus : 0;
                }
                //キラーじゃないならタスク数参照
                else
                {
                    Logger.Info($"タスクカウント", "debug");
                    returnByte = pc.GetPlayerTaskState().CompletedTasksCount;
                }

                return returnByte;
            }
            catch (Exception ex)
            {
                Logger.Info($"{ex.Message}:{ex.StackTrace}", "debug");
                return 0;
            }
        }
    }
}
