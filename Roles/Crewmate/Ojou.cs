using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Impostor;
using TownOfHostForE.GameMode;

namespace TownOfHostForE.Roles.Crewmate;
public class Ojou : RoleBase
{
    /// <summary>
    ///  20000:TOH4E役職
    ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
    ///    100:役職ID
    /// </summary>
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Ojou),
            pc => new Ojou(pc),
            CustomRoles.OjouSama,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21100,
            SetupOptionItem,
            "お嬢様",
            "#ff6be4"
        );
    public Ojou(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        optionWatchWearWolf = OptionWatchWearWolf.GetBool();
        optionOjousamaTurncoatProbability = OptionOjousamaTurncoatProbability.GetInt();
        ojouRadius = OjouRadius.GetFloat();
        alartLimit = AlartLimit.GetInt();
        optionRemainingTasks = OptionRemainingTasks.GetInt();


        //他視点用のMarkメソッド登録
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public override void OnDestroy()
    {
        TargetList.Clear();
        TargetColorlist.Clear();
        ExposedList.Clear();
    }

    private static OptionItem OptionOjousamaTurncoatProbability;
    private static OptionItem OjouRadius;
    private static OptionItem AlartLimit;
    private static OptionItem OptionRemainingTasks;
    private static OptionItem OptionWatchWearWolf;

    enum OptionName
    {
        OjouOptionWatchWearWolf,
        OjouSamaChangeChances,
        OjouRadius,
        OjouSamaAlartLimit,
        OjouRemainingTaskFound
    }

    private static bool optionWatchWearWolf;
    private static int optionOjousamaTurncoatProbability;
    private static float ojouRadius;
    private static int alartLimit;
    private static int optionRemainingTasks;

    private static bool AlartFlag = true;
    private static byte AlartTarget = 0;

    private bool IsExposed = false;
    private bool IsComplete = false;

    //複数Snitchで共有するためstatic
    private static HashSet<byte> TargetList = new();
    private static Dictionary<byte, Color> TargetColorlist = new();
    private static HashSet<byte> ExposedList = new();

    private static void SetupOptionItem()
    {
        OptionOjousamaTurncoatProbability = IntegerOptionItem.Create(RoleInfo, 11, OptionName.OjouSamaChangeChances, new(0, 100, 5), 10, false);
        OptionWatchWearWolf = BooleanOptionItem.Create(RoleInfo, 10, OptionName.OjouOptionWatchWearWolf, false, false);
        OjouRadius = FloatOptionItem.Create(RoleInfo.ConfigId + 12, OptionName.OjouRadius, new(0.5f, 3f, 0.5f), 1f,TabGroup.CrewmateRoles, false)
            .SetValueFormat(OptionFormat.Multiplier)
            .SetParent(OptionWatchWearWolf);
        AlartLimit = IntegerOptionItem.Create(RoleInfo.ConfigId + 13, OptionName.OjouSamaAlartLimit, new(0, 5, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(OptionWatchWearWolf); ;
        OptionRemainingTasks = IntegerOptionItem.Create(RoleInfo.ConfigId + 14, OptionName.OjouRemainingTaskFound, new(0, 10, 1), 1, TabGroup.CrewmateRoles, false)
            .SetParent(OptionWatchWearWolf);
    }

    /// <summary>
    /// スニッチのターゲットであるかの判定
    /// Others系でも使うためstatic実装
    /// </summary>
    /// <param name="target">判定対象</param>
    /// <returns></returns>
    private static bool IsSnitchTarget(PlayerControl target)
    {
        return target.Is(CustomRoleTypes.Impostor)
            || target.IsNeutralKiller()
            || target.IsAnimalsKiller();
    }


    /// <summary>
    /// キラーから見たスニッチ警告マーク
    /// キラーにはタスクが進んだスニッチを発見した警告マーク
    /// スニッチにはキラーに発見された警告マーク
    /// キラーが対象なためstatic実装
    /// </summary>
    /// <param name="seer">キラーの場合有効</param>
    /// <param name="seen">キラー自身またはスニッチの場合有効</param>
    /// <returns></returns>
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        //人外通知能力が有効じゃない場合
        if (!optionWatchWearWolf) return"";
        //キラーじゃなければ無し
        if (!IsSnitchTarget(seer)) return "";
        //タスクが進んでいなければ無し
        if (ExposedList.Count == 0) return "";

        if (seer.PlayerId == seen.PlayerId)
        {
            //キラー自身がseenのとき
            var mark = "★";
            if (!isForMeeting)
            {
                mark += TargetArrow.GetArrows(seer, ExposedList.ToArray());
            }
            return Utils.ColorString(RoleInfo.RoleColor, mark);
        }
        else if (seen.GetRoleClass() is Ojou ojou && ojou.IsExposed)
        {
            //seenがタスク終わりそうなスニッチの時
            return Utils.ColorString(RoleInfo.RoleColor, "★");

        }
        //その他seenなら無し
        return "";
    }
    /// <summary>
    /// タスクの進行状況の管理
    /// </summary>
    public override bool OnCompleteTask()
    {
        //人外通知能力が有効じゃない場合
        if (!optionWatchWearWolf) return true;
        var update = false;
        if (TargetList.Count == 0)
        {
            //TargetListが未作成ならここで作る
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (!IsSnitchTarget(target)) continue;

                var targetId = target.PlayerId;
                TargetList.Add(targetId);
                TargetColorlist.Add(targetId, target.GetRoleColor());
            }
        }
        if (!IsExposed && MyTaskState.RemainingTasksCount <= optionRemainingTasks)
        {
            IsExposed = true;
            ExposedList.Add(Player.PlayerId);
            foreach (var targetId in TargetList)
            {
                TargetArrow.Add(targetId, Player.PlayerId);
            }
            update = true;
        }

        if (!IsComplete && IsTaskFinished)
        {
            IsComplete = true;
        }
        if (update) Utils.NotifyRoles();
        return true;
    }


    public static void OjouOnReceiveChat(PlayerControl player, string text)
    //public override void OnReceiveChat(PlayerControl player, string text)
    {
        //最初の会議は対象外
        if (MeetingStates.FirstMeeting) return;

        string[] args = text.Split(' ');

        var cRole = player.GetCustomRole();

        if ((cRole == CustomRoles.OjouSama ||
             cRole == CustomRoles.MOjouSama ||
             cRole == CustomRoles.JOjouSama ||
             cRole == CustomRoles.OOjouSama ||
             cRole == CustomRoles.DOjouSama ||
             cRole == CustomRoles.AOjouSama ||
             cRole == CustomRoles.EOjouSama
            ) && !GameStates.IsLobby)
        {
            string words = SettingWords(player, args[0]);
            string name = "お嬢様";
            if (words == "")
            {
               name = RareWordName();
            }
            if (words != null)
            {
                OjouSendMessage(words, player, name);
            }
        }
    }

    private static string RareWordName()
    {
        //レアワード選出選手権
        string rareWords = "レアなお嬢様";

        System.Random rand = new();
        int rareWordsInt = rand.Next(2);

        switch (rareWordsInt)
        {
            case 0:
                rareWords = "<size=200%>ですわ</size>";
                break;
            case 1:
                rareWords = "<size=250%>(クソでかため息)</size>";
                break;
            default:
                break;
        }

        return rareWords;
    }

    private static string SettingWords(PlayerControl Player, string SendedWords)
    {
        if (Player.name == "お嬢様") return null;

        if (SendedWords.Length > 0 && SendedWords[0] == '/') return null;

        switch (SendedWords)
        {
            case "":
                return null;
            default:
                System.Random rand = new();

                int OjouSamaWords = rand.Next(81);
                string words = "ﾜｧ......";
                if (OjouSamaWords >= 80)
                {
                    words = "";
                }
                else if(OjouSamaWords > 70)
                {
                    words = "...ってｺﾄｯ!?";
                }
                else if(OjouSamaWords > 60)
                {
                    words = "ですわ！";
                }
                else if(OjouSamaWords > 50)
                {
                    words = "ですわ～";
                }
                else if(OjouSamaWords > 40)
                {
                    words = "ですのよ！！";
                }
                else if(OjouSamaWords > 30)
                {
                    words = "ですの！";
                }
                else if(OjouSamaWords > 20)
                {
                    words = "ですこと！？";
                }
                else if(OjouSamaWords > 10)
                {
                    words = "ですの？";
                }
                else
                {
                    words = "ですわ...";
                }

                return words;
        }
    }

    private static void OjouSendMessage(string text, PlayerControl player, string name)
    {
        if (Options.GetWordLimitMode() != WordLimit.regulation.None) WordLimit.nowSafeWords.Add(text);
        foreach (var sendTo in Main.AllPlayerControls)
        {
            if (player == PlayerControl.LocalPlayer && AmongUsClient.Instance.AmHost)
            {
                new LateTask(() =>
                {
                    Main.SuffixMessagesToSend.Add((text.RemoveHtmlTags(), sendTo.PlayerId, name, player));
                }, 0.25f, "OjouHostSend");
            }
            else
            {
                Main.SuffixMessagesToSend.Add((text.RemoveHtmlTags(), sendTo.PlayerId, name, player));
            }
        }
    }

    public override void OnFixedUpdate(PlayerControl pc)
    {
        if (GameStates.IsLobby) return;

        //人外通知能力が有効じゃない場合
        if (!optionWatchWearWolf) return;
        try
        {
            if (TargetList.Count == 0) return;

            var seerId = pc.PlayerId;
            if (!IsComplete) return;

            var seerIsDead = !pc.IsAlive();
            var pos = pc.transform.position;
            foreach (var targetId in TargetList)
            {
                var target = Utils.GetPlayerById(targetId);
                if (seerIsDead || !target.IsAlive())
                {
                    TargetList.Remove(targetId);
                    AlartTarget = 0;
                    AlartFlag = true;
                    continue;
                }

                if (alartLimit > 0)
                {
                    var dis = Vector2.Distance(pos, target.transform.position);
                    Logger.Info("残通知回数:" + alartLimit + " => ", "Ojou:343");

                    if (dis <= ojouRadius && AlartFlag)
                    {
                        Utils.KillFlash(pc);
                        alartLimit--;
                        AlartTarget = targetId;
                        AlartFlag = false;
                    }
                    //アラート対象が離れた場合
                    else if (AlartTarget == targetId && dis > ojouRadius)
                    {
                        AlartTarget = 0;
                        AlartFlag = true;

                    }
                }
            }

        }
        catch
        {

        }
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (Is(info.AttemptTarget))
        {
            (var killer, var target) = info.AttemptTuple;

            //自殺ならスルー
            if (info.IsSuicide) return true;
            //シェリフのキルなら処理をシェリフに委ねる。
            var cRole = killer.GetCustomRole();
            if (cRole == CustomRoles.Sheriff ||
                cRole == CustomRoles.SillySheriff ||
                cRole == CustomRoles.GrudgeSheriff ||
                cRole == CustomRoles.DogSheriff) return true;
            //既に変化していたらスルー
            if (!target.Is(CustomRoles.OjouSama)) return true;

            //確率算段
            System.Random rand = new System.Random();
            int OjouSamaKills = rand.Next(100);
            //はじき出された値が設定値を超えていた場合はスルー。
            if (OjouSamaKills > optionOjousamaTurncoatProbability) return true;


            //お嬢様が切られた場合の役職変化スタート
            killer.RpcProtectedMurderPlayer(target);
            info.CanKill = false;
            switch (killer.GetCustomRole())
            {
                case CustomRoles.BountyHunter:
                    var bountyHunter = (BountyHunter)killer.GetRoleClass();
                    if (bountyHunter.GetTarget() == target)
                        bountyHunter.ResetTarget();//ターゲットの選びなおし
                    break;
                case CustomRoles.SerialKiller:
                    var serialKiller = (SerialKiller)killer.GetRoleClass();
                    serialKiller.SuicideTimer = null;
                    break;
                case CustomRoles.Egoist:
                    target.RpcSetCustomRole(CustomRoles.EOjouSama);
                    break;
                case CustomRoles.Jackal:
                    target.RpcSetCustomRole(CustomRoles.JOjouSama);
                    break;
                case CustomRoles.DarkHide:
                    target.RpcSetCustomRole(CustomRoles.DOjouSama);
                    break;
                case CustomRoles.Opportunist:
                    target.RpcSetCustomRole(CustomRoles.OOjouSama);
                    break;
                case CustomRoles.Gizoku:
                    target.RpcSetCustomRole(CustomRoles.GOjouSama);
                    break;
            }
            if (killer.IsAnimalsKiller())
                target.RpcSetCustomRole(CustomRoles.AOjouSama);
            if (killer.Is(CustomRoleTypes.Impostor))
                target.RpcSetCustomRole(CustomRoles.MOjouSama);

            Utils.NotifyRoles();
            Utils.MarkEveryoneDirtySettings();
            return false;

        }
        return true;
    }
}