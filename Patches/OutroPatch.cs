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
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            GameStates.InGame = false;

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
                var noLivingImposter = !PlayerControl.AllPlayerControls.ToArray().Any(p => p.GetCustomRole().IsImpostor() && !p.Data.IsDead);
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.GetCustomSubRole() == CustomRoles.Lovers) continue;
                    bool canWin = p.Is(RoleType.Impostor) || p.Is(RoleType.Madmate);
                    if (canWin) winner.Add(p);
                    if (Main.currentWinner == CustomWinner.Impostor && p.Is(CustomRoles.Egoist) && !p.Data.IsDead && noLivingImposter)
                        Main.currentWinner = CustomWinner.Egoist;
                }
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
            && (Main.currentWinner == CustomWinner.Impostor
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
            if (Main.currentWinner == CustomWinner.Egoist && CustomRoles.Egoist.IsEnable())
            { //Egoist横取り勝利
                winner = new();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if ((p.Is(CustomRoles.Egoist) && !p.Data.IsDead) || p.Is(CustomRoles.EgoSchrodingerCat))
                    {
                        winner.Add(p);
                    }
                }
            }
            ///以降追加勝利陣営 (winnerリセット無し)
            //Opportunist
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
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
                Main.currentWinner != CustomWinner.Draw)
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
                        winner = new();
                        winner.Add(pc);
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
                TempData.winners.Add(new WinningPlayerData(pc.Data));
                Main.winnerList.Add(pc.PlayerId);
            }

            Main.BountyTimer = new Dictionary<byte, float>();
            Main.BitPlayers = new Dictionary<byte, (byte, float)>();
            Main.SerialKillerTimer = new Dictionary<byte, float>();
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

            switch (Main.currentWinner)
            {
                //通常勝利
                case CustomWinner.Impostor:
                    CustomWinnerText = Utils.GetRoleName(CustomRoles.Impostor);
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Impostor);
                    break;
                case CustomWinner.Crewmate:
                    CustomWinnerText = Utils.GetRoleName(CustomRoles.Crewmate);
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Engineer);
                    break;
                //特殊勝利
                case CustomWinner.Jester:
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Jester);
                    CustomWinnerText = Utils.GetRoleName(CustomRoles.Jester);
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Jester);
                    break;
                case CustomWinner.Terrorist:
                    __instance.Foreground.material.color = Color.red;
                    __instance.BackgroundBar.material.color = Color.green;
                    CustomWinnerText = Utils.GetRoleName(CustomRoles.Terrorist);
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Terrorist);
                    break;
                case CustomWinner.Lovers:
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Lovers);
                    CustomWinnerText = $"{Utils.GetRoleName(CustomRoles.Lovers)}";
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Lovers);
                    break;
                case CustomWinner.Executioner:
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Executioner);
                    CustomWinnerText = Utils.GetRoleName(CustomRoles.Executioner);
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Executioner);
                    break;
                case CustomWinner.Arsonist:
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Arsonist);
                    CustomWinnerText = Utils.GetRoleName(CustomRoles.Arsonist);
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Arsonist);
                    break;
                case CustomWinner.Egoist:
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Egoist);
                    CustomWinnerText = Utils.GetRoleName(CustomRoles.Egoist);
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Egoist);
                    break;
                case CustomWinner.HASTroll:
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.HASTroll);
                    CustomWinnerText = Utils.GetRoleName(CustomRoles.HASTroll);
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.HASTroll);
                    break;
                //引き分け処理
                case CustomWinner.Draw:
                    __instance.WinText.text = GetString("ForceEnd");
                    __instance.WinText.color = Color.white;
                    __instance.BackgroundBar.material.color = Color.gray;
                    textRenderer.text = GetString("ForceEndText");
                    textRenderer.color = Color.gray;
                    break;
            }

            foreach (var additionalwinners in Main.additionalwinners)
            {
                if (Main.additionalwinners.Contains(AdditionalWinners.Opportunist))
                    AdditionalWinnerText += "＆" + Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Opportunist), Utils.GetRoleName(CustomRoles.Opportunist));

                if (Main.additionalwinners.Contains(AdditionalWinners.SchrodingerCat))
                    AdditionalWinnerText += "＆" + Helpers.ColorString(Utils.GetRoleColor(CustomRoles.SchrodingerCat), Utils.GetRoleName(CustomRoles.SchrodingerCat));

                if (Main.additionalwinners.Contains(AdditionalWinners.Executioner))
                    AdditionalWinnerText += "＆" + Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), Utils.GetRoleName(CustomRoles.Executioner));

                if (Main.additionalwinners.Contains(AdditionalWinners.HASFox))
                    AdditionalWinnerText += "＆" + Helpers.ColorString(Utils.GetRoleColor(CustomRoles.HASFox), Utils.GetRoleName(CustomRoles.HASFox));
            }
            if (Main.currentWinner != CustomWinner.Draw)
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
                roleSummaryText += $"\n<color={CustomWinnerColor}>★</color> {Main.AllPlayerNames[id]}<pos=25%>{Helpers.ColorString(Utils.GetRoleColor(Main.AllPlayerCustomRoles[id]), Utils.GetRoleName(Main.AllPlayerCustomRoles[id]))}{Utils.GetShowLastSubRolesText(id)}</pos><pos=44%>{Utils.GetProgressText(id)}</pos><pos=51%>{Utils.GetVitalText(id)}</pos>";
                cloneRoles.Remove(id);
            }
            foreach (var kvp in cloneRoles)
            {
                var id = kvp.Key;
                roleSummaryText += $"\n　 {Main.AllPlayerNames[id]}<pos=25%>{Helpers.ColorString(Utils.GetRoleColor(Main.AllPlayerCustomRoles[id]), Utils.GetRoleName(Main.AllPlayerCustomRoles[id]))}{Utils.GetShowLastSubRolesText(id)}</pos><pos=44%>{Utils.GetProgressText(id)}</pos><pos=51%>{Utils.GetVitalText(id)}</pos>";
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