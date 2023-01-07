using System.Collections.Generic;
using HarmonyLib;
using Hazel;
using InnerNet;
using TownOfHost.Extensions;

namespace VentFramework;

public static class RpcManager
{
    public static readonly Dictionary<uint, List<ModRPC>> RpcBindings = new();


    public static void Register(ModRPC rpc)
    {
        if (!RpcBindings.ContainsKey(rpc.RPCId))
            RpcBindings.Add(rpc.RPCId, new List<ModRPC>());

        RpcBindings[rpc.RPCId].Add(rpc);
    }


    internal static bool HandleRpc(byte callId, MessageReader reader)
    {
        if (callId != 203) return true;
        uint customId = reader.ReadPackedUInt32();
        uint senderId = reader.ReadPackedUInt32();
        PlayerControl player = null;
        if (AmongUsClient.Instance.allObjectsFast.TryGet(senderId, out InnerNetObject netObject))
        {
            player = netObject.TryCast<PlayerControl>();
            if (player != null) ModRPC.LastSenders[customId] = player;
        }

        if (player != null && player.PlayerId == PlayerControl.LocalPlayer.PlayerId) return true;
        TownOfHost.Logger.Info($"Custom RPC Received ({customId})", "VentFramework");
        if (!RpcBindings.TryGetValue(customId, out List<ModRPC> RPCs))
        {
            TownOfHost.Logger.Warn($"Received Unknown RPC: {customId}", "VentFramework");
            return true;
        }


        foreach (ModRPC modRPC in RPCs)
        {
            // Cases in which the client is not the correct listener
            if (modRPC.Receivers is RpcActors.None || (modRPC.Receivers is RpcActors.Host && !AmongUsClient.Instance.AmHost) ||
                (modRPC.Receivers is RpcActors.NonHosts && AmongUsClient.Instance.AmHost))
                continue;

            modRPC.InvokeTrampoline(ParameterHelper.Cast(modRPC.Parameters, reader));
        }

        return true;
    }

}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public class RpcHandlerPC
{
    public static bool Prefix(InnerNetObject __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        return RpcManager.HandleRpc(callId, reader);
    }
}

