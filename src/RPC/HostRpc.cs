using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Options;
using VentLib;
using VentLib.Extensions;
using VentLib.Logging;
using VentLib.RPC.Attributes;

namespace TownOfHost.RPC;

public static class HostRpc
{
    [ModRPC((uint) ModCalls.SendOptionPreview, RpcActors.Host, RpcActors.NonHosts)]
    public static void RpcSendOptions(List<OptionHolder> options)
    {
        if (TOHPlugin.OptionManager.ReceivedOptions == null)
            VentLogger.Old($"Received {options.Count} Options From Host", "HostOptions");

        TOHPlugin.OptionManager.ReceivedOptions = options;
    }

    [ModRPC((uint) ModCalls.Debug, RpcActors.Host, RpcActors.NonHosts)]
    public static void RpcDebug(string message)
    {
        VentLogger.Info($"Message from {Vents.GetLastSender((uint)ModCalls.Debug).GetRawName()} => {message}", "RpcDebug");
        GameData.Instance.AllPlayers.ToArray().Select(p => (p.GetNameWithRole(), p.IsDead, p.IsIncomplete)).StrJoin().DebugLog("All Players: ");
    }
}