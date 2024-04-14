using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Neutral;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;
using static UnityEngine.GraphicsBuffer;
using TownOfHostForE.Modules;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Balancer : RoleBase
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Balancer),
            player => new Balancer(player),
            CustomRoles.Balancer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21900,
            SetupOptionItem,
            "真実の天秤",
            "#f8cd46",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Balancer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        chanceCount = OptionChanceCount.GetInt();
        killaway = OptionKillAway.GetInt();
        hidePlayer = OptionhidePlayer.GetBool();
        balancerPri = OptionbalancerPri.GetBool();
    }

    private bool ready = false;
    //発動上限
    private int chanceCount = 1;
    //キル数の倍数
    private int killaway = 1;
    //発言者を隠す
    private bool hidePlayer;
    //同数の場合天秤を優先する。
    private bool balancerPri;

    static OptionItem OptionChanceCount;
    static OptionItem OptionKillAway;
    static OptionItem OptionhidePlayer;
    static OptionItem OptionbalancerPri;


    enum OptionName
    {
        BalancerChanceCount,
        BalancerKillAway,
        BalancerHidePlayer,
        BalancerCountEqual
    }
    private static void SetupOptionItem()
    {
        OptionChanceCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.BalancerChanceCount, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionKillAway = IntegerOptionItem.Create(RoleInfo, 11, OptionName.BalancerKillAway, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionhidePlayer = BooleanOptionItem.Create(RoleInfo, 12, OptionName.BalancerHidePlayer, false, false);
        OptionbalancerPri = BooleanOptionItem.Create(RoleInfo, 13, OptionName.BalancerCountEqual, false, false);
    }

    public override bool CheckVoteAsVoter(PlayerControl votedFor)
    {
        //投票先が自分以外か
        if (votedFor.PlayerId != Player.PlayerId) return true;

        //自投票
        if(ready == false && chanceCount > 0)
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
        if(!ready) return base.ModifyVote(voterId, sourceVotedForId, isIntentional);

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

        string sendMessage = Utils.ColorString(killerInfo.Color,$"{targetInfo.PlayerName}、自害しろ");
        Utils.SendMessage(sendMessage,title: killerInfo.PlayerName);

        //判定
        if (BalancerManagement.CheckBalancerSkills(voterId, sourceVotedForId, (byte)killaway, balancerPri))
        {
            new LateTask(() =>
            {
                sendMessage = Utils.ColorString(targetInfo.Color, $"ありえない...!! この私が...");
                Utils.SendMessage(sendMessage, title: targetInfo.PlayerName);

                killer.MurderPlayer(target);
                //その死体は恐らく会議後残るが、面白いので通報できないようにしておく
                ReportDeadBodyPatch.CanReportByDeadBody[target.PlayerId] = false;

            }, 1f, "Balancer Kill");
        }
        else
        {
            new LateTask(() =>
            {
                sendMessage = Utils.ColorString(killerInfo.Color, $"＜(´⌯ω⌯`)＞");
                Utils.SendMessage(sendMessage, title: killerInfo.PlayerName);

                killer.MurderPlayer(killer);
                //その死体は恐らく会議後残るが、面白いので通報できないようにしておく
                ReportDeadBodyPatch.CanReportByDeadBody[killer.PlayerId] = false;

            }, 1f, "Balancer Kill");
        }

        chanceCount--;

        return (votedForId, numVotes, doVote);
    }
}