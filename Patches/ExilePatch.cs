using Hazel;
using HarmonyLib;

namespace TownOfHost
{
    class ExileControllerWrapUpPatch
    {
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                WrapUpPostfix(__instance.exiled);
            }
        }

        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
        class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance)
            {
                WrapUpPostfix(__instance.exiled);
            }
        }
        static void WrapUpPostfix(GameData.PlayerInfo exiled)
        {
            main.witchMeeting = false;
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            if (exiled != null)
            {
                PlayerState.setDeathReason(exiled.PlayerId, PlayerState.DeathReason.Vote);
                var role = exiled.getCustomRole();
                if (role == CustomRoles.Jester && AmongUsClient.Instance.AmHost)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.JesterExiled, Hazel.SendOption.Reliable, -1);
                    writer.Write(exiled.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPC.JesterExiled(exiled.PlayerId);
                }
                if (role == CustomRoles.Terrorist && AmongUsClient.Instance.AmHost)
                    Utils.CheckTerroristWin(exiled);
                if (role != CustomRoles.Witch && main.SpelledPlayer != null)
                {
                    foreach (var p in main.SpelledPlayer)
                    {
                        PlayerState.setDeathReason(p.PlayerId, PlayerState.DeathReason.Spell);
                        main.IgnoreReportPlayers.Add(p.PlayerId);
                        p.RpcMurderPlayer(p);
                    }
                }
                PlayerState.setDead(exiled.PlayerId);
            }
            if (AmongUsClient.Instance.AmHost && main.isFixedCooldown)
                main.RefixCooldownDelay = main.RealOptionsData.KillCooldown - 3f;
            main.SpelledPlayer.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                pc.ResetKillCooldown();
                if (PlayerControl.GameOptions.MapId != 4)
                {
                    if (pc.isSerialKiller())
                    {
                        pc.RpcGuardAndKill(pc);
                        main.SerialKillerTimer.Add(pc.PlayerId, 0f);
                    }
                    if (pc.isBountyHunter())
                    {
                        main.AllPlayerKillCooldown[pc.PlayerId] *= 2;
                        pc.RpcGuardAndKill(pc);
                        main.BountyTimer.Add(pc.PlayerId, 0f);
                    }
                    if (pc.isWarlock())
                    {
                        main.CursedPlayers[pc.PlayerId] = (null);
                        main.isCurseAndKill[pc.PlayerId] = false;
                    }
                }
                if (PlayerControl.GameOptions.MapId == 4)//Airship用
                {
                    if (pc.isSerialKiller() || pc.isBountyHunter())
                    {
                        main.AirshipMeetingTimer.Add(pc.PlayerId, 0f);
                        main.AllPlayerKillCooldown[pc.PlayerId] *= 2;
                    }
                    if (pc.isWarlock())
                    {
                        main.CursedPlayers[pc.PlayerId] = (null);
                        main.isCurseAndKill[pc.PlayerId] = false;
                    }
                }
            }
            Utils.CountAliveImpostors();
            Utils.CustomSyncAllSettings();
            Utils.NotifyRoles();
            Logger.info("タスクフェイズ開始", "Phase");
        }
    }
}
