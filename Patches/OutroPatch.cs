using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    class EndGamePatch
    {
        public static Dictionary<byte, string> SummaryText = new();
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            GameStates.InGame = false;

            SummaryText = new();
            foreach (var id in Main.AllPlayerCustomRoles.Keys)
                SummaryText[id] = Utils.SummaryTexts(id, disableColor: false);
            Logger.Info("-----------ゲーム終了-----------", "Phase");
            PlayerControl.GameOptions.killCooldown = Options.DefaultKillCooldown;
            //winnerListリセット
            TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
            Main.additionalwinners = new HashSet<AdditionalWinners>();
            var winner = new List<PlayerControl>();
            //勝者リスト作成
            if (TempData.DidHumansWin(endGameResult.GameOverReason) || endGameResult.GameOverReason.Equals(GameOverReason.HumansByTask) || endGameResult.GameOverReason.Equals(GameOverReason.HumansByVote))
            {
                if (Main.currentWinner == CustomWinner.Default)
                {
                    Main.currentWinner = CustomWinner.Crewmate;
                }
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.GetCustomSubRole() == CustomRoles.Lovers) continue;
                    bool canWin = p.Is(RoleType.Crewmate);
                    if (canWin) winner.Add(p);
                }
            }
            if (TempData.DidImpostorsWin(endGameResult.GameOverReason))
            {
                if (Main.currentWinner == CustomWinner.Default)
                    Main.currentWinner = CustomWinner.Impostor;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.GetCustomSubRole() == CustomRoles.Lovers) continue;
                    bool canWin = p.Is(RoleType.Impostor) || p.Is(RoleType.Madmate);
                    if (canWin) winner.Add(p);
                }
                Egoist.OverrideCustomWinner();
            }
            if (Main.currentWinner == CustomWinner.Jackal)
            {
                winner.Clear();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.Is(CustomRoles.Jackal) || p.Is(CustomRoles.JSchrodingerCat)) winner.Add(p);
                }
            }
            if (Main.currentWinner == CustomWinner.None)
            {
                winner.Clear();
            }

            //廃村時の処理など
            if (endGameResult.GameOverReason == GameOverReason.HumansDisconnect ||
            endGameResult.GameOverReason == GameOverReason.ImpostorDisconnect ||
            Main.currentWinner == CustomWinner.Draw)
            {
                winner = new List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    winner.Add(p);
                }
            }

            //単独勝利
            if (Main.currentWinner == CustomWinner.Jester && CustomRoles.Jester.IsEnable())
            { //Jester単独勝利
                winner = new();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == Main.ExiledJesterID)
                    {
                        winner.Add(p);
                    }
                }
            }
            if (Main.currentWinner == CustomWinner.Terrorist && CustomRoles.Terrorist.IsEnable())
            { //Terrorist単独勝利
                winner = new();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == Main.WonTerroristID)
                    {
                        winner.Add(p);
                    }
                }
            }
            if (CustomRoles.Lovers.IsEnable() && Options.CurrentGameMode == CustomGameMode.Standard && Main.LoversPlayers.Count > 0 && Main.LoversPlayers.ToArray().All(p => !p.Data.IsDead) //ラバーズが生きていて
            && (Main.currentWinner == CustomWinner.Impostor || Main.currentWinner == CustomWinner.Jackal
            || (Main.currentWinner == CustomWinner.Crewmate && !endGameResult.GameOverReason.Equals(GameOverReason.HumansByTask))))   //クルー勝利でタスク勝ちじゃなければ
            { //Loversの単独勝利
                winner = new();
                Main.currentWinner = CustomWinner.Lovers;
                foreach (var lp in Main.LoversPlayers)
                {
                    winner.Add(lp);
                }
            }
            if (Main.currentWinner == CustomWinner.Executioner && CustomRoles.Executioner.IsEnable())
            { //Executioner単独勝利
                winner = new();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == Main.WonExecutionerID)
                    {
                        winner.Add(p);
                    }
                }
            }
            if (Main.currentWinner == CustomWinner.Arsonist && CustomRoles.Arsonist.IsEnable())
            { //Arsonist単独勝利
                winner = new();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == Main.WonArsonistID)
                    {
                        winner.Add(p);
                    }
                }
            }
            TeamEgoist.SoloWin(winner);
            ///以降追加勝利陣営 (winnerリセット無し)
            //Opportunist
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (Main.currentWinner == CustomWinner.None) break;
                if (pc.Is(CustomRoles.Opportunist) && !pc.Data.IsDead && Main.currentWinner != CustomWinner.Draw && Main.currentWinner != CustomWinner.Terrorist)
                {
                    winner.Add(pc);
                    Main.additionalwinners.Add(AdditionalWinners.Opportunist);
                }
                //SchrodingerCat
                if (Options.CanBeforeSchrodingerCatWinTheCrewmate.GetBool())
                    if (pc.Is(CustomRoles.SchrodingerCat) && Main.currentWinner == CustomWinner.Crewmate)
                    {
                        winner.Add(pc);
                        Main.additionalwinners.Add(AdditionalWinners.SchrodingerCat);
                    }
                if (Main.currentWinner == CustomWinner.Jester)
                    foreach (var ExecutionerTarget in Main.ExecutionerTarget)
                    {
                        if (Main.ExiledJesterID == ExecutionerTarget.Value && pc.PlayerId == ExecutionerTarget.Key)
                        {
                            winner.Add(pc);
                            Main.additionalwinners.Add(AdditionalWinners.Executioner);
                        }
                    }
            }

            //HideAndSeek専用
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek &&
                Main.currentWinner != CustomWinner.Draw && Main.currentWinner != CustomWinner.None)
            {
                winner = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    var hasRole = Main.AllPlayerCustomRoles.TryGetValue(pc.PlayerId, out var role);
                    if (!hasRole) continue;
                    if (role.GetRoleType() == RoleType.Impostor)
                    {
                        if (TempData.DidImpostorsWin(endGameResult.GameOverReason))
                            winner.Add(pc);
                    }
                    else if (role.GetRoleType() == RoleType.Crewmate)
                    {
                        if (TempData.DidHumansWin(endGameResult.GameOverReason))
                            winner.Add(pc);
                    }
                    else if (role == CustomRoles.HASTroll && pc.Data.IsDead)
                    {
                        //トロールが殺されていれば単独勝ち
                        winner = new()
                        {
                            pc
                        };
                        break;
                    }
                    else if (role == CustomRoles.HASFox && Main.currentWinner != CustomWinner.HASTroll && !pc.Data.IsDead)
                    {
                        winner.Add(pc);
                        Main.additionalwinners.Add(AdditionalWinners.HASFox);
                    }
                }
            }
            Main.winnerList = new();
            foreach (var pc in winner)
            {
                if (Main.currentWinner is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;

                TempData.winners.Add(new WinningPlayerData(pc.Data));
                Main.winnerList.Add(pc.PlayerId);
            }

            BountyHunter.ChangeTimer = new();
            Main.BitPlayers = new Dictionary<byte, (byte, float)>();
            Main.isDoused = new Dictionary<(byte, byte), bool>();

            NameColorManager.Instance.RpcReset();
            Main.VisibleTasksCount = false;
            if (AmongUsClient.Instance.AmHost)
            {
                Main.RealOptionsData.KillCooldown = Options.DefaultKillCooldown;
                PlayerControl.LocalPlayer.RpcSyncSettings(Main.RealOptionsData);
            }
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch
    {
        public static string LastWinsText = "";

        public static void Postfix(EndGameManager __instance)
        {
            if (!Main.playerVersion.ContainsKey(0)) return;
            //#######################################
            //          ==勝利陣営表示==
            //#######################################

            GameObject bonusText = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            bonusText.transform.position = new Vector3(__instance.WinText.transform.position.x, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
            bonusText.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            TMPro.TMP_Text textRenderer = bonusText.GetComponent<TMPro.TMP_Text>();
            textRenderer.text = "";

            string CustomWinnerText = "";
            string AdditionalWinnerText = "";
            string CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Crewmate);

            var winnerRole = (CustomRoles)Main.currentWinner;
            if (winnerRole >= 0)
            {
                CustomWinnerText = Utils.GetRoleName(winnerRole);
                CustomWinnerColor = Utils.GetRoleColorCode(winnerRole);
                if (winnerRole.IsNeutral())
                {
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
                }
            }
            if (AmongUsClient.Instance.AmHost && Main.AllPlayerCustomRoles[0] == CustomRoles.GM)
            {
                __instance.WinText.text = "Game Over";
                __instance.WinText.color = Utils.GetRoleColor(CustomRoles.GM);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.GM);
            }
            switch (Main.currentWinner)
            {
                //通常勝利
                case CustomWinner.Crewmate:
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Engineer);
                    break;
                //特殊勝利
                case CustomWinner.Terrorist:
                    __instance.Foreground.material.color = Color.red;
                    break;
                //引き分け処理
                case CustomWinner.Draw:
                    __instance.WinText.text = GetString("ForceEnd");
                    __instance.WinText.color = Color.white;
                    __instance.BackgroundBar.material.color = Color.gray;
                    textRenderer.text = GetString("ForceEndText");
                    textRenderer.color = Color.gray;
                    break;
                //全滅
                case CustomWinner.None:
                    __instance.WinText.text = "";
                    __instance.WinText.color = Color.black;
                    __instance.BackgroundBar.material.color = Color.gray;
                    textRenderer.text = GetString("EveryoneDied");
                    textRenderer.color = Color.gray;
                    break;
            }

            foreach (var additionalwinners in Main.additionalwinners)
            {
                var addWinnerRole = (CustomRoles)additionalwinners;
                AdditionalWinnerText += "＆" + Helpers.ColorString(Utils.GetRoleColor(addWinnerRole), Utils.GetRoleName(addWinnerRole));
            }
            if (Main.currentWinner != CustomWinner.Draw && Main.currentWinner != CustomWinner.None)
            {
                textRenderer.text = $"<color={CustomWinnerColor}>{CustomWinnerText}{AdditionalWinnerText}{GetString("Win")}</color>";
            }
            LastWinsText = textRenderer.text.RemoveHtmlTags();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //#######################################
            //           ==最終結果表示==
            //#######################################

            var position = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));
            GameObject roleSummary = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            roleSummary.transform.position = new Vector3(__instance.Navigation.ExitButton.transform.position.x + 0.1f, position.y - 0.1f, -14f);
            roleSummary.transform.localScale = new Vector3(1f, 1f, 1f);

            string roleSummaryText = $"{GetString("RoleSummaryText")}";
            Dictionary<byte, CustomRoles> cloneRoles = new(Main.AllPlayerCustomRoles);
            foreach (var id in Main.winnerList)
            {
                roleSummaryText += $"\n<color={CustomWinnerColor}>★</color> " + EndGamePatch.SummaryText[id];
                cloneRoles.Remove(id);
            }
            foreach (var kvp in cloneRoles)
            {
                var id = kvp.Key;
                roleSummaryText += $"\n　 " + Utils.SummaryTexts(id, disableColor: false);
            }
            TMPro.TMP_Text roleSummaryTextMesh = roleSummary.GetComponent<TMPro.TMP_Text>();
            roleSummaryTextMesh.alignment = TMPro.TextAlignmentOptions.TopLeft;
            roleSummaryTextMesh.color = Color.white;
            roleSummaryTextMesh.outlineWidth *= 1.2f;
            roleSummaryTextMesh.fontSizeMin = 1.25f;
            roleSummaryTextMesh.fontSizeMax = 1.25f;
            roleSummaryTextMesh.fontSize = 1.25f;

            var roleSummaryTextMeshRectTransform = roleSummaryTextMesh.GetComponent<RectTransform>();
            roleSummaryTextMeshRectTransform.anchoredPosition = new Vector2(position.x + 3.5f, position.y - 0.1f);
            roleSummaryTextMesh.text = roleSummaryText;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //Utils.ApplySuffix();
        }
    }
}