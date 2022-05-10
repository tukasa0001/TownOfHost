using HarmonyLib;
using System.Collections.Generic;
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

            Logger.info("-----------ゲーム終了-----------", "Phase");
            //winnerListリセット
            TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
            main.additionalwinners = new HashSet<AdditionalWinners>();
            var winner = new List<PlayerControl>();
            //勝者リスト作成
            if (TempData.DidHumansWin(endGameResult.GameOverReason) || endGameResult.GameOverReason.Equals(GameOverReason.HumansByTask) || endGameResult.GameOverReason.Equals(GameOverReason.HumansByVote))
            {
                if (main.currentWinner == CustomWinner.Default)
                {
                    main.currentWinner = CustomWinner.Crewmate;
                }
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.getCustomSubRole() == CustomRoles.Lovers) continue;
                    bool canWin = p.Is(RoleType.Crewmate);
                    if (canWin) winner.Add(p);
                }
            }
            if (TempData.DidImpostorsWin(endGameResult.GameOverReason))
            {
                if (main.currentWinner == CustomWinner.Default)
                    main.currentWinner = CustomWinner.Impostor;
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.getCustomSubRole() == CustomRoles.Lovers) continue;
                    bool canWin = p.Is(RoleType.Impostor) || p.Is(RoleType.Madmate);
                    if (canWin) winner.Add(p);
                    if (main.currentWinner == CustomWinner.Impostor && p.Is(CustomRoles.Egoist) && !p.Data.IsDead && main.AliveImpostorCount == 0)
                        main.currentWinner = CustomWinner.Egoist;
                }
            }

            //廃村時の処理など
            if (endGameResult.GameOverReason == GameOverReason.HumansDisconnect ||
            endGameResult.GameOverReason == GameOverReason.ImpostorDisconnect ||
            main.currentWinner == CustomWinner.Draw)
            {
                winner = new List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    winner.Add(p);
                }
            }
            foreach (var p in winner)
            {
                TempData.winners.Add(new WinningPlayerData(p.Data));
            }

            //単独勝利
            if (main.currentWinner == CustomWinner.Jester && CustomRoles.Jester.isEnable())
            { //Jester単独勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == main.ExiledJesterID)
                    {
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                        winner = new();
                        winner.Add(p);
                    }
                }
            }
            if (main.currentWinner == CustomWinner.Terrorist && CustomRoles.Terrorist.isEnable())
            { //Terrorist単独勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == main.WonTerroristID)
                    {
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                        winner = new();
                        winner.Add(p);
                    }
                }
            }
            if (CustomRoles.Lovers.isEnable() && main.isLoversDead == false //ラバーズが生きていて
            && main.currentWinner == CustomWinner.Impostor
            && !endGameResult.GameOverReason.Equals(GameOverReason.HumansByTask))   //クルー勝利でタスク勝ちじゃなければ
            { //Loversの単独勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                winner = new();
                main.currentWinner = CustomWinner.Lovers;
                foreach (var lp in main.LoversPlayers)
                {
                    TempData.winners.Add(new WinningPlayerData(lp.Data));
                    winner.Add(lp);
                }
            }
            if (main.currentWinner == CustomWinner.Executioner && CustomRoles.Executioner.isEnable())
            { //Executioner単独勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == main.WonExecutionerID)
                    {
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                        winner = new();
                        winner.Add(p);
                    }
                }
            }
            if (main.currentWinner == CustomWinner.Arsonist && CustomRoles.Arsonist.isEnable())
            { //Arsonist単独勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == main.WonArsonistID)
                    {
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                        winner = new();
                        winner.Add(p);
                    }
                }
            }
            if (main.currentWinner == CustomWinner.Egoist && CustomRoles.Egoist.isEnable())
            { //Egoist横取り勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                winner = new();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if ((p.Is(CustomRoles.Egoist) && !p.Data.IsDead) || p.Is(CustomRoles.EgoSchrodingerCat))
                    {
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                        winner.Add(p);
                    }
                }
            }
            ///以降追加勝利陣営 (winnerリセット無し)
            //Opportunist
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.Opportunist) && !pc.Data.IsDead && main.currentWinner != CustomWinner.Draw && main.currentWinner != CustomWinner.Terrorist)
                {
                    TempData.winners.Add(new WinningPlayerData(pc.Data));
                    winner.Add(pc);
                    main.additionalwinners.Add(AdditionalWinners.Opportunist);
                }
                //SchrodingerCat
                if (Options.CanBeforeSchrodingerCatWinTheCrewmate.GetBool())
                    if (pc.Is(CustomRoles.SchrodingerCat) && main.currentWinner == CustomWinner.Crewmate)
                    {
                        TempData.winners.Add(new WinningPlayerData(pc.Data));
                        winner.Add(pc);
                        main.additionalwinners.Add(AdditionalWinners.SchrodingerCat);
                    }
                if (main.currentWinner == CustomWinner.Jester)
                    foreach (var ExecutionerTarget in main.ExecutionerTarget)
                    {
                        if (main.ExiledJesterID == ExecutionerTarget.Value && pc.PlayerId == ExecutionerTarget.Key)
                        {
                            TempData.winners.Add(new WinningPlayerData(pc.Data));
                            winner.Add(pc);
                            main.additionalwinners.Add(AdditionalWinners.Executioner);
                        }
                    }
            }

            //HideAndSeek専用
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek &&
                main.currentWinner != CustomWinner.Draw)
            {
                var winners = new List<PlayerControl>();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    var hasRole = main.AllPlayerCustomRoles.TryGetValue(pc.PlayerId, out var role);
                    if (!hasRole) continue;
                    if (role.getRoleType() == RoleType.Impostor)
                    {
                        if (TempData.DidImpostorsWin(endGameResult.GameOverReason))
                            winners.Add(pc);
                    }
                    else if (role.getRoleType() == RoleType.Crewmate)
                    {
                        if (TempData.DidHumansWin(endGameResult.GameOverReason))
                            winners.Add(pc);
                    }
                    if (main.currentWinner == CustomWinner.HASTroll)
                    {
                        winners = new List<PlayerControl>();
                        winners.Add(pc);
                        break;
                    }
                    else if (role == CustomRoles.HASFox && !pc.Data.IsDead)
                    {
                        winners.Add(pc);
                        main.additionalwinners.Add(AdditionalWinners.HASFox);
                    }
                }
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach (var pc in winners)
                {
                    TempData.winners.Add(new WinningPlayerData(pc.Data));
                }
            }
            main.winnerList = new();
            foreach (var pc in winner)
            {
                main.winnerList.Add(pc.PlayerId);
            }

            main.BountyTimer = new Dictionary<byte, float>();
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            main.SerialKillerTimer = new Dictionary<byte, float>();
            main.isDoused = new Dictionary<(byte, byte), bool>();

            NameColorManager.Instance.RpcReset();
            main.VisibleTasksCount = false;
            if (AmongUsClient.Instance.AmHost)
            {
                PlayerControl.LocalPlayer.RpcSyncSettings(main.RealOptionsData);
            }
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch
    {
        public static void Postfix(EndGameManager __instance)
        {
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
            string CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Crewmate);

            switch (main.currentWinner)
            {
                //通常勝利
                case CustomWinner.Impostor:
                    CustomWinnerText = Utils.getRoleName(CustomRoles.Impostor);
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Impostor);
                    break;
                case CustomWinner.Crewmate:
                    CustomWinnerText = Utils.getRoleName(CustomRoles.Crewmate);
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Crewmate);
                    break;
                //特殊勝利
                case CustomWinner.Jester:
                    __instance.BackgroundBar.material.color = Utils.getRoleColor(CustomRoles.Jester);
                    CustomWinnerText = Utils.getRoleName(CustomRoles.Jester);
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Jester);
                    break;
                case CustomWinner.Terrorist:
                    __instance.Foreground.material.color = Color.red;
                    __instance.BackgroundBar.material.color = Color.green;
                    CustomWinnerText = Utils.getRoleName(CustomRoles.Terrorist);
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Terrorist);
                    break;
                case CustomWinner.Lovers:
                    __instance.BackgroundBar.material.color = Utils.getRoleColor(CustomRoles.Lovers);
                    CustomWinnerText = $"{Utils.getRoleName(CustomRoles.Lovers)}";
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Lovers);
                    break;
                case CustomWinner.Executioner:
                    __instance.BackgroundBar.material.color = Utils.getRoleColor(CustomRoles.Executioner);
                    CustomWinnerText = Utils.getRoleName(CustomRoles.Executioner);
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Executioner);
                    break;
                case CustomWinner.Arsonist:
                    __instance.BackgroundBar.material.color = Utils.getRoleColor(CustomRoles.Arsonist);
                    CustomWinnerText = Utils.getRoleName(CustomRoles.Arsonist);
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Arsonist);
                    break;
                case CustomWinner.Egoist:
                    __instance.BackgroundBar.material.color = Utils.getRoleColor(CustomRoles.Egoist);
                    CustomWinnerText = Utils.getRoleName(CustomRoles.Egoist);
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Egoist);
                    break;
                case CustomWinner.HASTroll:
                    __instance.BackgroundBar.material.color = Utils.getRoleColor(CustomRoles.HASTroll);
                    CustomWinnerText = Utils.getRoleName(CustomRoles.HASTroll);
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.HASTroll);
                    break;
                //引き分け処理
                case CustomWinner.Draw:
                    __instance.BackgroundBar.material.color = Color.gray;
                    textRenderer.text = "ホストから強制終了コマンドが入力されました";
                    textRenderer.color = Color.gray;
                    __instance.WinText.text = "廃村";
                    __instance.WinText.color = Color.white;
                    break;
            }

            foreach (var additionalwinners in main.additionalwinners)
            {
                if (main.additionalwinners.Contains(AdditionalWinners.Opportunist))
                    AdditionalWinnerText += $"＆<color={Utils.getRoleColorCode(CustomRoles.Opportunist)}>{Utils.getRoleName(CustomRoles.Opportunist)}</color>";

                if (main.additionalwinners.Contains(AdditionalWinners.SchrodingerCat))
                    AdditionalWinnerText += $"＆<color={Utils.getRoleColorCode(CustomRoles.SchrodingerCat)}>{Utils.getRoleName(CustomRoles.SchrodingerCat)}</color>";

                if (main.additionalwinners.Contains(AdditionalWinners.Executioner))
                    AdditionalWinnerText += $"＆<color={Utils.getRoleColorCode(CustomRoles.Executioner)}>{Utils.getRoleName(CustomRoles.Executioner)}</color>";

                if (main.additionalwinners.Contains(AdditionalWinners.HASFox))
                    AdditionalWinnerText += $"＆<color={Utils.getRoleColorCode(CustomRoles.HASFox)}>{Utils.getRoleName(CustomRoles.HASFox)}</color>";
            }
            if (main.currentWinner != CustomWinner.Draw)
            {
                textRenderer.text = $"<color={CustomWinnerColor}>{CustomWinnerText}{AdditionalWinnerText}{getString("Win")}</color>";
            }

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //#######################################
            //           ==最終結果表示==
            //#######################################

            var position = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));
            GameObject roleSummary = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            roleSummary.transform.position = new Vector3(__instance.Navigation.ExitButton.transform.position.x + 0.1f, position.y - 0.1f, -14f);
            roleSummary.transform.localScale = new Vector3(1f, 1f, 1f);

            string roleSummaryText = $"{getString("RoleSummaryText")}";
            Dictionary<byte, CustomRoles> cloneRoles = new(main.AllPlayerCustomRoles);
            foreach (var id in main.winnerList)
            {
                roleSummaryText += $"\n<color={CustomWinnerColor}>★</color> {main.RealNames[id]} : <color={Utils.getRoleColorCode(main.AllPlayerCustomRoles[id])}>{Utils.getRoleName(main.AllPlayerCustomRoles[id])}{Utils.GetShowLastSubRolesText(id)}</color> {Utils.getTaskText(id)}  {Utils.getVitalText(id)}";
                cloneRoles.Remove(id);
            }
            foreach (var kvp in cloneRoles)
            {
                var id = kvp.Key;
                roleSummaryText += $"\n　 {main.RealNames[id]} : <color={Utils.getRoleColorCode(main.AllPlayerCustomRoles[id])}>{Utils.getRoleName(main.AllPlayerCustomRoles[id])}{Utils.GetShowLastSubRolesText(id)}</color> {Utils.getTaskText(id)}  {Utils.getVitalText(id)}";
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
