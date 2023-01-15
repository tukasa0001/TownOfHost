#nullable enable
using System;
using System.Reflection;
using MonoMod.RuntimeDetour;

namespace VentLib;


[AttributeUsage(AttributeTargets.Method)]
public class ModRPCAttribute : Attribute
{
    public readonly uint CallId;
    public RpcActors Senders { get; }
    public RpcActors Receivers { get; }
    public MethodInvocation Invocation { get; }
    internal Type[] Parameters = null!;
    private MethodBase trampoline = null!;
    private Func<object?> instanceSupplier = null!;
    private IDetour hook = null!;

    public ModRPCAttribute(uint call, RpcActors senders = RpcActors.Everyone, RpcActors receivers = RpcActors.Everyone, MethodInvocation invocation = MethodInvocation.ExecuteNever)
    {
        CallId = call;
        this.Senders = senders;
        this.Receivers = receivers;
        this.Invocation = invocation;
    }
}

public enum RpcActors: byte
{
    None,
    /// <summary>
    /// Permits ONLY Host to Send / Receive marked RPC
    /// </summary>
    Host,
    /// <summary>
    /// Permits everyone BUT host to Send / Receive marked RPC
    /// </summary>
    NonHosts,
    /// <summary>
    /// Is equivalent to marking as "Everyone"
    /// </summary>
    LastSender,
    Everyone
}

public enum MethodInvocation
{
    ExecuteNever,
    ExecuteBefore,
    ExecuteAfter
}
