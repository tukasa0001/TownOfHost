using HarmonyLib;
using Hazel;

namespace TownOfHost
{
    //勝利判定処理
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CheckEndCriteria))]
    class CheckGameEndPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            if (!GameData.Instance) return false;
            if (DestroyableSingleton<TutorialManager>.InstanceExists) return true;
            var statistics = new PlayerStatistics(__instance);

            if (CheckAndEndGameForTerminate(__instance)) return false;

            if (Options.NoGameEnd.GetBool()) return false;

            if (CheckAndEndGameForSoloWin(__instance)) return false;
            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Default)
            {
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (CheckAndEndGameForHideAndSeek(__instance, statistics)) return false;
                    if (CheckAndEndGameForTroll(__instance)) return false;
                    if (CheckAndEndGameForTaskWin(__instance)) return false;
                }
                else
                {
                    if (CheckAndEndGameForTaskWin(__instance)) return false;
                    if (CheckAndEndGameForSabotageWin(__instance)) return false;
                    if (CheckAndEndGameForEveryoneDied(__instance, statistics)) return false;
                    if (CheckAndEndGameForImpostorWin(__instance, statistics)) return false;
                    if (CheckAndEndGameForJackalWin(__instance, statistics)) return false;
                    if (CheckAndEndGameForCrewmateWin(__instance, statistics)) return false;
                }
            }
            return false;
        }

        private static bool CheckAndEndGameForSabotageWin(ShipStatus __instance)
        {
            if (__instance.Systems == null) return false;
            ISystemType systemType = __instance.Systems.ContainsKey(SystemTypes.LifeSupp) ? __instance.Systems[SystemTypes.LifeSupp] : null;
            if (systemType != null)
            {
                LifeSuppSystemType lifeSuppSystemType = systemType.TryCast<LifeSuppSystemType>();
                if (lifeSuppSystemType != null && lifeSuppSystemType.Countdown < 0f)
                {
                    EndGameForSabotage(__instance);
                    lifeSuppSystemType.Countdown = 10000f;
                    return true;
                }
            }
            ISystemType systemType2 = __instance.Systems.ContainsKey(SystemTypes.Reactor) ? __instance.Systems[SystemTypes.Reactor] : null;
            if (systemType2 == null)
            {
                systemType2 = __instance.Systems.ContainsKey(SystemTypes.Laboratory) ? __instance.Systems[SystemTypes.Laboratory] : null;
            }
            if (systemType2 != null)
            {
                ICriticalSabotage criticalSystem = systemType2.TryCast<ICriticalSabotage>();
                if (criticalSystem != null && criticalSystem.Countdown < 0f)
                {
                    EndGameForSabotage(__instance);
                    criticalSystem.ClearSabotage();
                    return true;
                }
            }
            return false;
        }

        private static bool CheckAndEndGameForTaskWin(ShipStatus __instance)
        {
            if (Options.DisableTaskWin.GetBool()) return false;
            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.HumansByTask, false);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForEveryoneDied(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TotalAlive <= 0)
            {
                __instance.enabled = false;
                CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                ResetRoleAndEndGame(GameOverReason.ImpostorByKill, true);
                return true;
            }
            return false;
        }
        private static bool CheckAndEndGameForImpostorWin(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TeamImpostorsAlive >= statistics.TotalAlive - statistics.TeamImpostorsAlive &&
                statistics.TeamJackalAlive <= 0)
            {
                if (Options.IsStandardHAS && statistics.TotalAlive - statistics.TeamImpostorsAlive != 0) return false;
                __instance.enabled = false;
                var endReason = TempData.LastDeathReason switch
                {
                    DeathReason.Exile => GameOverReason.ImpostorByVote,
                    DeathReason.Kill => GameOverReason.ImpostorByKill,
                    _ => GameOverReason.ImpostorByVote,
                };
                ResetRoleAndEndGame(endReason, false);
                return true;
            }
            return false;
        }
        private static bool CheckAndEndGameForJackalWin(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TeamJackalAlive >= statistics.TotalAlive - statistics.TeamJackalAlive &&
                statistics.TeamImpostorsAlive <= 0)
            {
                if (Options.IsStandardHAS && statistics.TotalAlive - statistics.TeamJackalAlive != 0) return false;
                __instance.enabled = false;
                var endReason = TempData.LastDeathReason switch
                {
                    DeathReason.Exile => GameOverReason.ImpostorByVote,
                    DeathReason.Kill => GameOverReason.ImpostorByKill,
                    _ => GameOverReason.ImpostorByVote,
                };

                CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JSchrodingerCat);
                ResetRoleAndEndGame(endReason, true);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForCrewmateWin(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TeamImpostorsAlive == 0 && statistics.TeamJackalAlive == 0)
            {
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.HumansByVote, false);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForHideAndSeek(ShipStatus __instance, PlayerStatistics statistics)
        {
            if (statistics.TotalAlive - statistics.TeamImpostorsAlive == 0)
            {
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.ImpostorByKill, false);
                return true;
            }
            return false;
        }

        private static bool CheckAndEndGameForTroll(ShipStatus __instance)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var hasRole = Main.AllPlayerCustomRoles.TryGetValue(pc.PlayerId, out var role);
                if (!hasRole) return false;
                if (role == CustomRoles.HASTroll && pc.Data.IsDead)
                {
                    CustomWinnerHolder.WinnerTeam = CustomWinner.HASTroll;
                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    __instance.enabled = false;
                    ResetRoleAndEndGame(GameOverReason.ImpostorByKill, true);
                    return true;
                }
            }
            return false;
        }

        private static bool CheckAndEndGameForTerminate(ShipStatus __instance)
        {
            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Draw)
            {
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.ImpostorByKill, false);
                return true;
            }
            return false;
        }
        private static bool CheckAndEndGameForSoloWin(ShipStatus __instance)
        {
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                __instance.enabled = false;
                ResetRoleAndEndGame(GameOverReason.ImpostorByKill, true);
                return true;
            }
            return false;
        }


        private static void EndGameForSabotage(ShipStatus __instance)
        {
            __instance.enabled = false;
            ResetRoleAndEndGame(GameOverReason.ImpostorBySabotage, false);
            return;
        }
        private static void ResetRoleAndEndGame(GameOverReason reason, bool SetImpostorsToGA, bool showAd = false)
        {
            var sender = new CustomRpcSender("EndGameSender", SendOption.Reliable, true);
            sender.StartMessage(-1); // 5:GameData

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                var LoseImpostorRole = Main.AliveImpostorCount == 0 ? pc.Is(RoleType.Impostor) : pc.Is(CustomRoles.Egoist);
                if ((SetImpostorsToGA && pc.Data.Role.IsImpostor) || //インポスター:引数による
                    pc.Is(CustomRoles.Sheriff) || //シェリフ:無条件
                    (!(CustomWinnerHolder.WinnerTeam == CustomWinner.Arsonist) && pc.Is(CustomRoles.Arsonist)) || //アーソニスト:敗北
                    (CustomWinnerHolder.WinnerTeam != CustomWinner.Jackal && pc.Is(CustomRoles.Jackal)) || //ジャッカル:敗北
                    LoseImpostorRole
                )
                {
                    sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                            .Write((ushort)RoleTypes.GuardianAngel)
                            .EndRpc();
                    pc.SetRole(RoleTypes.GuardianAngel); //ホスト用
                }
            }

            // CustomWinnerHolderの情報送信
            sender.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame);
            CustomWinnerHolder.WriteTo(sender.stream);
            sender.EndRpc();
            sender.EndMessage();

            // AmongUs側のゲーム終了RPC
            MessageWriter writer = sender.stream;
            writer.StartMessage(8);
            {
                writer.Write(AmongUsClient.Instance.GameId); //ここまでStartEndGameの内容
                writer.Write((byte)reason);
                writer.Write(showAd);
            }
            writer.EndMessage();

            sender.SendMessage();
        }
        //プレイヤー統計
        internal class PlayerStatistics
        {
            public int TeamImpostorsAlive { get; set; }
            public int TotalAlive { get; set; }
            public int TeamJackalAlive { get; set; }

            public PlayerStatistics(ShipStatus __instance)
            {
                GetPlayerCounts();
            }

            private void GetPlayerCounts()
            {
                int numImpostorsAlive = 0;
                int numTotalAlive = 0;
                int numJackalsAlive = 0;

                for (int i = 0; i < GameData.Instance.PlayerCount; i++)
                {
                    GameData.PlayerInfo playerInfo = GameData.Instance.AllPlayers[i];
                    var hasHideAndSeekRole = Main.AllPlayerCustomRoles.TryGetValue((byte)i, out var role);
                    if (!playerInfo.Disconnected)
                    {
                        if (!playerInfo.IsDead)
                        {
                            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek || !hasHideAndSeekRole)
                            {
                                numTotalAlive++;//HideAndSeek以外
                            }
                            else
                            {
                                //HideAndSeek中
                                if (role is not CustomRoles.HASFox and not CustomRoles.HASTroll) numTotalAlive++;
                            }

                            if (playerInfo.Role.TeamType == RoleTeamTypes.Impostor &&
                            (playerInfo.GetCustomRole() != CustomRoles.Sheriff || playerInfo.GetCustomRole() != CustomRoles.Arsonist))
                            {
                                numImpostorsAlive++;
                            }
                            else if (playerInfo.GetCustomRole() == CustomRoles.Jackal) numJackalsAlive++;
                        }
                    }
                }

                TeamImpostorsAlive = numImpostorsAlive;
                TotalAlive = numTotalAlive;
                TeamJackalAlive = numJackalsAlive;
            }
        }
    }
}