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
            pages = new();
            //1ページに基本ゲーム設定を格納
            pages.Add(PlayerControl.GameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10) + "\n\n");
            //ゲームモードの表示
            text += $"{Options.GameMode.GetName()}: {Options.GameMode.GetString()}\n\n";
            //Standardの時のみ実行
            if (Options.CurrentGameMode == CustomGameMode.Standard)
            {
                //役職一覧
                text += $"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>{GetString("LastImpostor")}:</color> {Options.EnableLastImpostor.GetString()}\n\n";
                foreach (var kvp in Options.CustomRoleSpawnChances)
                    if (kvp.Value.GameMode is CustomGameMode.Standard or CustomGameMode.All) //スタンダードか全てのゲームモードで表示する役職
                        text += $"<color={Utils.GetRoleColorCode(kvp.Key)}>{Utils.GetRoleName(kvp.Key)}:</color> {kvp.Value.GetString()}×{kvp.Key.GetCount()}\n";
                pages.Add(text + "\n\n");
                text = "";
            }
            //有効な役職と詳細設定一覧
            pages.Add("");
            if (Options.CurrentGameMode == CustomGameMode.Standard)
            {
                if (Options.EnableLastImpostor.GetBool())
                {
                    text += $"<color={Utils.GetRoleColorCode(CustomRoles.Impostor)}>{GetString("LastImpostor")}:</color> {Options.EnableLastImpostor.GetString()}\n";
                    text += $"\t{GetString("LastImpostorKillCooldown")}: {Options.LastImpostorKillCooldown.GetString()}\n\n";
                }
            }
            foreach (var kvp in Options.CustomRoleSpawnChances)
            {
                if (!kvp.Key.IsEnable()) continue;
                if (!(kvp.Value.GameMode == Options.CurrentGameMode || kvp.Value.GameMode == CustomGameMode.All)) continue; //現在のゲームモードでも全てのゲームモードでも表示しない役職なら飛ばす
                text += $"<color={Utils.GetRoleColorCode(kvp.Key)}>{Utils.GetRoleName(kvp.Key)}:</color> {kvp.Value.GetString()}×{kvp.Key.GetCount()}\n";
                foreach (var c in kvp.Value.Children) //詳細設定をループする
                {
                    if (c.Name == "Maximum") continue; //Maximumの項目は飛ばす
                    text += $"\t{c.GetName()}: {c.GetString()}\n";
                }
                if (kvp.Key.IsMadmate()) //マッドメイトの時に追加する詳細設定
                {
                    text += $"\t{Options.MadmateCanFixLightsOut.GetName()}: {Options.MadmateCanFixLightsOut.GetString()}\n";
                    text += $"\t{Options.MadmateCanFixComms.GetName()}: {Options.MadmateCanFixComms.GetString()}\n";
                    text += $"\t{Options.MadmateHasImpostorVision.GetName()}: {Options.MadmateHasImpostorVision.GetString()}\n";
                    text += $"\t{Options.MadmateVentCooldown.GetName()}: {Options.MadmateVentCooldown.GetString()}\n";
                    text += $"\t{Options.MadmateVentMaxTime.GetName()}: {Options.MadmateVentMaxTime.GetString()}\n";
                }
                if (kvp.Key is CustomRoles.Shapeshifter or CustomRoles.ShapeMaster or CustomRoles.Mafia or CustomRoles.BountyHunter or CustomRoles.SerialKiller) //シェイプシフター役職の時に追加する詳細設定
                {
                    text += $"\t{Options.CanMakeMadmateCount.GetName()}: {Options.CanMakeMadmateCount.GetString()}\n";
                }
                if (kvp.Key == CustomRoles.Mayor && Options.MayorHasPortableButton.GetBool())
                {
                    text += $"\t{Options.MayorNumOfUseButton.GetName()}: {Options.MayorNumOfUseButton.GetString()}\n";
                }
                text += "\n";
            }
            //Onの時に子要素まで表示するメソッド
            void listUp(CustomOption o)
            {
                if (o.GetBool())
                {
                    text += $"{o.GetName()}: {o.GetString()}\n";
                    foreach (var c in o.Children)
                        text += $"\t{c.GetName_v()}: {c.GetString()}\n";
                    text += "\n";
                }
            }
            void nameAndValue(CustomOption o) => text += $"{o.GetName()}: {o.GetString()}\n";
            if (Options.CurrentGameMode == CustomGameMode.Standard)
            {
                listUp(Options.SyncButtonMode);
                listUp(Options.VoteMode);
                listUp(Options.SabotageTimeControl);
                nameAndValue(Options.StandardHAS);
            }
            else if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                nameAndValue(Options.AllowCloseDoors);
                nameAndValue(Options.KillDelay);
                //nameAndValue(Options.IgnoreCosmetics);
                nameAndValue(Options.IgnoreVent);
            }
            text += "\n";
            listUp(Options.DisableTasks);
            listUp(Options.RandomMapsMode);
            nameAndValue(Options.NoGameEnd);
            nameAndValue(Options.GhostCanSeeOtherRoles);
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
    }
}