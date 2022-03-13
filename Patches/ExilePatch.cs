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
            if(!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            main.CursedPlayerDie.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);//呪われた人が死んだ場合にリストから削除する
            main.SpelledPlayer.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);
            foreach(var p in main.SpelledPlayer)
            {
                main.GetPlayerState(p).deathReason=PlayerState.DeathReason.Spell;
                main.IgnoreReportPlayers.Add(p.PlayerId);
                p.RpcMurderPlayer(p);
            }
            foreach(var p in main.CursedPlayerDie)
            {
                main.GetPlayerState(p).deathReason = PlayerState.DeathReason.Spell;
                main.IgnoreReportPlayers.Add(p.PlayerId);
                p.RpcMurderPlayer(p);
            }
            if (exiled != null)
            {
                var role = exiled.getCustomRole();
                if (role == CustomRoles.Jester && AmongUsClient.Instance.AmHost)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.JesterExiled, Hazel.SendOption.Reliable, -1);
                    writer.Write(exiled.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.JesterExiled(exiled.PlayerId);
                }
                if (role == CustomRoles.Terrorist && AmongUsClient.Instance.AmHost)
                {
                    main.CheckTerroristWin(exiled);
                }
                main.GetPlayerState(exiled).deathReason = PlayerState.DeathReason.Vote;
            }
            if (AmongUsClient.Instance.AmHost && main.isFixedCooldown)
            {
                if(main.BountyHunterCount == 0)main.RefixCooldownDelay = main.RealOptionsData.KillCooldown - 3f;
            }
            foreach(var wr in PlayerControl.AllPlayerControls){
                if(wr.isSerialKiller()){
                    wr.RpcGuardAndKill(wr);
                    main.SerialKillerTimer.Add(wr.PlayerId,0f);
                }
                if(wr.isBountyHunter()){
                    wr.RpcGuardAndKill(wr);
                    main.BountyTimer.Add(wr.PlayerId, 0f);
                }
                if(wr.isWarlock()){
                    wr.RpcGuardAndKill(wr);
                    main.CursedPlayers.Remove(wr.PlayerId);
                    main.FirstCursedCheck.Remove(wr.PlayerId);
                    main.FirstCursedCheck.Add(wr.PlayerId, false);
                }
            }
            main.BountyMeetingCheck = true;
            main.CustomSyncAllSettings();
            main.NotifyRoles();
        }
    }
}
