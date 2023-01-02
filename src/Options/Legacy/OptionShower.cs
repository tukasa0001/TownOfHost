using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TownOfHost.Translator;
using TownOfHost.Roles;

namespace TownOfHost
{
    public static class OptionShower
    {
        public static int currentPage = 0;
        public static List<string> pages = new();
        static OptionShower()
        {

        }
        public static string GetText()
        {
            //初期化
            string text = "";
            pages = new()
            {
                //1ページに基本ゲーム設定を格納
                GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10) + "\n\n"
            };
            //ゲームモードの表示
            text += $"{OldOptions.GameMode.GetName()}: {OldOptions.GameMode.GetString()}\n\n";
            if (OldOptions.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
            {
                text += $"<color=#ff0000>{GetString("Message.HideGameSettings")}</color>";
            }
            else
            {
                //Standardの時のみ実行
                if (OldOptions.CurrentGameMode == CustomGameMode.Standard)
                {
                    //有効な役職一覧
                    /*text += $"<color={GM.Ref<GM>().RoleColor}>{GM.Ref<GM>().RoleName}:</color> {OldOptions.EnableGM.GetString()}\n\n";*/
                    text += GetString("ActiveRolesList") + "\n";
                    foreach (var kvp in OldOptions.CustomRoleSpawnChances)
                        if (kvp.Value.GameMode is CustomGameMode.Standard or CustomGameMode.All && kvp.Value.GetBool()) //スタンダードか全てのゲームモードで表示する役職
                            text += $"{Utils.ColorString(kvp.Key.GetReduxRole().RoleColor, kvp.Key.GetReduxRole().RoleName)}: {kvp.Value.GetString()}×{kvp.Key.GetReduxRole().Count}\n";
                    pages.Add(text + "\n\n");
                    text = "";
                }
                //有効な役職と詳細設定一覧
                pages.Add("");
                nameAndValue(OldOptions.EnableGM);
                foreach (var kvp in OldOptions.CustomRoleSpawnChances)
                {
                    if (!kvp.Key.GetReduxRole().IsEnable() || kvp.Value.IsHiddenOn(OldOptions.CurrentGameMode)) continue;
                    text += "\n";
                    text += $"{Utils.ColorString(kvp.Key.GetReduxRole().RoleColor, kvp.Key.GetReduxRole().RoleName)}: {kvp.Value.GetString()}×{kvp.Key.GetReduxRole().Count}\n";
                    ShowChildren(kvp.Value, ref text, kvp.Key.GetReduxRole().RoleColor.ShadeColor(-0.5f), 1);
                    string rule = Utils.ColorString(Palette.ImpostorRed.ShadeColor(-0.5f), "┣ ");
                    string ruleFooter = Utils.ColorString(Palette.ImpostorRed.ShadeColor(-0.5f), "┗ ");
                    if (kvp.Key.GetReduxRole().IsMadmate()) //マッドメイトの時に追加する詳細設定
                    {
                        text += $"{rule}{OldOptions.MadmateCanFixLightsOut.GetName()}: {OldOptions.MadmateCanFixLightsOut.GetString()}\n";
                        text += $"{rule}{OldOptions.MadmateCanFixComms.GetName()}: {OldOptions.MadmateCanFixComms.GetString()}\n";
                        text += $"{rule}{OldOptions.MadmateHasImpostorVision.GetName()}: {OldOptions.MadmateHasImpostorVision.GetString()}\n";
                        text += $"{rule}{OldOptions.MadmateCanSeeKillFlash.GetName()}: {OldOptions.MadmateCanSeeKillFlash.GetString()}\n";
                        text += $"{rule}{OldOptions.MadmateCanSeeOtherVotes.GetName()}: {OldOptions.MadmateCanSeeOtherVotes.GetString()}\n";
                        text += $"{rule}{OldOptions.MadmateCanSeeDeathReason.GetName()}: {OldOptions.MadmateCanSeeDeathReason.GetString()}\n";
                        text += $"{rule}{OldOptions.MadmateRevengeCrewmate.GetName()}: {OldOptions.MadmateRevengeCrewmate.GetString()}\n";
                        text += $"{rule}{OldOptions.MadmateVentCooldown.GetName()}: {OldOptions.MadmateVentCooldown.GetString()}\n";
                        text += $"{ruleFooter}{OldOptions.MadmateVentMaxTime.GetName()}: {OldOptions.MadmateVentMaxTime.GetString()}\n";
                    }
                    if (kvp.Key.GetReduxRole().CanMakeMadmate()) //シェイプシフター役職の時に追加する詳細設定
                    {
                        text += $"{ruleFooter}{OldOptions.CanMakeMadmateCount.GetName()}: {OldOptions.CanMakeMadmateCount.GetString()}\n";
                    }
                }

                foreach (var opt in OptionItem.AllOptions.Where(x => x.Id >= 90000 && !x.IsHiddenOn(OldOptions.CurrentGameMode) && x.Parent == null))
                {
                    if (opt.IsHeader) text += "\n";
                    text += $"{opt.GetName()}: {opt.GetString()}\n";
                    if (opt.GetBool())
                        ShowChildren(opt, ref text, Color.white, 1);
                }
                //Onの時に子要素まで表示するメソッド
                void nameAndValue(OptionItem o) => text += $"{o.GetName()}: {o.GetString()}\n";
            }
            //1ページにつき35行までにする処理
            List<string> tmp = new(text.Split("\n\n"));
            for (var i = 0; i < tmp.Count; i++)
            {
                if (pages[^1].Count(c => c == '\n') + 1 + tmp[i].Count(c => c == '\n') + 1 > 35)
                    pages.Add(tmp[i] + "\n\n");
                else pages[^1] += tmp[i] + "\n\n";
            }
            if (currentPage >= pages.Count) currentPage = pages.Count - 1; //現在のページが最大ページ数を超えていれば最後のページに修正
            return $"{pages[currentPage]}{GetString("PressTabToNextPage")}({currentPage + 1}/{pages.Count})";
        }
        public static void Next()
        {
            currentPage++;
            if (currentPage >= pages.Count) currentPage = 0; //現在のページが最大ページを超えていれば最初のページに
        }
        private static void ShowChildren(OptionItem option, ref string text, Color color, int deep = 0)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                text += string.Concat(Enumerable.Repeat(Utils.ColorString(color, "┃"), deep - 1));
                text += Utils.ColorString(color, opt.Index == option.Children.Count ? "┗ " : "┣ ");
                text += $"{opt.Value.GetName()}: {opt.Value.GetString()}\n";
                if (opt.Value.GetBool()) ShowChildren(opt.Value, ref text, color, deep + 1);
            }
        }
    }
}