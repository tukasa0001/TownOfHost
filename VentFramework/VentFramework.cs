#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace VentLib;


//if the client has an unsupported addon it's rpcs get disabled completely CHECK!
//if the client is missing an addon then the host's rpcs from that addon to that client get disabled

public static class VentFramework
{
    public static uint[] BuiltinRPCs = { 1017 };
    private static Harmony harmony;
    /*static VentFramework()
    {
        harmony = new Harmony("com.tealeaf.VentFramework");
        harmony.PatchAll();
    }*/

    internal static readonly Dictionary<uint, List<ModRPC>> RpcBindings = new();
    internal static readonly Dictionary<Assembly, VentControlFlag> RegisteredAssemblies = new();
    internal static readonly Dictionary<Assembly, int[]> BlockedReceivers = new();
    internal static readonly Dictionary<uint, PlayerControl> LastSenders = new();

    public static int[]? CallingAssemblyBlacklist() => BlockedReceivers.GetValueOrDefault(Assembly.GetCallingAssembly());

    public static VentControlFlag CallingAssemblyFlag(Assembly? assembly = null)
    {
        if (!RegisteredAssemblies.TryGetValue(assembly ?? Assembly.GetCallingAssembly(), out VentControlFlag flag))
            flag = VentControlFlag.AllowedReceiver | VentControlFlag.AllowedSender;
        return flag;
    }

    public static void BlockClient(Assembly assembly, int clientId)
    {
        int[] newBlockedArray = BlockedReceivers.TryGetValue(assembly, out int[]? blockedClients)
            ? blockedClients.AddToArray(clientId)
            : new[] { clientId };
        BlockedReceivers[Assembly.GetCallingAssembly()] = newBlockedArray;
    }

    internal static void SetControlFlag(Assembly assembly, VentControlFlag flag)
    {
        // Assemblies must be registered first before they can be updated
        if (!RegisteredAssemblies.ContainsKey(assembly)) return;
        RegisteredAssemblies[assembly] = flag;
    }

    public static ModRPC? FindRPC(uint callId, MethodInfo? targetMethod = null)
    {
        if (!RpcBindings.TryGetValue(callId, out List<ModRPC>? RPCs))
        {
            TownOfHost.Logger.Warn($"Attempted to find unregistered RPC: {callId}", "VentFramework");
            return null;
        }

        return RPCs.FirstOrDefault(v => targetMethod == null || v.TargetMethod.Equals(targetMethod));
    }

    public static PlayerControl? GetLastSender(uint rpcId) => VentFramework.LastSenders.GetValueOrDefault(rpcId);

    public static void Register(Assembly assembly)
    {
        if (RegisteredAssemblies.ContainsKey(assembly)) return;
        RegisteredAssemblies.Add(assembly, VentControlFlag.AllowedReceiver | VentControlFlag.AllowedSender);

        var methods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            .Where(m => m.GetCustomAttribute<ModRPCAttribute>() != null).ToList();

        TownOfHost.Logger.Info($"Registering {methods.Count} methods from {assembly.GetName().Name}", "VentFramework");
        foreach (var method in methods)
        {
            ModRPCAttribute attribute = method.GetCustomAttribute<ModRPCAttribute>()!;
            Type? declaringType = method.DeclaringType;

            if (!method.IsStatic && declaringType != null && !declaringType.IsAssignableTo(typeof(IRpcInstance)))
                throw new ArgumentException($"Unable to Register Method {method.Name}. Reason: Declaring Class of non-static methods must implement IRpcInstance");

            RpcManager.Register(assembly, new ModRPC(attribute, method));
        }
    }

    internal static void Initialize()
    {
        IL2CPPChainloader.Instance.PluginLoad += (_, assembly, _) => Register(assembly);
    }
}

[Flags]
public enum VentControlFlag
{
    AllowedReceiver = 1,
    AllowedSender = 2,
}