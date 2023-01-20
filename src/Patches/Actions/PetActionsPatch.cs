using HarmonyLib;
using Hazel;
using TownOfHost.Extensions;
using TownOfHost.Roles;
using VentLib.Utilities;

namespace TownOfHost.Patches.Actions;


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TryPet))]
class LocalPetPatch
{
    public static bool Prefix(PlayerControl __instance)
    {
        if (!(AmongUsClient.Instance.AmHost)) return true;
        ExternalRpcPetPatch.Prefix(__instance.MyPhysics, 51, new MessageReader());
        return false;
    }

    public static void Postfix(PlayerControl __instance)
    {
        __instance.MyPhysics.CancelPet();
        __instance.petting = false;
    }

}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
class ExternalRpcPetPatch
{
    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callId,
        [HarmonyArgument(1)] MessageReader reader)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var rpcType = callId == 51 ? RpcCalls.Pet : (RpcCalls)callId;
        if (rpcType != RpcCalls.Pet) return;

        PlayerControl playerControl = __instance.myPlayer;


        if (AmongUsClient.Instance.AmHost)
            __instance.CancelPet();
        foreach (PlayerControl player in PlayerControl.AllPlayerControls)
            AmongUsClient.Instance.FinishRpcImmediately(
                AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, 50, SendOption.None,
                    player.GetClientId()));

        ActionHandle handle = ActionHandle.NoInit();
        playerControl.Trigger(RoleActionType.OnPet, ref handle, __instance);
    }
}




















/*
*/