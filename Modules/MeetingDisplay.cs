//using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Epic.OnlineServices.UI;
using TownOfHostForE;
using TownOfHostForE.Roles.Core;
using System.Data.Common;
using UnityEngine.UIElements;

namespace TownOfHostForE.Modules
{
    //参考:TOHY：MeetingDisplayText
    internal class MeetingDisplay
    {
        public static string reportInfo = "";

        public static void SetDisplayForClient(int targetNum,PlayerVoteArea pva)
        {

            if (targetNum == 0)
            {
                if (!Options.ShowReportReason.GetBool()) return;
                var plusVoteArea = Object.Instantiate(pva.NameText);
                plusVoteArea.transform.DestroyChildren();
                plusVoteArea.transform.SetParent(pva.NameText.transform);
                (string text, bool kaigyou) = ButtonInfo();
                plusVoteArea.text = text;
                plusVoteArea.transform.localPosition = pva.NameText.transform.localPosition;
                float setY = kaigyou ? 0.5f : 0.6f;
                plusVoteArea.transform.localPosition += new Vector3(-0.3f, setY);

            }
            else if (targetNum == 1)
            {
                var plusVoteArea = Object.Instantiate(pva.NameText);
                plusVoteArea.transform.DestroyChildren();
                plusVoteArea.transform.SetParent(pva.NameText.transform);
                (string text, bool kaigyou) = SetMyRoleInfo(PlayerControl.LocalPlayer);
                plusVoteArea.text = text;
                plusVoteArea.transform.localPosition = pva.NameText.transform.localPosition;
                float setY = kaigyou ? 0.3f : 0.4f;
                plusVoteArea.transform.localPosition += new Vector3(-0.3f, setY);

            }
        }

        public static string SetDisplayForVanilla(PlayerControl seer,PlayerControl seen, string name, string suffix, string roleText, bool isMeeting)
        {
            if (!isMeeting) return name;

            // 表示するテキスト
            string addText = "";
            // 高さ調節用
            //int column = 0;
            float height = 2.0f;

            bool kaigyou = false;

            if (seen == Main.AllAlivePlayerControls.ElementAtOrDefault(0))
            {
                if (!Options.ShowReportReason.GetBool()) return name;
                (addText, kaigyou) = ButtonInfo();
                if (addText == "") return name;
            }
            else if (seen == Main.AllAlivePlayerControls.ElementAtOrDefault(1))
            {
                //(addText, column, height) = SetMyRoleInfo();
                (addText, kaigyou) = SetMyRoleInfo(seer);
                if (addText == "") return name;
            }

            // 名前が数行になる時に一段ぶん少なくする
            if (roleText != "") height -= 0.5f;
            if (suffix != "") height -= 0.5f;

            string plusDisplay = $"<align={"left"}>{addText}</align>" +
                $"<line-height={height}em>\n</line-height>";

            string adjust = $"<line-height={height}em>\n</line-height>";
            //for (int i = 0; i < column; i++) adjust += '\n';
            if (kaigyou) adjust += "\nㅤ";
            else adjust += "　";

            return plusDisplay + name + adjust;
        }


        public static void CreateButtonInfo(PlayerControl reporter, GameData.PlayerInfo target)
        {
            var reporterInfo = Utils.GetPlayerInfoById(reporter.PlayerId);
            var reporterColor = reporterInfo.Color;

            //ボタンレポート
            if (target == null)
            {
                reportInfo = Utils.ColorString(reporterColor,"ボタン");
                return;
            }

            //死体がある場合
            string temp = Utils.ColorString(Color.white,"通報先：");

            temp += Utils.ColorString(target.Color,target.ColorName);

            reportInfo = temp;
        }

        private static (string,bool) ButtonInfo()
        {
            bool enterKaigYOU = false;
            string returnString = $"<align={"left"}><size=80%>" + Utils.ColorString(Color.red, "『") + reportInfo + Utils.ColorString(Color.red, "』") + "</size></align>" + "\n";

            if (returnString.Length >= 12) enterKaigYOU = true;
            return (returnString,enterKaigYOU);
        }

        /// <summary>
        /// 自身のロール情報を画面に映す
        /// </summary>
        /// <param name="seer"></param>
        /// <returns>情報入り名称/改行ありか</returns>
        private static (string,bool) SetMyRoleInfo(PlayerControl seer)
        {
            string returnString = "";
            var cRole = seer.GetCustomRole();
            bool enterKaigYOU = false;

            if (!cRole.IsNotAssignRoles())
            {
                returnString = seer.GetRoleClass().MeetingInfo();
                if (returnString.Length >= 12) enterKaigYOU = true;
            }

            if (returnString != "")
            {
                returnString = $"<size=80%><align={"left"}>{returnString}</align></size>";
            }

            return (returnString,enterKaigYOU);
        }
    }
}
