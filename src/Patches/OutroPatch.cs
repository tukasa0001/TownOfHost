using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using UnityEngine;
using TownOfHost.Modules;
using static TownOfHost.Translator;
using TownOfHost.Roles;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    class EndGamePatch
    {
        public static Dictionary<byte, string> SummaryText = new();
        public static string KillLog = "";
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            GameStates.InGame = false;
            Game.State = GameState.InLobby;

            SummaryText = new();
            foreach (var id in TOHPlugin.PlayerStates.Keys)
                SummaryText[id] = Utils.SummaryTexts(id, disableColor: false);
            KillLog = GetString("KillLog") + ":";
            foreach (var kvp in TOHPlugin.PlayerStates.OrderBy(x => x.Value.RealKiller.Item1.Ticks))
            {
                var date = kvp.Value.RealKiller.Item1;
                if (date == DateTime.MinValue) continue;
                var killerId = kvp.Value.GetRealKiller();
                var targetId = kvp.Key;
                KillLog += $"\n{date.ToString("T")} {TOHPlugin.AllPlayerNames[targetId]}({Utils.GetDisplayRoleName(targetId)}{Utils.GetSubRolesText(targetId)}) [{Utils.GetVitalText(kvp.Key)}]";
                if (killerId != byte.MaxValue && killerId != targetId)
                    KillLog += $"\n\t\t⇐ {TOHPlugin.AllPlayerNames[killerId]}({Utils.GetDisplayRoleName(killerId)}{Utils.GetSubRolesText(killerId)})";
            }
            Logger.Info("-----------ゲーム終了-----------", "Phase");
            TOHPlugin.NormalOptions.KillCooldown = OldOptions.DefaultKillCooldown;
            //winnerListリセット
            TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
            var winner = new List<PlayerControl>();
            //勝者リスト作成
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole()) ||
                        CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                        winner.Add(pc);
                }
            }
            /*EgoistOLD.OverrideCustomWinner();*/

            //廃村時の処理など
            if (endGameResult.GameOverReason == GameOverReason.HumansDisconnect ||
            endGameResult.GameOverReason == GameOverReason.ImpostorDisconnect ||
            CustomWinnerHolder.WinnerTeam == CustomWinner.Draw)
            {
                winner = new List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    winner.Add(p);
                }
            }

            /*TeamEgoist.SoloWin(winner);*/

            ///以降追加勝利陣営 (winnerリセット無し)
            //Opportunist
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.None) break;
                if (pc.Is(CustomRoles.Opportunist) && !pc.Data.IsDead && CustomWinnerHolder.WinnerTeam != CustomWinner.Draw && CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist)
                {
                    winner.Add(pc);
                    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Opportunist);
                }
                //SchrodingerCat
                /*if (SchrodingerCatOLD.CanWinTheCrewmateBeforeChange.GetBool())
                    if (pc.Is(CustomRoles.SchrodingerCat) && CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
                    {
                        winner.Add(pc);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.SchrodingerCat);
                    }*/
            }

            //HideAndSeek専用
            if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek &&
                CustomWinnerHolder.WinnerTeam != CustomWinner.Draw && CustomWinnerHolder.WinnerTeam != CustomWinner.None)
            {
                winner = new();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    var role = TOHPlugin.PlayerStates[pc.PlayerId].MainRole;
                    if (role.GetReduxRole().GetRoleType() == RoleType.Impostor)
                    {
                        if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                            winner.Add(pc);
                    }
                    else if (role.GetReduxRole().GetRoleType() == RoleType.Crewmate)
                    {
                        if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
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
                    else if (role == CustomRoles.HASFox && CustomWinnerHolder.WinnerTeam != CustomWinner.HASTroll && !pc.Data.IsDead)
                    {
                        winner.Add(pc);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.HASFox);
                    }
                }
            }
            TOHPlugin.winnerList = new();
            foreach (var pc in winner)
            {
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;

                TempData.winners.Add(new WinningPlayerData(pc.Data));
                TOHPlugin.winnerList.Add(pc.PlayerId);
            }


            TOHPlugin.VisibleTasksCount = false;
            if (AmongUsClient.Instance.AmHost)
            {
                TOHPlugin.RealOptionsData.Restore(GameOptionsManager.Instance.CurrentGameOptions);
                GameOptionsSender.AllSenders.Clear();
                GameOptionsSender.AllSenders.Add(new NormalGameOptionsSender());
                /* Send SyncSettings RPC */
            }
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch
    {
        public static string LastWinsText = "";

        public static void Postfix(EndGameManager __instance)
        {
            if (!TOHPlugin.playerVersion.ContainsKey(0)) return;
            //#######################################
            //          ==勝利陣営表示==
            //#######################################

            var WinnerTextObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            WinnerTextObject.transform.position = new(__instance.WinText.transform.position.x, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
            WinnerTextObject.transform.localScale = new(0.6f, 0.6f, 0.6f);
            var WinnerText = WinnerTextObject.GetComponent<TMPro.TextMeshPro>(); //WinTextと同じ型のコンポーネントを取得
            WinnerText.fontSizeMin = 3f;
            WinnerText.text = "";

            string CustomWinnerText = "";
            string AdditionalWinnerText = "";
            string CustomWinnerColor = Utils.GetRoleColorCode(CustomRoleManager.Static.Crewmate);

            var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
            if (winnerRole >= 0)
            {
                CustomWinnerText = winnerRole.GetReduxRole().RoleName;
                CustomWinnerColor = winnerRole.GetReduxRole().RoleColor.ToString();
                /*if (winnerRole.IsNeutral())
                {
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
                }*/
            }
            if (AmongUsClient.Instance.AmHost && TOHPlugin.PlayerStates[0].MainRole == CustomRoles.GM)
            {
                __instance.WinText.text = "Game Over";
                __instance.WinText.color = Utils.GetRoleColor(CustomRoleManager.Static.GM);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoleManager.Static.GM);
            }
            switch (CustomWinnerHolder.WinnerTeam)
            {
                //通常勝利
                case CustomWinner.Crewmate:
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoleManager.Static.Engineer);
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
                    WinnerText.text = GetString("ForceEndText");
                    WinnerText.color = Color.gray;
                    break;
                //全滅
                case CustomWinner.None:
                    __instance.WinText.text = "";
                    __instance.WinText.color = Color.black;
                    __instance.BackgroundBar.material.color = Color.gray;
                    WinnerText.text = GetString("EveryoneDied");
                    WinnerText.color = Color.gray;
                    break;
            }

            /*foreach (var additionalWinners in CustomWinnerHolder.AdditionalWinnerTeams)
            {
                var addWinnerRole = (CustomRoles)additionalWinners;
                AdditionalWinnerText += "＆" + Utils.ColorString(Utils.GetRoleColor(addWinnerRole), Utils.GetRoleName(addWinnerRole));
            }*/
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
            {
                WinnerText.text = $"<color={CustomWinnerColor}>{CustomWinnerText}{AdditionalWinnerText}{GetString("Win")}</color>";
            }
            LastWinsText = WinnerText.text.RemoveHtmlTags();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //#######################################
            //           ==最終結果表示==
            //#######################################

            var Pos = Camera.main.ViewportToWorldPoint(new Vector3(0f, 1f, Camera.main.nearClipPlane));
            var RoleSummaryObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            RoleSummaryObject.transform.position = new Vector3(__instance.Navigation.ExitButton.transform.position.x + 0.1f, Pos.y - 0.1f, -14f);
            RoleSummaryObject.transform.localScale = new Vector3(1f, 1f, 1f);

            string RoleSummaryText = $"{GetString("RoleSummaryText")}";
            List<byte> cloneRoles = new(TOHPlugin.PlayerStates.Keys);
            foreach (var id in TOHPlugin.winnerList)
            {
                RoleSummaryText += $"\n<color={CustomWinnerColor}>★</color> " + EndGamePatch.SummaryText[id];
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                RoleSummaryText += $"\n　 " + EndGamePatch.SummaryText[id];
            }
            var RoleSummary = RoleSummaryObject.GetComponent<TMPro.TextMeshPro>();
            RoleSummary.alignment = TMPro.TextAlignmentOptions.TopLeft;
            RoleSummary.color = Color.white;
            RoleSummary.outlineWidth *= 1.2f;
            RoleSummary.fontSizeMin = RoleSummary.fontSizeMax = RoleSummary.fontSize = 1.25f;

            var RoleSummaryRectTransform = RoleSummary.GetComponent<RectTransform>();
            RoleSummaryRectTransform.anchoredPosition = new Vector2(Pos.x + 3.5f, Pos.y - 0.1f);
            RoleSummary.text = RoleSummaryText;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //Utils.ApplySuffix();
        }
    }
}