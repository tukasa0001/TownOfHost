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
            //winnerListリセット
            TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
            main.additionalwinners = new HashSet<AdditionalWinners>();
            var winner = new List<PlayerControl>();
            //勝者リスト作成
            if (TempData.DidHumansWin(endGameResult.GameOverReason))
            {
                if (main.currentWinner == CustomWinner.Default)
                {
                    main.currentWinner = CustomWinner.Crewmate;
                }
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    CustomRoles role = p.getCustomRole();
                    IntroTypes introType = role.getIntroType();
                    bool canWin = introType == IntroTypes.Crewmate;
                    if (canWin) winner.Add(p);
                }
            }
            if (TempData.DidImpostorsWin(endGameResult.GameOverReason))
            {
                if (main.currentWinner == CustomWinner.Default)
                {
                    main.currentWinner = CustomWinner.Impostor;
                }
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    CustomRoles role = p.getCustomRole();
                    IntroTypes introType = role.getIntroType();
                    bool canWin = introType == IntroTypes.Impostor || introType == IntroTypes.Madmate;
                    if (canWin) winner.Add(p);
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
            //Opportunist
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.isOpportunist() && !pc.Data.IsDead && main.currentWinner != CustomWinner.Draw && main.currentWinner != CustomWinner.Terrorist)
                {
                    TempData.winners.Add(new WinningPlayerData(pc.Data));
                    winner.Add(pc);
                    main.additionalwinners.Add(AdditionalWinners.Opportunist);
                }
            }

            //HideAndSeek専用
            if (Options.IsHideAndSeek && main.currentWinner != CustomWinner.Draw)
            {
                var winners = new List<PlayerControl>();
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    var hasRole = main.AllPlayerCustomRoles.TryGetValue(pc.PlayerId, out var role);
                    if (!hasRole) continue;
                    if (role == CustomRoles.Crewmate)
                    {
                        if (pc.Data.Role.IsImpostor && TempData.DidImpostorsWin(endGameResult.GameOverReason))
                            winners.Add(pc);
                        if (!pc.Data.Role.IsImpostor && TempData.DidHumansWin(endGameResult.GameOverReason))
                            winners.Add(pc);
                    }
                    if (role == CustomRoles.Fox && !pc.Data.IsDead)
                    {
                        winners.Add(pc);
                        main.additionalwinners.Add(AdditionalWinners.Fox);
                    }
                    if (role == CustomRoles.Troll && pc.Data.IsDead)
                    {
                        main.currentWinner = CustomWinner.Troll;
                        winners = new List<PlayerControl>();
                        winners.Add(pc);
                        break;
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
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch
    {
        public static void Postfix(EndGameManager __instance)
        {
            // Additional code
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
                    CustomWinnerText = $"{Utils.getRoleName(CustomRoles.Impostor)}";
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Impostor);
                    break;
                case CustomWinner.Crewmate:
                    CustomWinnerText = $"{Utils.getRoleName(CustomRoles.Crewmate)}";
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Crewmate);
                    break;
                //特殊勝利
                case CustomWinner.Jester:
                    __instance.BackgroundBar.material.color = Utils.getRoleColor(CustomRoles.Jester);
                    CustomWinnerText = $"{Utils.getRoleName(CustomRoles.Jester)}";
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Jester);
                    break;
                case CustomWinner.Terrorist:
                    __instance.Foreground.material.color = Color.red;
                    __instance.BackgroundBar.material.color = Color.green;
                    CustomWinnerText = $"{Utils.getRoleName(CustomRoles.Terrorist)}";
                    CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Terrorist);
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
                {
                    AdditionalWinnerText += $"＆<color={Utils.getRoleColorCode(CustomRoles.Opportunist)}>{Utils.getRoleName(CustomRoles.Opportunist)}</color>";
                }
                if (main.additionalwinners.Contains(AdditionalWinners.Fox))
                {
                    AdditionalWinnerText += $"＆<color={Utils.getRoleColorCode(CustomRoles.Fox)}>{Utils.getRoleName(CustomRoles.Fox)}</color>";
                }
            }
            if (Options.IsHideAndSeek)
            {
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.Data.IsDead)
                    {
                        var hasRole = main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var role);
                        if (hasRole && role == CustomRoles.Troll)
                        {
                            __instance.BackgroundBar.material.color = Color.green;
                            CustomWinnerText = $"{Utils.getRoleName(CustomRoles.Troll)}";
                            CustomWinnerColor = Utils.getRoleColorCode(CustomRoles.Troll);
                        }
                    }
                }
            }
            if (main.currentWinner != CustomWinner.Draw)
            {
                textRenderer.text = $"<color={CustomWinnerColor}>{CustomWinnerText}{AdditionalWinnerText}{getString("Win")}</color>";
            }
            main.BountyTimer = new Dictionary<byte, float>();
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            main.SerialKillerTimer = new Dictionary<byte, float>();

            NameColorManager.Instance.RpcReset();
            main.VisibleTasksCount = false;
            if (AmongUsClient.Instance.AmHost)
            {
                PlayerControl.LocalPlayer.RpcSyncSettings(main.RealOptionsData);
            }
            //Utils.ApplySuffix();
        }
    }
}
