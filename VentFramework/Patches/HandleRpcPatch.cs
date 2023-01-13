using HarmonyLib;
using Hazel;
using InnerNet;

namespace VentLib.Patches;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public class HandleRpcPatch
{
    public static bool Prefix(InnerNetObject __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        return RpcManager.HandleRpc(callId, reader);
    }
}