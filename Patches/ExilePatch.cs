using AmongUs.Data;
using HarmonyLib;
using System;
using TownOfHostForE.Roles.AddOns.Common;
using TownOfHostForE.Roles.Animals;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Crewmate;
using TownOfHostForE.Roles.Neutral;

namespace TownOfHostForE
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
                catch (Exception ex)
                {
                    Logger.Info("追放処理例外：" + ex.Message + "/" + ex.StackTrace, "WrapUpAndSpawn");
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
                catch (Exception ex)
                {
                    Logger.Info("追放処理例外：" + ex.Message + "/" + ex.StackTrace, "WrapUpAndSpawn");
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

            var mapId = Main.NormalOptions.MapId;
            // エアシップではまだ湧かない
            if ((MapNames)mapId != MapNames.Airship)
            {
                foreach (var state in PlayerState.AllPlayerStates.Values)
                {
                    state.HasSpawned = true;
                }
            }

            bool DecidedWinner = false;
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            AntiBlackout.RestoreIsDead(doSend: false);
            if (exiled != null)
            {
                var role = exiled.GetCustomRole();
                var info = role.GetRoleInfo();
                //霊界用暗転バグ対処
                if (!AntiBlackout.OverrideExiledPlayer && info?.IsDesyncImpostor == true)
                    exiled.Object?.ResetPlayerCam(1f);

                exiled.IsDead = true;
                if (role != CustomRoles.AntiComplete)
                    PlayerState.GetByPlayerId(exiled.PlayerId).DeathReason = CustomDeathReason.Vote;

                foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
                {
                    roleClass.OnExileWrapUp(exiled, ref DecidedWinner);
                }
                Sending.OnExileWrapUp(exiled.Object);

                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) PlayerState.GetByPlayerId(exiled.PlayerId).SetDead();
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                pc.ResetKillCooldown();
            }
            if (RandomSpawn.IsRandomSpawn())
            {
                RandomSpawn.SpawnMap map;
                switch (mapId)
                {
                    case 0:
                        map = new RandomSpawn.SkeldSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 1:
                        map = new RandomSpawn.MiraHQSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 2:
                        map = new RandomSpawn.PolusSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                    case 5:
                        map = new RandomSpawn.FungleSpawnMap();
                        Main.AllPlayerControls.Do(map.RandomTeleport);
                        break;
                }
            }
            FallFromLadder.Reset();
            Utils.CountAlivePlayers(true);
            Utils.AfterMeetingTasks();
            Utils.SyncAllSettings();
            Utils.NotifyRoles();
        }
        static void WrapUpFinalizer(GameData.PlayerInfo exiled)
        {
            //WrapUpPostfixで例外が発生しても、この部分だけは確実に実行されます。
            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    exiled = AntiBlackout_LastExiled;
                    AntiBlackout.SendGameData();
                    if (AntiBlackout.OverrideExiledPlayer && // 追放対象が上書きされる状態 (上書きされない状態なら実行不要)
                        exiled != null && //exiledがnullでない
                        exiled.Object != null) //exiled.Objectがnullでない
                    {
                        exiled.Object.RpcExileV2();
                    }
                }, 0.5f, "Restore IsDead Task");
                _ = new LateTask(() =>
                {
                    Main.AfterMeetingDeathPlayers.Do(x =>
                    {
                        var player = Utils.GetPlayerById(x.Key);
                        var roleClass = CustomRoleManager.GetByPlayerId(x.Key);
                        var requireResetCam = player?.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true;
                        var state = PlayerState.GetByPlayerId(x.Key);
                        Logger.Info($"{player.GetNameWithRole()}を{x.Value}で死亡させました", "AfterMeetingDeath");
                        state.DeathReason = x.Value;
                        state.SetDead();
                        player?.RpcExileV2();
                        if (x.Value == CustomDeathReason.Suicide)
                            player?.SetRealKiller(player, true);
                        if (requireResetCam)
                            player?.ResetPlayerCam(1f);
                        if (roleClass is Executioner executioner && executioner.TargetId == x.Key)
                            Executioner.ChangeRoleByTarget(x.Key);
                    });
                    Main.AfterMeetingDeathPlayers.Clear();
                }, 0.5f, "AfterMeetingDeathPlayers Task");
            }

            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            RemoveDisableDevicesPatch.UpdateDisableDevices();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
            Logger.Info("タスクフェイズ開始", "Phase");
            Badger.MeetingEndCheck();
            Tiikawa.MeetingEndCheck();
        }
    }
    //static void WrapUpFinalizer(GameData.PlayerInfo exiled)
    //{
    //    //WrapUpPostfixで例外が発生しても、この部分だけは確実に実行されます。
    //    if (AmongUsClient.Instance.AmHost)
    //    {
    //        _ = new LateTask(() =>
    //        {
    //            exiled = AntiBlackout_LastExiled;
    //            AntiBlackout_LastExiled = null;
    //            AntiBlackout.SendGameData();
    //            if (AntiBlackout.OverrideExiledPlayer && // 追放対象が上書きされる状態 (上書きされない状態なら実行不要)
    //                exiled != null && //exiledがnullでない
    //                exiled.Object != null) //exiled.Objectがnullでない
    //            {
    //                exiled.Object.RpcExileV2();
    //            }
    //        }, 0.5f, "Restore IsDead Task");
    //        _ = new LateTask(() =>
    //        {
    //            Main.AfterMeetingDeathPlayers.Do(x =>
    //            {
    //                REIKAITENSOU(x.Key, x.Value);
    //            });
    //            Main.AfterMeetingDeathPlayers.Clear();
    //        }, 0.5f, "AfterMeetingDeathPlayers Task");
    //    }

    //    GameStates.AlreadyDied |= !Utils.IsAllAlive;
    //    RemoveDisableDevicesPatch.UpdateDisableDevices();
    //    SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
    //    Logger.Info("タスクフェイズ開始", "Phase");
    //    Badger.MeetingEndCheck();
    //    Tiikawa.MeetingEndCheck();
    //}



    [HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
    class PolusExileHatFixPatch
    {
        public static void Prefix(PbExileController __instance)
        {
            __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
        }
    }
}