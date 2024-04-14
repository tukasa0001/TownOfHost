using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.Crewmate;
using TownOfHostForE.Roles.Neutral;
using TownOfHostForE.Roles.Animals;
using TownOfHostForE.Patches;

namespace TownOfHostForE
{
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    class GameEndChecker
    {
        private static GameEndPredicate predicate;
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            //ゲーム終了判定済みなら中断
            if (predicate == null) return false;

            //ゲーム終了しないモードで廃村以外の場合は中断
            if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam != CustomWinner.Draw) return false;

            //廃村用に初期値を設定
            var reason = GameOverReason.ImpostorByKill;

            //ゲーム終了判定
            predicate.CheckForEndGame(out reason);

            //ゲーム終了時
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                //カモフラージュ強制解除
                Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true));

                switch (CustomWinnerHolder.WinnerTeam)
                {
                    case CustomWinner.Crewmate:
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if(pc.Is(CustomRoleTypes.Crewmate) && !pc.Is(CustomRoles.Lovers)
                                && !(pc.Is(CustomRoles.Bakery) && Bakery.IsNeutral(pc)) && !pc.Is(CustomRoles.Archenemy))
                            {
                                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            }
                        }
                        break;
                    case CustomWinner.Impostor:
                        if (Egoist.CheckWin()) break;

                        Main.AllPlayerControls
                            .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Archenemy))
                            .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                        break;
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
                {
                    List<PlayerControl> winnerLoversList = null;
                    if(CheckLoversWin(reason,ref winnerLoversList))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                        //Main.AllPlayerControls
                        winnerLoversList
                            .Where(p => p.Is(CustomRoles.Lovers) && p.IsAlive())
                            .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    }
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoles.DarkHide) && !pc.Data.IsDead
                            && ((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorBySabotage)) ||
                                 CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide
                                ||
                                (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.HumansByTask) && ((DarkHide)pc.GetRoleClass()).IsWinKill == true)))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DarkHide);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        else if (pc.Is(CustomRoles.Bakery) && Bakery.IsNeutral(pc) && pc.IsAlive()
                            && ((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorBySabotage)) || CustomWinnerHolder.WinnerTeam == CustomWinner.NBakery
                            || (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.HumansByTask))))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.NBakery);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                        else if (pc.Is(CustomRoles.Tuna) && pc.IsAlive())
                        {
                            //マグロ側で処理
                            Tuna.CheckAliveWin(pc);
                        }
                    }

                    List<PlayerControl> shPC = new ();
                    //追加勝利陣営
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        //Lover追加勝利
                        //if (pc.Is(CustomRoles.Lovers) && pc.IsAlive()
                        //    && (Options.LoversAddWin.GetBool() || PlatonicLover.AddWin))
                        if(CheckLoversAddWin(pc,winnerLoversList))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Lovers);
                        }
                        //シュレ猫は後でもう一度
                        if (pc.GetCustomRole() == CustomRoles.SchrodingerCat)
                        {
                            shPC.Add(pc);
                        }
                        else if (pc.GetRoleClass() is IAdditionalWinner additionalWinner)
                        {
                            var winnerRole = pc.GetCustomRole();
                            //bool result = additionalWinner.CheckWin(out var winnerType);
                            bool result = additionalWinner.CheckWin(ref winnerRole);
                            if (result)
                            {
                                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                CustomWinnerHolder.AdditionalWinnerRoles.Add(winnerRole);
                            }
                        }
                        if (Duelist.ArchenemyCheckWin(pc))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Archenemy);
                        }
                    }
                    //弁護士且つ追跡者
                    Lawyer.EndGameCheck();
                    //シュレ猫用
                    foreach (var sh in shPC)
                    {
                        if (sh.GetRoleClass() is IAdditionalWinner additionalWinner)
                        {
                            var winnerRole = sh.GetCustomRole();
                            bool result = additionalWinner.CheckWin(ref winnerRole);
                            if (result)
                            {
                                CustomWinnerHolder.WinnerIds.Add(sh.PlayerId);
                                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.SchrodingerCat);
                            }
                        }
                    }
                }
                ShipStatus.Instance.enabled = false;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }

        private static bool CheckLoversWin(GameOverReason reason, ref List<PlayerControl> winnerLoversList)
        {
            winnerLoversList = new();
            //ラバーズがいないなら処理しない
            if (Main.LoversPlayersV2 == null || Main.LoversPlayersV2.Count() == 0)return false;

            byte winnerTeamLeaderId = CheckWinnerLoversLeaderID();

            //ラバーズの生き残りがいない場合
            if (winnerTeamLeaderId == byte.MaxValue) return false;

            //生き残りの1チームなんで選択
            foreach (var id in Main.LoversPlayersV2[winnerTeamLeaderId])
            {
                winnerLoversList.Add(Utils.GetPlayerById(id));
            }

            //そのチームがラバーズ若しくは純愛者で、追加勝利ありだとここでは処理しない。
            if (Utils.GetPlayerById(winnerTeamLeaderId).GetCustomRole() != CustomRoles.OtakuPrincess
                &&
                ((Utils.GetPlayerById(winnerTeamLeaderId).GetCustomRole() == CustomRoles.PlatonicLover &&
                 PlatonicLover.AddWin)
                 ||
                 (Utils.GetPlayerById(winnerTeamLeaderId).GetCustomSubRoles().All(p => p != CustomRoles.Lovers) == false &&
                 Options.LoversAddWin.GetBool()))
                )
            {
                return false;
            }

            return true;
        }

        private static byte CheckWinnerLoversLeaderID()
        {
            Dictionary<byte,int> countLovers = new ();

            //生きてる奴確認 登録順で行う
            foreach (var id in Main.isLoversLeaders)
            {
                //trueなら死んでる
                if (Main.isLoversDeadV2[id]) continue;
                //生きているなら人数を記録
                countLovers.Add(id, Main.LoversPlayersV2[id].Count());
            }

            int maxCount = -1;
            byte Leader = byte.MaxValue;
            List<byte> drrowCount = new();
            //生き残り精査
            foreach (var data in countLovers)
            {
                //相手の人数より多い場合
                if (data.Value > maxCount)
                {
                    maxCount = data.Value;
                    Leader = data.Key;
                }
                //相手の人数と等しい場合
                else if (data.Value == maxCount)
                {
                    drrowCount.Add(data.Key);
                    if(!drrowCount.Contains(Leader)) drrowCount.Add(Leader);
                }
            }

            //最終確認
            //同じ数のラバーズが勝利条件を満たしているとき
            if (drrowCount.Count() > 0)
            {
                //一番最初に登録されてる奴が勝者

                //なんか0指定で一番最後のラバーズが取れるから取り敢えず一番最後のラバーズを取る
                Leader = drrowCount[drrowCount.Count() -1];
            }

            return Leader;
        }

        private static bool CheckLoversAddWin(PlayerControl pc,List<PlayerControl> winnerLoversList)
        {
            //死んでるのは対象外
            if (!pc.IsAlive()) return false;
            //対象がいないなら対象外
            if (winnerLoversList.Any(p => p.PlayerId == pc.PlayerId) == false) return false;

            //欲しいのは代表のロール
            var cRole = winnerLoversList[0].GetCustomRole();
            switch (cRole)
            {
                case CustomRoles.PlatonicLover:
                    return PlatonicLover.AddWin;
                case CustomRoles.Lovers:
                default:
                    //代表のロールが純愛者以外であればラバーズの追加勝利から取得
                    //(姫ちゃんには追加はないので)
                    return Options.LoversAddWin.GetBool();
            }
        }

        public static void StartEndGame(GameOverReason reason)
        {
            AmongUsClient.Instance.StartCoroutine(CoEndGame(AmongUsClient.Instance, reason).WrapToIl2Cpp());
        }

        private static IEnumerator CoEndGame(AmongUsClient self, GameOverReason reason)
        {
            //ペットを強制的につけた人は外す
            PetSettings.RemovePetSet();


            // サーバー側のパケットサイズ制限によりCustomRpcSenderが利用できないため，遅延を挟むことで順番の整合性を保つ．

            //ゴーストロール化
            // バニラ画面でのアウトロを正しくするためのゴーストロール化
            List<byte> ReviveRequiredPlayerIds = new();
            var winner = CustomWinnerHolder.WinnerTeam;
            foreach (var pc in Main.AllPlayerControls)
            {
                if (winner == CustomWinner.Draw)
                {
                    SetGhostRole(ToGhostImpostor: true);
                    continue;
                }
                bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                        CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
                bool isCrewmateWin = reason.Equals(GameOverReason.HumansByVote) || reason.Equals(GameOverReason.HumansByTask);
                SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);

                void SetGhostRole(bool ToGhostImpostor)
                {
                    if (!pc.Data.IsDead) ReviveRequiredPlayerIds.Add(pc.PlayerId);
                    if (ToGhostImpostor)
                    {
                        Logger.Info($"{pc.GetNameWithRole()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRole(RoleTypes.ImpostorGhost);
                    }
                    else
                    {
                        Logger.Info($"{pc.GetNameWithRole()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRole(RoleTypes.CrewmateGhost);
                    }
                }
            }

            // CustomWinnerHolderの情報の同期
            var winnerWriter = self.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, SendOption.Reliable);
            CustomWinnerHolder.WriteTo(winnerWriter);
            self.FinishRpcImmediately(winnerWriter);

            // 蘇生を確実にゴーストロール設定の後に届けるための遅延
            yield return new WaitForSeconds(EndGameDelay);

            if (ReviveRequiredPlayerIds.Count > 0)
            {
                // 蘇生 パケットが膨れ上がって死ぬのを防ぐため，1送信につき1人ずつ蘇生する
                for (int i = 0; i < ReviveRequiredPlayerIds.Count; i++)
                {
                    var playerId = ReviveRequiredPlayerIds[i];
                    var playerInfo = GameData.Instance.GetPlayerById(playerId);
                    // 蘇生
                    playerInfo.IsDead = false;
                    // 送信
                    GameData.Instance.SetDirtyBit(0b_1u << playerId);
                    AmongUsClient.Instance.SendAllStreamedObjects();
                }
                // ゲーム終了を確実に最後に届けるための遅延
                yield return new WaitForSeconds(EndGameDelay);
            }
            // ゲーム終了
            GameManager.Instance.RpcEndGame(reason, false);
        }
        private const float EndGameDelay = 0.2f;

        public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
        public static void SetPredicateToHideAndSeek() => predicate = new HideAndSeekGameEndPredicate();
        public static void SetPredicateToSuperBombParty() => predicate = new SuperBombPartyGameEndPredicate();

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

                int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
                int Jackal = Utils.AlivePlayersCount(CountTypes.Jackal);
                int Animals = Utils.AlivePlayersCount(CountTypes.Animals);
                int Crew = Utils.AlivePlayersCount(CountTypes.Crew);

                if (Imp == 0 && Crew == 0 && Jackal == 0 && Animals == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers))) //ラバーズ勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                }
                else if (Jackal == 0 && Animals == 0 && Crew <= Imp) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                }
                else if (Imp == 0 && Animals == 0 && Crew <= Jackal) //ジャッカル勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JClient);
                }
                else if (Imp == 0 && Jackal == 0 && Crew <= Animals) //アニマルズ勝利
                {
                    reason = GameOverReason.ImpostorByKill;
                    Vulture.AnimalsWin();
                }
                else if (Jackal == 0 && Imp == 0 && Animals == 0) //クルー勝利
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

                int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
                int Crew = Utils.AlivePlayersCount(CountTypes.Crew);

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

        // 大惨事爆裂大戦用
        class SuperBombPartyGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

                if (CheckGameEndByLivingPlayers(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorByKill;

                int AliveSB = Utils.AlivePlayersCount(CountTypes.SB);

                if (AliveSB == 0) //全滅
                {
                    reason = GameOverReason.ImpostorByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (AliveSB == 1) //勝者決定
                {
                    reason = GameOverReason.ImpostorByKill;
                    foreach (var AlivePlayer in Main.AllAlivePlayerControls)
                    {
                        CustomWinnerHolder.WinnerIds.Add(AlivePlayer.PlayerId);
                    }
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

        /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndByTask(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (Options.DisableTaskWin.GetBool() || TaskState.InitialTotalTasks == 0) return false;
            //ラバーズ変化などでトータルタスクが0になった際はゲームを終えない
            if (GameData.Instance.TotalTasks == 0) return false;

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
            else if (systems.ContainsKey(SystemTypes.HeliSabotage)) sys = systems[SystemTypes.HeliSabotage];

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