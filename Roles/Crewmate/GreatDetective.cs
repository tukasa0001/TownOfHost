using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostForE.Roles.Core;
using UnityEngine;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class GreatDetective : RoleBase
{
    public enum TargetState
    {
        Initial = 1,
        Killed,
        Shaped,
        PlayAbilitty,
    }
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GreatDetective),
            player => new GreatDetective(player),
            CustomRoles.GreatDetective,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21600,
            SetupOptionItem,
            "名探偵",
            "#c88f57"
        );
    public GreatDetective(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TargetId = byte.MaxValue;
        ReportId = 20;
        nowTargetState = TargetState.Initial;
        gd = null;
        suiriCount = SuiriCount.GetInt();
        reportTime = ReportTime.GetInt();
        watchedDeadBody = false;
        deadTime.Clear();

        //他視点用のMarkメソッド登録
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    private static byte TargetId = byte.MaxValue;
    private static TargetState nowTargetState = TargetState.Initial;
    private static PlayerControl gd = null;

    private static OptionItem SuiriCount;
    private int suiriCount;
    private static OptionItem ReportTime;
    private int reportTime;
    private static OptionItem WatchGdPower;

    private bool watchedDeadBody = false;

    static Dictionary<byte,deadbodyTargetKillerAndTimes> deadTime = new();
    static byte ReportId = 20;

    enum OptionName
    {
        SuiriCount,
        GDReportTime,
        WatchGdPower,
    }

    private static void SetupOptionItem()
    {
        SuiriCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SuiriCount, new(0, 10, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        ReportTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.GDReportTime, new(0, 30, 1), 2, false)
            .SetValueFormat(OptionFormat.Seconds);
        WatchGdPower = BooleanOptionItem.Create(RoleInfo, 12, OptionName.WatchGdPower, false, false);
    }
    public override void Add()
    {
        setGd();
    }
    public override void OnStartMeeting()
    {
        foreach (var id in deadTime.Keys)
        {
            //小数点切り捨て
            int tempTime = (int)Time.time - (int)deadTime[id].KilledTime;
            deadTime[id].ReportTime = tempTime;
        }

        if (watchedDeadBody && deadTime.ContainsKey(ReportId))
        {
            //自身が見つけた時の処理
            setContentsMessage(deadTime);
        }

        if (TargetId != byte.MaxValue)
        {
            string text = "";

            switch (nowTargetState)
            {
                case TargetState.Killed:
                    text = "どうやら彼は過ちを犯したらしい";
                    break;
                case TargetState.Shaped:
                    text = "ここにいる彼は本当の姿なんだろうか";
                    break;
                case TargetState.PlayAbilitty:
                    text = "どうやらペットへの愛情が過剰のようだ";
                    break;
                default:
                    text = "怪しいところは見当たらなかった";
                    break;

            }

            Utils.SendMessage(text, Player.PlayerId,
                title: $"<color={RoleInfo.RoleColorCode}>名探偵の推理情報</color>");
            //対象リセット
            TargetId = byte.MaxValue;
            SendRPC();
        }
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(Color.yellow, $"({suiriCount})");

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);

        //自分自身じゃなければ抜ける
        if (voterId != Player.PlayerId) return baseVote;

        if (suiriCount > 0 && sourceVotedForId < 253)
        {
            Logger.Info("対象のID:" + sourceVotedForId, "GD");
            TargetId = sourceVotedForId;
            suiriCount--;
            SendRPC();
        }

        return baseVote;
    }

    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender(CustomRPC.GreatDetectiveSync);
        sender.Writer.Write(TargetId);
        sender.Writer.Write(suiriCount);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.GreatDetectiveSync) return;

        TargetId = reader.ReadByte();
        suiriCount = reader.ReadInt32();
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (isForMeeting) return "";
        //シーアもしくはシーンが死んでいたら処理しない。
        if (!seer.IsAlive() || !seen.IsAlive()) return "";
        //有効じゃないなら
        if (!WatchGdPower.GetBool()) return "";
        if (seer.PlayerId != seen.PlayerId) return "";
        //監視対象なら処理
        if (seer.PlayerId != TargetId) return "";

        string targetSet = "\n誰かの視線を感じる...";

        //キラー自身がseenのとき
        return Utils.ColorString(RoleInfo.RoleColor, targetSet);
    }
    public static void TargetOnShapeShifft(PlayerControl target,bool shapeshifting)
    {
        if (gd == null) return;
        if (!gd.IsAlive()) return;
        //対象でなければ抜ける
        if (target == null) return;
        if (target.PlayerId != TargetId) return;
        //変身開始のみで記録
        // (!shapeshifting) return;

        nowTargetState = TargetState.Shaped;
    }

    public static void TargetOnMurder(PlayerControl killer,PlayerControl target)
    {
        if (gd == null) return;
        if (!gd.IsAlive()) return;
        if (target != null && !target.IsAlive())
        {
            deadbodyTargetKillerAndTimes tempdata = new();
            tempdata.KillerId = killer.PlayerId;
            tempdata.KilledTime = Time.time;
            deadTime.Add(target.PlayerId, tempdata);
        }
        if (killer == null) return;
        //対象でなければ抜ける
        if (killer.PlayerId != TargetId) return;

        nowTargetState = TargetState.Killed;
    }

    public static void TargetOnPetsButton(PlayerControl target)
    {
        if (gd == null) return;
        if (!gd.IsAlive()) return;
        //対象でなければ抜ける
        if (target == null) return;
        if (target.PlayerId != TargetId) return;

        nowTargetState = TargetState.PlayAbilitty;
    }

    private static void setGd()
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            var cRole = pc.GetCustomRole();
            if (cRole == CustomRoles.GreatDetective)
            {
                gd = pc;
                break;
            }
        }
    }

    public override void AfterMeetingTasks()
    {
        nowTargetState = TargetState.Initial;
    }

    //-------以下通報系-------//


    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        //自分の通報じゃない場合
        if (reporter != Player) return true;
        //ボタンの場合
        if (target == null) return true;
        watchedDeadBody = true;
        if (target != null)
        {
            ReportId = target.PlayerId;
        }

        return true;
    }

    private void setContentsMessage(Dictionary<byte,deadbodyTargetKillerAndTimes> reportData)
    {
        //指定時間を越えていれば処理しない
        if (reportData[ReportId].ReportTime >= reportTime) return;

        string sendText = "";

        System.Random rand = new();
        int reportMessageNum = rand.Next(4);

        switch (reportMessageNum)
        {
            //死亡時刻：何秒前か
            case 0:
                sendText = "どうやら死後" + reportData[ReportId].ReportTime + "秒は経過しているようだ";
                break;
            //死因
            case 1:
                var playerState = PlayerState.GetByPlayerId(ReportId);
                sendText = "どうやら彼の死因は" + playerState.DeathReason + "らしい。惨いことをする。";
                break;
            //殺した人物の陣営
            case 2:
                var killer = Utils.GetPlayerById(reportData[ReportId].KillerId);
                var role = killer.GetCustomRole();
                if (role.IsImpostor())
                {
                    sendText = "どうやら彼はインポスターにやられたらしい";
                }
                else if (role.IsAnimals())
                {
                    sendText = "どうやら彼はアニマルズに襲われたらしい";
                }
                else if(role.IsNeutral())
                {
                    sendText = "どうやら彼は第3陣営にやられたらしい";
                }
                else
                {
                    //クルーにやられた以外は来ることないはず。
                    sendText = "どうやら彼は船員に襲われたらしい";
                }

                break;
            //死体が担っていた役職名
            case 3:
                var target = Utils.GetPlayerById(ReportId);
                var deadRoles = target.GetCustomRole();
                sendText = "どうやら彼は" + Utils.ColorString(Utils.GetRoleColor(deadRoles),Utils.GetRoleName(deadRoles)) + "の役割を担っていたようだ";
                break;
        }

        Utils.SendMessage(sendText, Player.PlayerId,
            title: $"<color={RoleInfo.RoleColorCode}>名探偵の検死情報</color>");
    }

    public class deadbodyTargetKillerAndTimes
    {
        byte killerId;
        float killedTime;
        float reportTime;

        public byte KillerId
        {
            get { return killerId; }
            set { killerId = value; }
        }
        public float KilledTime
        {
            get { return killedTime; }
            set { killedTime = value; }
        }
        public float ReportTime
        {
            get { return reportTime; }
            set { reportTime = value; }
        }
    }
}
