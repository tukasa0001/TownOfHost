using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using System;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;

using System.Linq;
using MS.Internal.Xml.XPath;

namespace TownOfHostForE.Roles.Impostor
{
   public sealed class Talktive : RoleBase, IImpostor
    {
        public enum TalkContents
        {
            Free,
            Talktive1,
            Talktive2,
            Talktive3,
            Talktive4,
            Talktive5,
            Talktive6,
            Talktive7,
            Talktive8,
            Talktive9,
            Talktive10,
            Talktive11,
            Talktive12,
            Talktive13,
            Talktive14,
            Talktive15,
            Talktive16,
            Talktive17,
            Talktive18,
            Talktive19,
            Talktive20,
            Talktive21,
            Talktive22,
            Talktive23,
            Talktive24,
            Talktive25,
            Talktive26,
            Talktive27,
            Talktive28,
            Talktive29,
            Talktive30,
        }
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Talktive),
            player => new Talktive(player),
            CustomRoles.Talktive,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            22400,
            SetupOptionItem,
            "トークティブ"
        );
        public Talktive(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            talktiveKillCooldown = TalktiveKillCooldown.GetFloat();
            killCooldown = KillCooldown.GetFloat();
            setFiverFlag = false;
        }


        static OptionItem KillCooldown;
        float killCooldown;
        static OptionItem TalktiveKillCooldown;
        float talktiveKillCooldown;

        private readonly static string clearWords = "上手く喋れたようだね、次のターンのキルクールを下げてあげよう";

        TalkContents nowTalkContents = TalkContents.Free;

        bool setFiverFlag = false;
        bool setModPlayerResetKillCool = false;

        enum OptionName
        {
            TalktiveKillCool
        }

        private static void SetupOptionItem()
        {
            KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 35f, false)
                .SetValueFormat(OptionFormat.Seconds);
            TalktiveKillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.TalktiveKillCool, new(2.5f, 180f, 2.5f), 25f, false)
                .SetValueFormat(OptionFormat.Seconds);
        }
        public override void Add()
        {
            nowTalkContents = TalkContents.Free;
        }

        //開始時はデフォルトのキルクール
        public float CalculateKillCooldown() => setFiverFlag ? talktiveKillCooldown : killCooldown;

        public override void OnReceiveChat(PlayerControl player, string text)
        {
            string[] args = text.Split(' ');

            var cRole = player.GetCustomRole();

            if (cRole != CustomRoles.Talktive) return;
            if (GameStates.IsLobby || !player.IsAlive()) return;
            //対象の言葉を言っているか確認
            if (!SearchWords(nowTalkContents, args[0])) return;
            //正しい言葉を言ったのでキルクールを変更
            setFiverFlag = true;
            Utils.SendMessage(clearWords, Player.PlayerId, "トークティブ");
        }

        public override void OnStartMeeting()
        {
            //ミーティング開始時にキルクールをデフォルトにリセット
            if (setFiverFlag && PlayerControl.LocalPlayer == Player) setModPlayerResetKillCool = true;
            setFiverFlag = false;
            RandomSetContents();
        }

        public override void AfterMeetingTasks()
        {
            //mod導入者がキーワードを言わなかった場合、キルクが直らないバグ対応
            if (setModPlayerResetKillCool && PlayerControl.LocalPlayer == Player)
            {
                setModPlayerResetKillCool = false;
                PlayerControl.LocalPlayer.SetKillCooldown();
            }
        }

        private void RandomSetContents()
        {
            if (!Player.IsAlive()) return;
            System.Random rand = new();
            int contentsInt = rand.Next(1,30);
            nowTalkContents = (TalkContents)Enum.ToObject(typeof(TalkContents), contentsInt);

            string sendWords = "「" + setTalkContents(contentsInt) + "」" + "\n↑↑↑↑↑\n上手く言葉に紛れ込ませるのだ";

            Utils.SendMessage(sendWords, Player.PlayerId, "<color=#ff1919>【今回のお題】</color>");

            foreach (var pc in Main.AllDeadPlayerControls)
            {
                Utils.SendMessage(sendWords, pc.PlayerId, "<color=#ff1919>【今回のトークティブのお題】</color>");
            }
        }

        private string setTalkContents(int nowId)
        {
            string tempString = "Talktive" + nowId.ToString();

            return GetString(tempString);
        }

        //public override void OnFixedUpdate(PlayerControl player)
        //{
        //    //mod導入者がキーワードを言わなかった場合、キルクが直らないバグ対応
        //    if (GameStates.IsInTask && setModPlayerResetKillCool && player == Player)
        //    {
        //        setModPlayerResetKillCool = false;
        //        player.SetKillCooldown();
        //    }
        //}

        private bool SearchWords(TalkContents nowContents, string words)
        {
            if (words.Contains("上手く言葉に紛れ込ませるのだ")) return false;
            if (words.Contains(clearWords)) return false;
            Logger.Info("コンテンツ：" + nowContents + "/words:" + words,"talktive");
            return (nowContents == TalkContents.Talktive1 && words.Contains(GetString("Talktive1")) ||
                    nowContents == TalkContents.Talktive2 && words.Contains(GetString("Talktive2")) ||
                    nowContents == TalkContents.Talktive3 && words.Contains(GetString("Talktive3")) ||
                    nowContents == TalkContents.Talktive4 && words.Contains(GetString("Talktive4")) ||
                    nowContents == TalkContents.Talktive5 && words.Contains(GetString("Talktive5")) ||
                    nowContents == TalkContents.Talktive6 && words.Contains(GetString("Talktive6")) ||
                    nowContents == TalkContents.Talktive7 && words.Contains(GetString("Talktive7")) ||
                    nowContents == TalkContents.Talktive8 && words.Contains(GetString("Talktive8")) ||
                    nowContents == TalkContents.Talktive9 && words.Contains(GetString("Talktive9")) ||
                    nowContents == TalkContents.Talktive10 && words.Contains(GetString("Talktive10")) ||
                    nowContents == TalkContents.Talktive11 && words.Contains(GetString("Talktive11")) ||
                    nowContents == TalkContents.Talktive12 && words.Contains(GetString("Talktive12")) ||
                    nowContents == TalkContents.Talktive13 && words.Contains(GetString("Talktive13")) ||
                    nowContents == TalkContents.Talktive14 && words.Contains(GetString("Talktive14")) ||
                    nowContents == TalkContents.Talktive15 && words.Contains(GetString("Talktive15")) ||
                    nowContents == TalkContents.Talktive16 && words.Contains(GetString("Talktive16")) ||
                    nowContents == TalkContents.Talktive17 && words.Contains(GetString("Talktive17")) ||
                    nowContents == TalkContents.Talktive18 && words.Contains(GetString("Talktive18")) ||
                    nowContents == TalkContents.Talktive19 && words.Contains(GetString("Talktive19")) ||
                    nowContents == TalkContents.Talktive20 && words.Contains(GetString("Talktive20")) ||
                    nowContents == TalkContents.Talktive21 && words.Contains(GetString("Talktive21")) ||
                    nowContents == TalkContents.Talktive22 && words.Contains(GetString("Talktive22")) ||
                    nowContents == TalkContents.Talktive23 && words.Contains(GetString("Talktive23")) ||
                    nowContents == TalkContents.Talktive24 && words.Contains(GetString("Talktive24")) ||
                    nowContents == TalkContents.Talktive25 && words.Contains(GetString("Talktive25")) ||
                    nowContents == TalkContents.Talktive26 && words.Contains(GetString("Talktive26")) ||
                    nowContents == TalkContents.Talktive27 && words.Contains(GetString("Talktive27")) ||
                    nowContents == TalkContents.Talktive28 && words.Contains(GetString("Talktive28")) ||
                    nowContents == TalkContents.Talktive29 && words.Contains(GetString("Talktive29")) ||
                    nowContents == TalkContents.Talktive30 && words.Contains(GetString("Talktive30")));
        }
    }
}
