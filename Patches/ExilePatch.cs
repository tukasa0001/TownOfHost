using AmongUs.Data;
using HarmonyLib;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Neutral;

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

            bool DecidedWinner = false;
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            AntiBlackout.RestoreIsDead(doSend: false);
            if (exiled != null)
            {
                var role = exiled.GetCustomRole();
                var info = role.GetRoleInfo();
                //霊界用暗転バグ対処
                if (!AntiBlackout.OverrideExiledPlayer && (Main.ResetCamPlayerList.Contains(exiled.PlayerId) || (info?.RequireResetCam ?? false)))
                    exiled.Object?.ResetPlayerCam(1f);

                exiled.IsDead = true;
                Main.PlayerStates[exiled.PlayerId].deathReason = PlayerState.DeathReason.Vote;

                CustomRoleManager.GetByPlayerId(exiled.PlayerId).OnExileWrapUp(exiled, ref DecidedWinner);
                if (role == CustomRoles.Terrorist && AmongUsClient.Instance.AmHost)
                {
                    Utils.CheckTerroristWin(exiled);
                    DecidedWinner = true;
                }
                Executioner.CheckExileTarget(exiled, DecidedWinner);
                SchrodingerCat.ChangeTeam(exiled.Object);


                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) Main.PlayerStates[exiled.PlayerId].SetDead();
            }
            if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
                Main.RefixCooldownDelay = Options.DefaultKillCooldown - 3f;

            Witch.RemoveSpelledPlayer();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.ResetKillCooldown();
                if (pc.Is(CustomRoles.Warlock))
                {
                    Main.CursedPlayers[pc.PlayerId] = null;
                    Main.isCurseAndKill[pc.PlayerId] = false;
                }
            }
            if (Options.RandomSpawn.GetBool())
            {
                RandomSpawn.SpawnMap map;
                switch (Main.NormalOptions.MapId)
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
                new LateTask(() =>
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
                new LateTask(() =>
                {
                    Main.AfterMeetingDeathPlayers.Do(x =>
                    {
                        var player = Utils.GetPlayerById(x.Key);
                        var requireResetCam = player?.GetCustomRole().GetRoleInfo()?.RequireResetCam;
                        Logger.Info($"{player.GetNameWithRole()}を{x.Value}で死亡させました", "AfterMeetingDeath");
                        Main.PlayerStates[x.Key].deathReason = x.Value;
                        Main.PlayerStates[x.Key].SetDead();
                        player?.RpcExileV2();
                        if (x.Value == PlayerState.DeathReason.Suicide)
                            player?.SetRealKiller(player, true);
                        if (Main.ResetCamPlayerList.Contains(x.Key) || (requireResetCam.HasValue && requireResetCam.Value))
                            player?.ResetPlayerCam(1f);
                        if (Executioner.Target.ContainsValue(x.Key))
                            Executioner.ChangeRoleByTarget(player);
                    });
                    Main.AfterMeetingDeathPlayers.Clear();
                }, 0.5f, "AfterMeetingDeathPlayers Task");
            }

            GameStates.AlreadyDied |= !Utils.IsAllAlive;
            RemoveDisableDevicesPatch.UpdateDisableDevices();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
            Logger.Info("タスクフェイズ開始", "Phase");
        }
    }

    [HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
    class PolusExileHatFixPatch
    {
        public static void Prefix(PbExileController __instance)
        {
            __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
        }
    }
}