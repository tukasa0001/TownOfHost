using System.Collections.Generic;
using TownOfHost.ReduxOptions;
using VentLib;

namespace TownOfHost.RPC;

public static class HostRpc
{
    [ModRPC((uint) ModCalls.SendOptionPreview, RpcActors.Host, RpcActors.NonHosts)]
    public static void RpcSendOptions(List<OptionHolder> options)
    {
        if (TOHPlugin.OptionManager.receivedOptions == null)
            Logger.Info($"Received {options.Count} Options From Host", "HostOptions");

        TOHPlugin.OptionManager.receivedOptions = options;
    }
}