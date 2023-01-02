using HarmonyLib;
using Hazel;
using TownOfHost.Extensions;
using TownOfHost.Roles;

namespace TownOfHost.Patches.Actions;

public static class EnterVentPatches
{
    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    class EnterVentPatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
        {
            if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek && OldOptions.IgnoreVent.GetBool())
                pc.MyPhysics.RpcBootFromVent(__instance.Id);
            if (pc.Is(Mayor.Ref<Mayor>()))
            {
                if (Main.MayorUsedButtonCount.TryGetValue(pc.PlayerId, out var count) && count < OldOptions.MayorNumOfUseButton.GetInt())
                {
                    pc?.MyPhysics?.RpcBootFromVent(__instance.Id);
                    pc?.ReportDeadBody(null);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (AmongUsClient.Instance.IsGameStarted &&
                    __instance.myPlayer.IsDouseDone())
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (!pc.Data.IsDead)
                        {
                            if (pc != __instance.myPlayer)
                            {
                                //生存者は焼殺
                                //pc.SetRealKiller(__instance.myPlayer);
                                pc.RpcMurderPlayer(pc);
                                Main.PlayerStates[pc.PlayerId].deathReason = PlayerStateOLD.DeathReason.Torched;
                                Main.PlayerStates[pc.PlayerId].SetDead();
                            }
                            else
                                OldRPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
                        }
                    }
                    CustomWinnerHolder.ShiftWinnerAndSetWinner(CustomWinner.Arsonist); //焼殺で勝利した人も勝利させる
                    CustomWinnerHolder.WinnerIds.Add(__instance.myPlayer.PlayerId);
                    return true;
                }
                if (__instance.myPlayer.Is(CustomRoles.Sheriff) ||
                __instance.myPlayer.Is(CustomRoles.SKMadmate) ||
                __instance.myPlayer.Is(CustomRoles.Arsonist) ||
                (__instance.myPlayer.Is(CustomRoles.Mayor) && Main.MayorUsedButtonCount.TryGetValue(__instance.myPlayer.PlayerId, out var count) && count >= OldOptions.MayorNumOfUseButton.GetInt()) ||
                (__instance.myPlayer.Is(CustomRoles.Jackal) && !CustomRoleManager.Static.Jackal.CanVent())
                )
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                    writer.WritePacked(127);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    new DTask(() =>
                    {
                        int clientId = __instance.myPlayer.GetClientId();
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.5f, "Fix DesyncImpostor Stuck");
                    return false;
                }
            }
            return true;
        }
    }

}