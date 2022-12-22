using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using AmongUs.GameOptions;

namespace TownOfHost
{
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    class GameEndChecker
    {
        private static GameEndPredicate predicate;
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam != CustomWinner.Draw) return false;

            GameOverReason reason = GameOverReason.ImpostorByKill;

            if (predicate != null && predicate.CheckForEndGame(out var r)) reason = r;

            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                switch (CustomWinnerHolder.WinnerTeam)
                {
                    case CustomWinner.Crewmate:
                        PlayerControl.AllPlayerControls.ToArray()
                            .Where(pc => pc.Is(RoleType.Crewmate) && !pc.Is(CustomRoles.Lovers))
                            .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                    case CustomWinner.Impostor:
                        PlayerControl.AllPlayerControls.ToArray()
                                .Where(pc => (pc.Is(RoleType.Impostor) || pc.Is(RoleType.Madmate)) && !pc.Is(CustomRoles.Lovers))
                                .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                }
                ShipStatus.Instance.enabled = false;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }
        public static void StartEndGame(GameOverReason reason)
        {
            var sender = new CustomRpcSender("EndGameSender", SendOption.Reliable, true);
            sender.StartMessage(-1); // 5: GameData
            MessageWriter writer = sender.stream;

            //ゴーストロール化
            List<byte> ReviveReqiredPlayerIds = new();
            var winner = CustomWinnerHolder.WinnerTeam;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (winner == CustomWinner.Draw)
                {
                    SetGhostRole(ToGhostImpostor: true);
                    continue;
                }
                if (winner == CustomWinner.Impostor)
                {
                    switch (pc.GetCustomRole().GetRoleType())
                    {
                        case RoleType.Impostor:
                        case RoleType.Madmate:
                            SetGhostRole(ToGhostImpostor: true);
                            break;
                        case RoleType.Neutral:
                        case RoleType.Crewmate:
                            SetGhostRole(ToGhostImpostor: false);
                            break;
                    }
                }
                else if (winner == CustomWinner.Crewmate)
                {
                    switch (pc.GetCustomRole().GetRoleType())
                    {
                        case RoleType.Impostor:
                        case RoleType.Madmate:
                        case RoleType.Neutral:
                            SetGhostRole(ToGhostImpostor: true);
                            break;
                        case RoleType.Crewmate:
                            SetGhostRole(ToGhostImpostor: false);
                            break;
                    }
                }
                else if (((CustomRoles)winner).IsNeutral())
                {
                    if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                        CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole()))
                    {
                        SetGhostRole(ToGhostImpostor: true);
                    }
                    else SetGhostRole(ToGhostImpostor: false);
                }
                void SetGhostRole(bool ToGhostImpostor)
                {
                    if (!pc.Data.IsDead) ReviveReqiredPlayerIds.Add(pc.PlayerId);
                    if (ToGhostImpostor)
                    {
                        Logger.Info($"{pc.GetNameWithRole()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                        sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                            .Write((ushort)RoleTypes.ImpostorGhost)
                            .EndRpc();
                        pc.SetRole(RoleTypes.ImpostorGhost);
                    }
                    else
                    {
                        Logger.Info($"{pc.GetNameWithRole()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                        sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                            .Write((ushort)RoleTypes.CrewmateGhost)
                            .EndRpc();
                        pc.SetRole(RoleTypes.Crewmate);
                    }
                }
            }

            // CustomWinnerHolderの情報の同期
            sender.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame);
            CustomWinnerHolder.WriteTo(sender.stream);
            sender.EndRpc();

            // GameDataによる蘇生処理
            writer.StartMessage(1); // Data
            {
                writer.WritePacked(GameData.Instance.NetId); // NetId
                foreach (var info in GameData.Instance.AllPlayers)
                {
                    if (ReviveReqiredPlayerIds.Contains(info.PlayerId))
                    {
                        // 蘇生&メッセージ書き込み
                        info.IsDead = false;
                        writer.StartMessage(info.PlayerId);
                        info.Serialize(writer);
                        writer.EndMessage();
                    }
                }
                writer.EndMessage();
            }

            sender.EndMessage();

            // バニラ側のゲーム終了RPC
            writer.StartMessage(8); //8: EndGame
            {
                writer.Write(AmongUsClient.Instance.GameId); //GameId
                writer.Write((byte)reason); //GameoverReason
                writer.Write(false); //showAd
            }
            writer.EndMessage();

            sender.SendMessage();
        }

        public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
        public static void SetPredicateToHideAndSeek() => predicate = new HideAndSeekGameEndPredicate();

        // ===== ゲーム終了条件 =====
        // 通常ゲーム用
        class NormalGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;
                if (CheckGameEndBySabotage(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int[] counts = CountLivingPlayersByPredicates(
                    pc => pc.Is(RoleType.Impostor) || pc.Is(CustomRoles.Egoist), //インポスター
                    pc => pc.Is(CustomRoles.Jackal), //ジャッカル
                    pc => !pc.Is(RoleType.Impostor) && !pc.Is(CustomRoles.Egoist) && !pc.Is(CustomRoles.Jackal) //その他
                );
                int Imp = counts[0], Jackal = counts[1], Crew = counts[2];


                if (Imp == 0 && Crew == 0 && Jackal == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Jackal == 0 && Crew <= Imp) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0 && Crew <= Jackal) //ジャッカル勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JSchrodingerCat);
                }
                else if (Jackal == 0 && Imp == 0) //クルー勝利
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }

        // HideAndSeek用
        class HideAndSeekGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int[] counts = CountLivingPlayersByPredicates(
                    pc => pc.Is(RoleType.Impostor), //インポスター
                    pc => pc.Is(RoleType.Crewmate) //クルー(Troll,Fox除く)
                );
                int Imp = counts[0], Crew = counts[1];


                if (Imp == 0 && Crew == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Crew <= 0) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0) //クルー勝利(インポスター切断など)
                {
                    reason = GameOverReason.HumansByVote;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }
    }

    public abstract class GameEndPredicate
    {
        /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
        /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
        /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
        public abstract bool CheckForEndGame(out GameOverReason reason);

        /// <summary>各条件に合ったプレイヤーの人数を取得し、配列に同順で格納します。</summary>
        public int[] CountLivingPlayersByPredicates(params Predicate<PlayerControl>[] predicates)
        {
            int[] counts = new int[predicates.Length];
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                for (int i = 0; i < predicates.Length; i++)
                {
                    if (pc.IsAlive() && predicates[i](pc)) counts[i]++;
                }
            }
            return counts;
        }


        /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndByTask(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (Options.DisableTaskWin.GetBool()) return false;

            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                reason = GameOverReason.HumansByTask;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
                return true;
            }
            return false;
        }
        /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (ShipStatus.Instance.Systems == null) return false;

            // TryGetValueは使用不可
            var systems = ShipStatus.Instance.Systems;
            LifeSuppSystemType LifeSupp;
            if (systems.ContainsKey(SystemTypes.LifeSupp) && // サボタージュ存在確認
                (LifeSupp = systems[SystemTypes.LifeSupp].TryCast<LifeSuppSystemType>()) != null && // キャスト可能確認
                LifeSupp.Countdown < 0f) // タイムアップ確認
            {
                // 酸素サボタージュ
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                reason = GameOverReason.ImpostorBySabotage;
                LifeSupp.Countdown = 10000f;
                return true;
            }

            ISystemType sys = null;
            if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
            else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];

            ICriticalSabotage critical;
            if (sys != null && // サボタージュ存在確認
                (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
                critical.Countdown < 0f) // タイムアップ確認
            {
                // リアクターサボタージュ
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                reason = GameOverReason.ImpostorBySabotage;
                critical.ClearSabotage();
                return true;
            }

            return false;
        }
    }
}