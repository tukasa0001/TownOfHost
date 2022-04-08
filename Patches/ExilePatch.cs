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
                {
                    Utils.CheckTerroristWin(exiled);
                }
                if (role != CustomRoles.Witch && main.SpelledPlayer != null)
                {
                    foreach (var p in main.SpelledPlayer)
                    {
                        PlayerState.setDeathReason(p.PlayerId, PlayerState.DeathReason.Spell);
                        main.IgnoreReportPlayers.Add(p.PlayerId);
                        p.RpcMurderPlayer(p);
                    }
                }
                PlayerState.isDead[exiled.PlayerId] = true;
            }
            if (exiled == null && main.SpelledPlayer != null)
            {
                foreach (var p in main.SpelledPlayer)
                {
                    PlayerState.setDeathReason(p.PlayerId, PlayerState.DeathReason.Spell);
                    main.IgnoreReportPlayers.Add(p.PlayerId);
                    p.RpcMurderPlayer(p);
                }
            }
            if (AmongUsClient.Instance.AmHost && main.isFixedCooldown)
            {
                if (CustomRoles.BountyHunter.getCount() == 0) main.RefixCooldownDelay = main.RealOptionsData.KillCooldown - 3f;
            }
            main.SpelledPlayer.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);
            foreach (var wr in PlayerControl.AllPlayerControls)
            {
                if (wr.isSerialKiller())
                {
                    wr.RpcGuardAndKill(wr);
                    main.SerialKillerTimer.Add(wr.PlayerId, 0f);
                }
                if (wr.isBountyHunter())
                {
                    wr.RpcGuardAndKill(wr);
                    main.BountyTimer.Add(wr.PlayerId, 0f);
                }
                if (wr.isWarlock())
                {
                    wr.RpcGuardAndKill(wr);
                    main.CursedPlayers[wr.PlayerId] = (null);
                    main.isCurseAndKill[wr.PlayerId] = false;
                }
                if (wr.isSchrodingerCat() && Options.SchrodingerCatExiledTeamChanges.GetBool())
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SchrodingerCatExiled, Hazel.SendOption.Reliable, -1);
                    writer.Write(exiled.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    wr.ExiledSchrodingerCatTeamChange();
                }
                if (wr.isArsonist()) wr.RpcGuardAndKill(wr);
            }
            main.BountyMeetingCheck = true;
            Utils.CountAliveImpostors();
            Utils.CustomSyncAllSettings();
            Utils.NotifyRoles();
        }
    }
}
