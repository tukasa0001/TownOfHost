using AmongUs.Data;
using HarmonyLib;
using System.Linq;
using TOHE.Roles.Impostor;
using TOHE.Roles.Neutral;

namespace TOHE;

internal class ExileControllerWrapUpPatch
{
    public static GameData.PlayerInfo AntiBlackout_LastExiled;
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
    private class BaseExileControllerPatch
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
    private class AirshipExileControllerPatch
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

    private static void WrapUpPostfix(GameData.PlayerInfo exiled)
    {
        if (AntiBlackout.OverrideExiledPlayer)
        {
            exiled = AntiBlackout_LastExiled;
        }

        if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
        AntiBlackout.RestoreIsDead(doSend: false);

        if (!Collector.CollectorWin(false) && exiled != null) //判断集票者胜利
        {
            //霊界用暗転バグ対処
            if (!AntiBlackout.OverrideExiledPlayer && Main.ResetCamPlayerList.Contains(exiled.PlayerId))
                exiled.Object?.ResetPlayerCam(1f);

            bool DecidedWinner = false;
            exiled.IsDead = true;
            Main.PlayerStates[exiled.PlayerId].deathReason = PlayerState.DeathReason.Vote;

            if (AmongUsClient.Instance.AmHost)
            {

                var role = exiled.GetCustomRole();

                //判断冤罪师胜利
                var playerList = Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Innocent) && !x.IsAlive() && x.GetRealKiller().PlayerId == exiled.PlayerId);
                if (playerList.Count() > 0)
                {
                    if (!Options.InnocentCanWinByImp.GetBool() && role.IsImpostor())
                    {
                        Logger.Info("冤罪的目标是内鬼，非常可惜啊", "Exeiled Winner Check");
                    }
                    else
                    {
                        if (DecidedWinner) CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Innocent);
                        else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Innocent);
                        foreach (var pc in playerList) CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                        DecidedWinner = true;
                    }
                }

                //判断小丑胜利
                if (role == CustomRoles.Jester)
                {
                    if (DecidedWinner) CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Jester);
                    else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Jester);
                    CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
                    DecidedWinner = true;
                }

                //判断处刑人胜利
                Executioner.CheckExileTarget(exiled, DecidedWinner);
                foreach (var executioner in Executioner.playerIdList)
                {
                    var GetValue = Executioner.Target.TryGetValue(executioner, out var targetId);
                    if (GetValue && exiled.PlayerId == targetId)
                    {
                        if (DecidedWinner) CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Executioner);
                        else CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Executioner);
                        CustomWinnerHolder.WinnerIds.Add(executioner);
                    }
                    DecidedWinner = true;
                }

                //判断恐怖分子胜利
                if (role == CustomRoles.Terrorist) Utils.CheckTerroristWin(exiled);

                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) Main.PlayerStates[exiled.PlayerId].SetDead();
            }
        }

        if (AmongUsClient.Instance.AmHost && Main.IsFixedCooldown)
            Main.RefixCooldownDelay = Options.DefaultKillCooldown - 3f;

        Witch.RemoveSpelledPlayer();

        foreach (var pc in Main.AllPlayerControls)
        {
            pc.ResetKillCooldown();
            if (Options.MayorHasPortableButton.GetBool() && pc.Is(CustomRoles.Mayor))
                pc.RpcResetAbilityCooldown();
            if (pc.Is(CustomRoles.Paranoia))
                pc.RpcResetAbilityCooldown();
            if (pc.Is(CustomRoles.Veteran))
                pc.RpcResetAbilityCooldown();
            if (pc.Is(CustomRoles.Warlock))
            {
                Main.CursedPlayers[pc.PlayerId] = null;
                Main.isCurseAndKill[pc.PlayerId] = false;
            }
            if (pc.Is(CustomRoles.Assassin))
            {
                Main.MarkedPlayers[pc.PlayerId] = null;
                Main.isMarkAndKill[pc.PlayerId] = false;
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

    private static void WrapUpFinalizer(GameData.PlayerInfo exiled)
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
                    Logger.Info($"{player.GetNameWithRole()}を{x.Value}で死亡させました", "AfterMeetingDeath");
                    Main.PlayerStates[x.Key].deathReason = x.Value;
                    Main.PlayerStates[x.Key].SetDead();
                    player?.RpcExileV2();
                    if (x.Value == PlayerState.DeathReason.Suicide)
                        player?.SetRealKiller(player, true);
                    if (Main.ResetCamPlayerList.Contains(x.Key))
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
internal class PolusExileHatFixPatch
{
    public static void Prefix(PbExileController __instance)
    {
        __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
    }
}