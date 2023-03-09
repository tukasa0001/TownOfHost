using HarmonyLib;
using Hazel;
using TOHTOR.Extensions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using VentLib.Networking.RPC;
using VentLib.Utilities;

namespace TOHTOR.Patches.Actions;


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

        RpcV2.Immediate(__instance.NetId, RpcCalls.CancelPet)
            .SendExclusive(__instance.myPlayer.GetClientId());
        Async.Schedule(() => RpcV2.Immediate(__instance.NetId, RpcCalls.CancelPet).SendInclusive(__instance.myPlayer.GetClientId()), NetUtils.DeriveDelay(0.5f));

        ActionHandle handle = ActionHandle.NoInit();
        playerControl.Trigger(RoleActionType.OnPet, ref handle, __instance);
    }
}




















/*
*/