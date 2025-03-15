using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Templates;
using static TownOfHost.Translator;

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

            //GameStatesのリセット
            GameStates.InGame = false;
            GameStates.InTask = false;

            Logger.Info("-----------ゲーム終了-----------", "Phase");
            if (!GameStates.IsModHost) return;
            SummaryText = new();
            foreach (var id in PlayerState.AllPlayerStates.Keys)
                SummaryText[id] = Utils.SummaryTexts(id, false);

            var sb = new StringBuilder(GetString("KillLog"));
            sb.Append("<size=70%>");
            foreach (var kvp in PlayerState.AllPlayerStates.OrderBy(x => x.Value.RealKiller.Item1.Ticks))
            {
                var date = kvp.Value.RealKiller.Item1;
                if (date == DateTime.MinValue) continue;
                var killerId = kvp.Value.GetRealKiller();
                var targetId = kvp.Key;
                sb.Append($"\n{date:T} {Main.AllPlayerNames[targetId]}({Utils.GetTrueRoleName(targetId, false)}{Utils.GetSubRolesText(targetId)}) [{Utils.GetVitalText(kvp.Key)}]".RemoveHtmlTags());
                if (killerId != byte.MaxValue && killerId != targetId)
                    sb.Append($"\n\t\t⇐ {Main.AllPlayerNames[killerId]}({Utils.GetTrueRoleName(killerId, false)}{Utils.GetSubRolesText(killerId)})".RemoveHtmlTags());
            }
            KillLog = sb.ToString();

            Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
            //winnerListリセット
            EndGameResult.CachedWinners = new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();
            var winner = new List<PlayerControl>();
            foreach (var pc in Main.AllPlayerControls)
            {
                if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) winner.Add(pc);
            }
            foreach (var team in CustomWinnerHolder.WinnerRoles)
            {
                winner.AddRange(Main.AllPlayerControls.Where(p => p.Is(team) && !winner.Contains(p)));
            }

            //HideAndSeek専用
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek &&
                CustomWinnerHolder.WinnerTeam != CustomWinner.Draw && CustomWinnerHolder.WinnerTeam != CustomWinner.None)
            {
                winner = new();
                foreach (var pc in Main.AllPlayerControls)
                {
                    var role = PlayerState.GetByPlayerId(pc.PlayerId).MainRole;
                    if (role.GetCustomRoleTypes() == CustomRoleTypes.Impostor)
                    {
                        if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                            winner.Add(pc);
                    }
                    else if (role.GetCustomRoleTypes() == CustomRoleTypes.Crewmate)
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
                        CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.HASFox);
                    }
                }
            }
            Main.winnerList = new();
            foreach (var pc in winner)
            {
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;

                EndGameResult.CachedWinners.Add(new CachedPlayerData(pc.Data));
                Main.winnerList.Add(pc.PlayerId);
            }

            Main.VisibleTasksCount = false;
            if (AmongUsClient.Instance.AmHost)
            {
                Main.RealOptionsData.Restore(GameOptionsManager.Instance.CurrentGameOptions);
                GameOptionsSender.AllSenders.Clear();
                GameOptionsSender.AllSenders.Add(new NormalGameOptionsSender());
                /* Send SyncSettings RPC */
            }
            //オブジェクト破棄
            CustomRoleManager.Dispose();
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch
    {
        public static string LastWinsText = "";
        private static TextMeshPro roleSummary;
        private static SimpleButton showHideButton;

        public static void Postfix(EndGameManager __instance)
        {
            if (!Main.playerVersion.ContainsKey(0)) return;
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
            var AdditionalWinnerText = new StringBuilder(32);
            string CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Crewmate);

            var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
            if (winnerRole >= 0)
            {
                CustomWinnerText = Utils.GetRoleName(winnerRole);
                CustomWinnerColor = Utils.GetRoleColorCode(winnerRole);
                if (winnerRole.IsNeutral())
                {
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(winnerRole);
                }
            }
            if (AmongUsClient.Instance.AmHost && PlayerState.GetByPlayerId(0).MainRole == CustomRoles.GM)
            {
                __instance.WinText.text = "Game Over";
                __instance.WinText.color = Utils.GetRoleColor(CustomRoles.GM);
                __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.GM);
            }
            switch (CustomWinnerHolder.WinnerTeam)
            {
                //通常勝利
                case CustomWinner.Crewmate:
                    CustomWinnerColor = Utils.GetRoleColorCode(CustomRoles.Engineer);
                    break;
                //特殊勝利
                case CustomWinner.Terrorist:
                    __instance.Foreground.material.color = Color.red;
                    break;
                case CustomWinner.Lovers:
                    __instance.BackgroundBar.material.color = Utils.GetRoleColor(CustomRoles.Lovers);
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

            foreach (var role in CustomWinnerHolder.AdditionalWinnerRoles)
            {
                AdditionalWinnerText.Append('＆').Append(Utils.ColorString(Utils.GetRoleColor(role), Utils.GetRoleName(role)));
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
            {
                WinnerText.text = $"<color={CustomWinnerColor}>{CustomWinnerText}{AdditionalWinnerText}{GetString("Win")}</color>";
            }
            LastWinsText = WinnerText.text.RemoveHtmlTags();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //#######################################
            //           ==最終結果表示==
            //#######################################

            var showInitially = Main.ShowResults.Value;
            showHideButton = new SimpleButton(
               __instance.transform,
               "ShowHideResultsButton",
               new(-4.5f, 2.6f, -14f),  // BackgroundLayer(z=-13)より手前
               new(0, 136, 209, byte.MaxValue),
               new(0, 196, byte.MaxValue, byte.MaxValue),
               () =>
               {
                   var setToActive = !roleSummary.gameObject.activeSelf;
                   roleSummary.gameObject.SetActive(setToActive);
                   Main.ShowResults.Value = setToActive;
                   showHideButton.Label.text = GetString(setToActive ? "HideResults" : "ShowResults");
               },
               GetString(showInitially ? "HideResults" : "ShowResults"))
            {
                Scale = new(1.5f, 0.5f),
                FontSize = 2f,
            };

            StringBuilder sb = new($"{GetString("RoleSummaryText")}");
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);
            foreach (var id in Main.winnerList)
            {
                sb.Append($"\n<color={CustomWinnerColor}>★</color> ").Append(EndGamePatch.SummaryText[id]);
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id]);
            }
            roleSummary = TMPTemplate.Create(
                "RoleSummaryText",
                sb.ToString(),
                Color.white,
                1.25f,
                TextAlignmentOptions.TopLeft,
                setActive: showInitially,
                parent: showHideButton.Button.transform);
            roleSummary.transform.localPosition = new(1.7f, -0.4f, 0f);
            roleSummary.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //Utils.ApplySuffix();
        }
    }
}
