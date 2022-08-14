using System;
using HarmonyLib;
using Hazel;

namespace TownOfHost
{
    class ExileControllerWrapUpPatch
    {
        public static GameData.PlayerInfo AntiBlackout_LastExiled;
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.exiled);
                }
                finally
                {
                    WrapUpFinalizer(__instance.exiled);
                }
            }
        }

        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
        class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.exiled);
                }
                finally
                {
                    WrapUpFinalizer(__instance.exiled);
                }
            }
        }
        static void WrapUpPostfix(GameData.PlayerInfo exiled)
        {
            if (AntiBlackout.OverrideExiledPlayer)
            {
                exiled = AntiBlackout_LastExiled;
            }

            Main.witchMeeting = false;
            bool DecidedWinner = false;
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            AntiBlackout.RestoreIsDead(doSend: false);
            if (exiled != null)
            {
                exiled.IsDead = true;
                PlayerState.SetDeathReason(exiled.PlayerId, PlayerState.DeathReason.Vote);
                var role = exiled.GetCustomRole();
                if (role == CustomRoles.Jester && AmongUsClient.Instance.AmHost)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                    writer.Write((byte)CustomWinner.Jester);
                    writer.Write(exiled.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPC.JesterExiled(exiled.PlayerId);
                    DecidedWinner = true;
                }
                if (role == CustomRoles.Terrorist && AmongUsClient.Instance.AmHost)
                {
                    Utils.CheckTerroristWin(exiled);
                    DecidedWinner = true;
                }
                foreach (var kvp in Main.ExecutionerTarget)
                {
                    var executioner = Utils.GetPlayerById(kvp.Key);
                    if (executioner == null) continue;
                    if (executioner.Data.IsDead || executioner.Data.Disconnected) continue; //Keyが死んでいたらor切断していたらこのforeach内の処理を全部スキップ
                    if (kvp.Value == exiled.PlayerId && AmongUsClient.Instance.AmHost && !DecidedWinner)
                    {
                        //RPC送信開始
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                        writer.Write((byte)CustomWinner.Executioner);
                        writer.Write(kvp.Key);
                        AmongUsClient.Instance.FinishRpcImmediately(writer); //終了

                        RPC.ExecutionerWin(kvp.Key);
                    }
                }
                if (exiled.Object.Is(CustomRoles.TimeThief))
                    exiled.Object.ResetVotingTime();
                if (exiled.Object.Is(CustomRoles.SchrodingerCat) && Options.SchrodingerCatExiledTeamChanges.GetBool())
                    exiled.Object.ExiledSchrodingerCatTeamChange();


                if (Main.currentWinner != CustomWinner.Terrorist) PlayerState.SetDead(exiled.PlayerId);
            }
            if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
                Main.RefixCooldownDelay = Options.DefaultKillCooldown - 3f;
            Main.SpelledPlayer.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                pc.ResetKillCooldown();
                if (Options.MayorHasPortableButton.GetBool() && pc.Is(CustomRoles.Mayor))
                    pc.RpcResetAbilityCooldown();
                if (pc.Is(CustomRoles.Warlock))
                {
                    Main.CursedPlayers[pc.PlayerId] = null;
                    Main.isCurseAndKill[pc.PlayerId] = false;
                }
            }
            Main.AfterMeetingDeathPlayers.Do(x =>
            {
                var player = Utils.GetPlayerById(x.Key);
                Logger.Info($"{player.GetNameWithRole()}を{x.Value}で死亡させました", "AfterMeetingDeath");
                PlayerState.SetDeathReason(x.Key, x.Value);
                PlayerState.SetDead(x.Key);
                player?.RpcExileV2();
                if (player.Is(CustomRoles.TimeThief) && x.Value == PlayerState.DeathReason.LoversSuicide)
                    player?.ResetVotingTime();
            });
            Main.AfterMeetingDeathPlayers.Clear();
            FallFromLadder.Reset();
            Utils.CountAliveImpostors();
            Utils.AfterMeetingTasks();
            Utils.CustomSyncAllSettings();
            Utils.NotifyRoles();
        }

        static void WrapUpFinalizer(GameData.PlayerInfo exiled)
        {
            //WrapUpPostfixで例外が発生しても、この部分だけは確実に実行されます。
            if (AmongUsClient.Instance.AmHost)
                new LateTask(() =>
                {
                    AntiBlackout.SendGameData();
                    if (AntiBlackout.OverrideExiledPlayer && // 追放対象が上書きされる状態 (上書きされない状態なら実行不要)
                        exiled != null && //exiledがnullでない
                        exiled.Object != null) //exiled.Objectがnullでない
                    {
                        exiled.Object.RpcExileV2();
                    }
                }, 0.5f, "Restore IsDead Task");
            Logger.Info("タスクフェイズ開始", "Phase");
        }
    }
}