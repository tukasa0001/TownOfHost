using System;
using Il2CppSystem.Collections.Generic;
using HarmonyLib;
using Hazel;
using InnerNet;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(InnerNetClient))]
static class InnerNetClientPatch
{
    [HarmonyPatch(nameof(InnerNetClient.SendOrDisconnect)), HarmonyPrefix]
    public static void SendOrDisconnectPatch(InnerNetClient __instance, MessageWriter msg)
    {
        if (DebugModeManager.IsDebugMode)
        {
            Logger.Info($"Sending message size: {msg.Length}", "InnerNetClient");
        }
    }
    [HarmonyPatch(nameof(InnerNetClient.SendAllStreamedObjects)), HarmonyPrefix]
    public static bool SendAllStreamedObjectsPatch(InnerNetClient __instance, ref bool __result)
    {
        __result = false;
        List<InnerNetObject> obj = __instance.allObjects;
        lock (obj)
        {
            for (int i = 0; i < __instance.allObjects.Count; i++)
            {
                InnerNetObject innerNetObject = __instance.allObjects[i];
                if (innerNetObject && innerNetObject.IsDirty && (innerNetObject.AmOwner || (innerNetObject.OwnerId == -2 && __instance.AmHost)))
                {
                    MessageWriter messageWriter = __instance.Streams[(int)innerNetObject.sendMode];
                    messageWriter.StartMessage(1);
                    messageWriter.WritePacked(innerNetObject.NetId);
                    try
                    {
                        if (innerNetObject.Serialize(messageWriter, false))
                        {
                            messageWriter.EndMessage();
                        }
                        else
                        {
                            messageWriter.CancelMessage();
                        }
                        if (innerNetObject.Chunked && innerNetObject.IsDirty)
                        {
                            __result = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Info($"Exception:{ex.Message}", "InnerNetClient");
                        messageWriter.CancelMessage();
                    }
                    if (messageWriter.HasBytes(7))
                    {
                        messageWriter.EndMessage();
                        if (DebugModeManager.IsDebugMode)
                        {
                            Logger.Info($"SendAllStreamedObjects", "InnerNetClient");
                        }
                        __instance.SendOrDisconnect(messageWriter);
                        messageWriter.Clear(SendOption.Reliable);
                        messageWriter.StartMessage(5);
                        messageWriter.Write(__instance.GameId);
                    }
                }
            }
        }
        return false;
    }
}
