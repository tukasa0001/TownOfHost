using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;
using VentLib;
using VentLib.Logging;

namespace TownOfHost.RPC;

public static class HostRpc
{
    [ModRPC((uint) ModCalls.SendOptionPreview, RpcActors.Host, RpcActors.NonHosts)]
    public static void RpcSendOptions(List<OptionHolder> options)
    {
        if (TOHPlugin.OptionManager.receivedOptions == null)
            VentLogger.Old($"Received {options.Count} Options From Host", "HostOptions");

        TOHPlugin.OptionManager.receivedOptions = options;
    }

    [ModRPC((uint) ModCalls.Debug, RpcActors.Host, RpcActors.NonHosts)]
    public static void RpcDebug(string message)
    {
        Logger.Msg($"Message from {VentFramework.GetLastSender((uint)ModCalls.Debug).GetRawName()} => {message}", "RpcDebug");

        GameData.Instance.AllPlayers.ToArray().Select(p => (p.GetNameWithRole(), p.IsDead, p.IsIncomplete)).PrettyString().DebugLog("All Players: ");
    }
}