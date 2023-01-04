using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MonoMod.RuntimeDetour;

namespace VentWork;


[AttributeUsage(AttributeTargets.Method)]
public class ModRPC : Attribute
{
    private static readonly HashSet<Assembly> RegisteredAssemblies = new();
    internal static readonly Dictionary<uint, PlayerControl> LastSenders = new();
    public readonly uint RPCId;
    public RpcActors Senders { get; }
    public RpcActors Receivers { get; }
    public bool ExecuteOnSend { get; }
    internal Type[] parameters;
    internal MethodBase trampoline;
    internal Func<object?> instanceSupplier;
    private IDetour hook;

    public ModRPC(CustomRPC rpc, RpcActors senders = RpcActors.Everyone, RpcActors receivers = RpcActors.Everyone, bool executeOnSend = false) : this((uint)rpc, senders, receivers, executeOnSend)
    { }

    public ModRPC(uint rpc, RpcActors senders = RpcActors.Everyone, RpcActors receivers = RpcActors.Everyone, bool executeOnSend = false)
    {
        RPCId = rpc;
        this.Senders = senders;
        this.Receivers = receivers;
        this.ExecuteOnSend = executeOnSend;
    }

    internal static void Register(Assembly assembly, BasePlugin plugin)
    {
        if (RegisteredAssemblies.Contains(assembly)) return;
        RegisteredAssemblies.Add(assembly);

        var methods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

        Type declaringType = null;
        foreach (var method in methods)
        {
            declaringType ??= method.DeclaringType!;
            ModRPC attribute = method.GetCustomAttribute<ModRPC>();
            if (attribute == null) continue;

            if (!method.IsStatic && !declaringType.IsAssignableTo(typeof(IRpcInstance)))
            {
                TownOfHost.Logger.Error($"Unable to Register Method {method.Name}. Reason: Declaring Class of non-static methods must implement IRpcInstance", "VentFramework");
                continue;
            }

            attribute.instanceSupplier = () =>
            {
                if (method.IsStatic) return null;
                if (!IRpcInstance.Instances.TryGetValue(declaringType, out IRpcInstance instance))
                    throw new NullReferenceException(
                        $"Cannot invoke non-static method because IRpcInstance.EnableInstance() was never called for {declaringType}");
                return instance;
            };

            RpcManager.Register(attribute);
            attribute.parameters = ParameterHelper.Verify(method.GetParameters());
            attribute.hook = HookHelper.Generate(method, attribute);
            attribute.trampoline = attribute.hook.GenerateTrampoline();
        }
    }


    internal static void Initialize()
    {
        IL2CPPChainloader.Instance.PluginLoad += (_, assembly, plugin) => Register(assembly, plugin);
    }

    public void InvokeTrampoline(object[] args)
    {
        trampoline.Invoke(instanceSupplier(), args);
    }

    public static PlayerControl GetLastSender(CustomRPC rpc) => GetLastSender((uint)rpc);

    public static PlayerControl GetLastSender(uint rpcId) => LastSenders.GetValueOrDefault(rpcId);
}


public enum RpcActors
{
    None,
    Host,
    NonHosts,
    Everyone
}

public enum CustomRPC: uint
{
    VerifyAddons,
    CamouflageActivation,
    TestRPC,
    WarnAddons
}
