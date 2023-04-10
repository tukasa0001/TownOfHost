using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using static TOHE.Translator;

namespace TOHE;

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
        if (Options.NoGameEnd.GetBool() && CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.Error) return false;

        //廃村用に初期値を設定
        var reason = GameOverReason.ImpostorByKill;

        //ゲーム終了判定
        predicate.CheckForEndGame(out reason);

        // SoloKombat
        if (Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            if (CustomWinnerHolder.WinnerIds.Count > 0 || CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
            {
                ShipStatus.Instance.enabled = false;
                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }

        //ゲーム終了時
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default)
        {
            //カモフラージュ強制解除
            Main.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true));

            switch (CustomWinnerHolder.WinnerTeam)
            {
                case CustomWinner.Crewmate:
                    Main.AllPlayerControls
                        .Where(pc => pc.Is(CustomRoleTypes.Crewmate) && !pc.Is(CustomRoles.Lovers) && !pc.Is(CustomRoles.Madmate))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
                case CustomWinner.Impostor:
                    Main.AllPlayerControls
                        .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoles.Madmate)) && !pc.Is(CustomRoles.Lovers))
                        .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                    break;
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None and not CustomWinner.Error)
            {

                //潜藏者抢夺胜利
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Is(CustomRoles.DarkHide) && !pc.Data.IsDead
                        && ((CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor && !reason.Equals(GameOverReason.ImpostorBySabotage)) || CustomWinnerHolder.WinnerTeam == CustomWinner.DarkHide
                        || (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && !reason.Equals(GameOverReason.HumansByTask) && DarkHide.IsWinKill[pc.PlayerId] == true)))
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.DarkHide);
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                    }
                }

                //利己主义者抢夺胜利（船员）
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
                {
                    foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsCrewmate()))
                        if (pc.Is(CustomRoles.Egoist))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                }

                //利己主义者抢夺胜利（内鬼）
                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                {
                    foreach (var pc in Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsImpostor()))
                        if (pc.Is(CustomRoles.Egoist))
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        }
                }

                //神抢夺胜利
                if (CustomRolesHelper.RoleExist(CustomRoles.God))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.God);
                    Main.AllPlayerControls
                        .Where(p => p.Is(CustomRoles.God) && p.IsAlive())
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                }

                //恋人抢夺胜利
                else if (CustomRolesHelper.RoleExist(CustomRoles.Lovers) && !reason.Equals(GameOverReason.HumansByTask))
                {
                    if (!(!Main.LoversPlayers.ToArray().All(p => p.IsAlive()) && Options.LoverSuicide.GetBool()))
                    {
                        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.Impostor or CustomWinner.Jackal or CustomWinner.Pelican)
                        {
                            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
                            Main.AllPlayerControls
                                .Where(p => p.Is(CustomRoles.Lovers))
                                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                        }
                    }
                }

                //追加胜利
                foreach (var pc in Main.AllPlayerControls)
                {
                    //Opportunist
                    if (pc.Is(CustomRoles.Opportunist) && pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Opportunist);
                    }
                    //Sunnyboy
                    if (pc.Is(CustomRoles.Sunnyboy) && !pc.IsAlive())
                    {
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Sunnyboy);
                    }
                    //自爆卡车来咯
                    if (pc.Is(CustomRoles.Provocateur) && Main.Provoked.TryGetValue(pc.PlayerId, out var tar))
                    {
                        if (!CustomWinnerHolder.WinnerIds.Contains(tar))
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Provocateur);
                        }
                    }
                }

                //Lovers follow winner
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Lovers and not CustomWinner.Crewmate and not CustomWinner.Impostor)
                {
                    foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Lovers)))
                    {
                        if (CustomWinnerHolder.WinnerIds.Where(x => Utils.GetPlayerById(x).Is(CustomRoles.Lovers)).Count() > 0)
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lovers);
                        }
                    }
                }

                //FFF
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Lovers && !CustomWinnerHolder.AdditionalWinnerTeams.Contains(AdditionalWinners.Lovers) && !CustomRolesHelper.RoleExist(CustomRoles.Lovers) && !CustomRolesHelper.RoleExist(CustomRoles.Ntr))
                {
                    foreach (var pc in Main.AllPlayerControls.Where(x => x.Is(CustomRoles.FFF)))
                    {
                        if (Main.AllPlayerControls.Where(x => (x.Is(CustomRoles.Lovers) || x.Is(CustomRoles.Ntr)) && (x.GetRealKiller() == null ? -1 : x.GetRealKiller().PlayerId) == pc.PlayerId).Count() > 0)
                        {
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                            CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.FFF);
                        }
                    }
                }

                //中立共同胜利
                if (Options.NeutralWinTogether.GetBool() && CustomWinnerHolder.WinnerIds.Where(x => Utils.GetPlayerById(x) != null && Utils.GetPlayerById(x).GetCustomRole().IsNeutral()).Count() >= 1)
                {
                    foreach (var pc in Main.AllPlayerControls)
                        if (pc.GetCustomRole().IsNeutral() && !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId))
                            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }
                else if (Options.NeutralRoleWinTogether.GetBool())
                {
                    foreach (var id in CustomWinnerHolder.WinnerIds)
                    {
                        var pc = Utils.GetPlayerById(id);
                        if (pc == null || !pc.GetCustomRole().IsNeutral()) continue;
                        foreach (var tar in Main.AllPlayerControls)
                            if (!CustomWinnerHolder.WinnerIds.Contains(tar.PlayerId) && tar.GetCustomRole() == pc.GetCustomRole())
                                CustomWinnerHolder.WinnerIds.Add(tar.PlayerId);
                    }
                }
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
            SetEverythingUpPatch.LastWinsReason = winner is CustomWinner.Crewmate or CustomWinner.Impostor ? GetString($"GameOverReason.{reason}") : "";
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
                if (ReviveRequiredPlayerIds.Contains(info.PlayerId))
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
    public static void SetPredicateToSoloKombat() => predicate = new SoloKombatGameEndPredicate();

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

            if (CustomRolesHelper.RoleExist(CustomRoles.Sunnyboy) && Main.AllAlivePlayerControls.Count() > 1) return false;

            int Imp = Utils.AlivePlayersCount(CountTypes.Impostor);
            int Jackal = Utils.AlivePlayersCount(CountTypes.Jackal);
            int Pel = Utils.AlivePlayersCount(CountTypes.Pelican);
            int Crew = Utils.AlivePlayersCount(CountTypes.Crew);
            int Gam = Utils.AlivePlayersCount(CountTypes.Gamer);

            Imp += Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsImpostor() && x.Is(CustomRoles.DualPersonality)).Count();
            Crew += Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsCrewmate() && x.Is(CustomRoles.DualPersonality)).Count();

            if (Imp == 0 && Crew == 0 && Jackal == 0 && Pel == 0 && Gam == 0) //全灭
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
            }
            else if (Main.AllAlivePlayerControls.All(p => p.Is(CustomRoles.Lovers))) //恋人胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lovers);
            }
            else if (Jackal == 0 && Pel == 0 && Gam == 0 && Crew <= Imp) //内鬼胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
            }
            else if (Imp == 0 && Pel == 0 && Gam == 0 && Crew <= Jackal) //豺狼胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jackal);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
            }
            else if (Imp == 0 && Jackal == 0 && Gam == 0 && Crew <= Pel) //鹈鹕胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Pelican);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Pelican);
            }
            else if (Imp == 0 && Jackal == 0 && Pel == 0 && Crew <= Gam) //玩家胜利
            {
                reason = GameOverReason.ImpostorByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Gamer);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Gamer);
            }
            else if (Jackal == 0 && Pel == 0 && Imp == 0 && Gam == 0) //船员胜利
            {
                reason = GameOverReason.HumansByVote;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Crewmate);
            }
            else return false; //胜利条件未达成

            return true;
        }
    }

    // 个人竞技模式用
    class SoloKombatGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;
            if (CustomWinnerHolder.WinnerIds.Count > 0) return false;
            if (CheckGameEndByLivingPlayers(out reason)) return true;
            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorByKill;

            if (SoloKombatManager.RoundTime > 0) return false;

            var list = Main.AllPlayerControls.Where(x => !x.Is(CustomRoles.GM) && SoloKombatManager.GetRankOfScore(x.PlayerId) == 1);
            var winner = list.FirstOrDefault();

            CustomWinnerHolder.WinnerIds = new()
            {
                winner.PlayerId
            };

            Main.DoBlockNameChange = true;

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