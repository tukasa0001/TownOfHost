using System.Collections.Generic;
using System.Linq;
using static TownOfHost.Translator;

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
                PlayerControl.GameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10) + "\n\n"
            };
            //ゲームモードの表示
            text += $"{Options.GameMode.GetName()}: {Options.GameMode.GetString()}\n\n";
            if (Options.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
            {
                text += $"<color=#ff0000>{GetString("Message.HideGameSettings")}</color>";
            }
            else
            {
                //Standardの時のみ実行
                if (Options.CurrentGameMode == CustomGameMode.Standard)
                {
                    //有効な役職一覧
                    text += $"<color={Utils.GetRoleColorCode(CustomRoles.LastImpostor)}>{Utils.GetRoleName(CustomRoles.LastImpostor)}:</color> {Options.EnableLastImpostor.GetString()}\n\n";
                    text += $"<color={Utils.GetRoleColorCode(CustomRoles.GM)}>{Utils.GetRoleName(CustomRoles.GM)}:</color> {Options.EnableGM.GetString()}\n\n";
                    text += GetString("ActiveRolesList") + "\n";
                    foreach (var kvp in Options.CustomRoleSpawnChances)
                        if (kvp.Value.GameMode is CustomGameMode.Standard or CustomGameMode.All && kvp.Value.Enabled) //スタンダードか全てのゲームモードで表示する役職
                            text += $"{Helpers.ColorString(Utils.GetRoleColor(kvp.Key), Utils.GetRoleName(kvp.Key))}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}\n";
                    pages.Add(text + "\n\n");
                    text = "";
                }
                //有効な役職と詳細設定一覧
                pages.Add("");

                if (Options.EnableLastImpostor.GetBool() && !Options.EnableLastImpostor.IsHidden(Options.CurrentGameMode))
                {
                    text += $"<color={Utils.GetRoleColorCode(CustomRoles.LastImpostor)}>{Utils.GetRoleName(CustomRoles.LastImpostor)}:</color> {Options.EnableLastImpostor.GetString()}\n";
                    ShowChildren(Options.EnableLastImpostor, ref text, 1);
                }
                nameAndValue(Options.EnableGM);
                foreach (var kvp in Options.CustomRoleSpawnChances)
                {
                    if (!kvp.Key.IsEnable() || kvp.Value.IsHidden(Options.CurrentGameMode)) continue;
                    text += "\n";
                    text += $"{Helpers.ColorString(Utils.GetRoleColor(kvp.Key), Utils.GetRoleName(kvp.Key))}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}\n";
                    ShowChildren(kvp.Value, ref text, 1);
                    if (kvp.Key.IsMadmate()) //マッドメイトの時に追加する詳細設定
                    {
                        text += $"┣ {Options.MadmateCanFixLightsOut.GetName()}: {Options.MadmateCanFixLightsOut.GetString()}\n";
                        text += $"┣ {Options.MadmateCanFixComms.GetName()}: {Options.MadmateCanFixComms.GetString()}\n";
                        text += $"┣ {Options.MadmateHasImpostorVision.GetName()}: {Options.MadmateHasImpostorVision.GetString()}\n";
                        text += $"┣ {Options.MadmateCanSeeKillFlash.GetName()}: {Options.MadmateCanSeeKillFlash.GetString()}\n";
                        text += $"┣ {Options.MadmateCanSeeOtherVotes.GetName()}: {Options.MadmateCanSeeOtherVotes.GetString()}\n";
                        text += $"┣ {Options.MadmateVentCooldown.GetName()}: {Options.MadmateVentCooldown.GetString()}\n";
                        text += $"┗ {Options.MadmateVentMaxTime.GetName()}: {Options.MadmateVentMaxTime.GetString()}\n";
                    }
                    if (kvp.Key is CustomRoles.Shapeshifter/* or CustomRoles.ShapeMaster*/ or CustomRoles.BountyHunter or CustomRoles.SerialKiller) //シェイプシフター役職の時に追加する詳細設定
                    {
                        text += $"┗ {Options.CanMakeMadmateCount.GetName()}: {Options.CanMakeMadmateCount.GetString()}\n";
                    }
                }

                foreach (var opt in CustomOption.Options.Where(x => x.Id >= 90000 && !x.IsHidden(Options.CurrentGameMode) && x.Parent == null))
                {
                    if (opt.isHeader) text += "\n";
                    text += $"{opt.GetName()}: {opt.GetString()}\n";
                    if (opt.Enabled)
                        ShowChildren(opt, ref text, 1);
                }
                //Onの時に子要素まで表示するメソッド
                void nameAndValue(CustomOption o) => text += $"{o.GetName()}: {o.GetString()}\n";
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
        private static void ShowChildren(CustomOption option, ref string text, int deep = 0)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name == "Maximum") continue; //Maximumの項目は飛ばす
                text += string.Concat(Enumerable.Repeat("┃", deep - 1));
                text += opt.Index == option.Children.Count ? "┗ " : "┣ ";
                text += $"{opt.Value.GetName()}: {opt.Value.GetString()}\n";
                if (opt.Value.Enabled) ShowChildren(opt.Value, ref text, deep + 1);
            }
        }
    }
}