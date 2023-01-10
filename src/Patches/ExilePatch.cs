using System.Linq;
using AmongUs.Data;
using HarmonyLib;
using TownOfHost.Roles;
using TownOfHost.ReduxOptions;
using TownOfHost.Extensions;
using TownOfHost.Managers;
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
                //霊界用暗転バグ対処
                if (!AntiBlackout.OverrideExiledPlayer && TOHPlugin.ResetCamPlayerList.Contains(exiled.PlayerId))
                    exiled.Object?.ResetPlayerCam(1f);


                ActionHandle selfExiledHandle = ActionHandle.NoInit();
                ActionHandle otherExiledHandle = ActionHandle.NoInit();
                exiled.Object.Trigger(RoleActionType.SelfExiled, ref selfExiledHandle);
                Game.TriggerForAll(RoleActionType.OtherExiled, ref otherExiledHandle, exiled);


                exiled.IsDead = true;
                TOHPlugin.PlayerStates[exiled.PlayerId].deathReason = PlayerStateOLD.DeathReason.Vote;
                var role = exiled.GetCustomRole();
                if (role is Jester && AmongUsClient.Instance.AmHost)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jester);
                    CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                    //吊られたJesterをターゲットにしているExecutionerも追加勝利
                    DecidedWinner = true;
                }
                if (role is Terrorist && AmongUsClient.Instance.AmHost)
                {
                    Utils.CheckTerroristWin(exiled);
                    DecidedWinner = true;
                }


                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) TOHPlugin.PlayerStates[exiled.PlayerId].SetDead();
            }


            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                /*pc.ResetKillCooldown();*/
                if (OldOptions.MayorHasPortableButton.GetBool() && pc.Is(CustomRoles.Mayor))
                    pc.RpcResetAbilityCooldown();
                if (!pc.Is(CustomRoles.Warlock)) continue;
            }
            if (StaticOptions.RandomSpawn)
            {
                RandomSpawn.SpawnMap map;
                switch (TOHPlugin.NormalOptions.MapId)
                {
                    case 0:
                        map = new RandomSpawn.SkeldSpawnMap();
                        PlayerControl.AllPlayerControls.ToArray().Do(map.RandomTeleport);
                        break;
                    case 1:
                        map = new RandomSpawn.MiraHQSpawnMap();
                        PlayerControl.AllPlayerControls.ToArray().Do(map.RandomTeleport);
                        break;
                    case 2:
                        map = new RandomSpawn.PolusSpawnMap();
                        PlayerControl.AllPlayerControls.ToArray().Do(map.RandomTeleport);
                        break;
                }
            }
            FallFromLadder.Reset();
        }

        static void WrapUpFinalizer(GameData.PlayerInfo exiled)
        {
            //WrapUpPostfixで例外が発生しても、この部分だけは確実に実行されます。
            if (AmongUsClient.Instance.AmHost)
            {
                new DTask(() =>
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
                new DTask(() =>
                {
                    TOHPlugin.AfterMeetingDeathPlayers.Do(x =>
                    {
                        var player = Utils.GetPlayerById(x.Key);
                        Logger.Info($"{player.GetNameWithRole()}を{x.Value}で死亡させました", "AfterMeetingDeath");
                        TOHPlugin.PlayerStates[x.Key].deathReason = x.Value;
                        TOHPlugin.PlayerStates[x.Key].SetDead();
                        player?.RpcExileV2();
                        /*if (x.Value == PlayerStateOLD.DeathReason.Suicide)
                            player?.SetRealKiller(player, true);*/
                        if (TOHPlugin.ResetCamPlayerList.Contains(x.Key))
                            player?.ResetPlayerCam(1f);
                        // TODO: investigate reset voting time
                        /*if (player.Is(CustomRoles.TimeThief) && x.Value == PlayerStateOLD.DeathReason.FollowingSuicide)
                            player?.ResetVotingTime();*/
                    });
                    TOHPlugin.AfterMeetingDeathPlayers.Clear();
                }, 0.5f, "AfterMeetingDeathPlayers Task");
            }

            GameStates.AlreadyDied |= GameData.Instance.AllPlayers.ToArray().Any(x => x.IsDead);
            RemoveDisableDevicesPatch.UpdateDisableDevices();
            SoundManager.Instance.ChangeMusicVolume(DataManager.Settings.Audio.MusicVolume);
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