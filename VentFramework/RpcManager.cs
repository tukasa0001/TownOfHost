using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Hazel;
using InnerNet;
using TownOfHost.Extensions;

namespace VentLib;

public static class RpcManager
{
    public static void Register(Assembly assembly, ModRPC rpc)
    {
        if (VentFramework.BuiltinRPCs.Contains(rpc.CallId) && Assembly.GetExecutingAssembly() != assembly)
            throw new ArgumentException($"RPC {rpc.CallId} shares an ID with a Builtin-VentFramework RPC. Please choose a different ID. (Builtin-IDs: {VentFramework.BuiltinRPCs.PrettyString()})");

        if (!VentFramework.RpcBindings.ContainsKey(rpc.CallId))
            VentFramework.RpcBindings.Add(rpc.CallId, new List<ModRPC>());

        VentFramework.RpcBindings[rpc.CallId].Add(rpc);
    }


    internal static bool HandleRpc(byte callId, MessageReader reader)
    {
        if (callId != 203) return true;
        uint customId = reader.ReadUInt32();
        RpcActors actor = (RpcActors)reader.ReadByte();
        if (!CanReceive(actor)) return true;
        uint senderId = reader.ReadPackedUInt32();
        PlayerControl player = null;
        if (AmongUsClient.Instance.allObjectsFast.TryGet(senderId, out InnerNetObject netObject))
        {
            player = netObject.TryCast<PlayerControl>();
            if (player != null) VentFramework.LastSenders[customId] = player;
        }

        if (player != null && player.PlayerId == PlayerControl.LocalPlayer.PlayerId) return true;
        string sender = player == null ? "Unknown" : player.GetRawName();
        string receiverType = AmongUsClient.Instance.AmHost ? "Host" : "NonHost";
        TownOfHost.Logger.Info($"Custom RPC Received ({customId}) from \"{sender}\" as {receiverType}", "VentFramework");
        if (!VentFramework.RpcBindings.TryGetValue(customId, out List<ModRPC> RPCs))
        {
            TownOfHost.Logger.Warn($"Received Unknown RPC: {customId}", "VentFramework");
            return true;
        }


        foreach (ModRPC modRPC in RPCs)
        {
            // Cases in which the client is not the correct listener
            if (!CanReceive(modRPC.Receivers)) continue;
            if (!VentFramework.CallingAssemblyFlag(modRPC.Assembly).HasFlag(VentControlFlag.AllowedReceiver)) continue;
            modRPC.InvokeTrampoline(ParameterHelper.Cast(modRPC.Parameters, reader));
        }

        return true;
    }

    private static bool CanReceive(RpcActors actor)
    {
        return actor switch
        {
            RpcActors.None => false,
            RpcActors.Host => AmongUsClient.Instance.AmHost,
            RpcActors.NonHosts => !AmongUsClient.Instance.AmHost,
            RpcActors.LastSender => true,
            RpcActors.Everyone => true,
            _ => throw new ArgumentOutOfRangeException()
        };
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

