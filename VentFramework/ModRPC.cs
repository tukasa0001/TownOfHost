#nullable enable
using System;
using System.Reflection;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using TownOfHost;
using TownOfHost.Extensions;
using VentLib.Interfaces;

namespace VentLib;

public class ModRPC
{
    public readonly uint CallId;
    public RpcActors Senders { get; }
    public RpcActors Receivers { get; }
    public MethodInvocation Invocation { get; }
    internal readonly Type[] Parameters;
    internal readonly Assembly? Assembly;
    internal readonly MethodInfo TargetMethod;
    internal DetouredSender Sender;
    private readonly Hook hook;
    private readonly MethodBase trampoline;
    private readonly Func<object?> instanceSupplier;

    public ModRPC(ModRPCAttribute attribute, MethodInfo targetMethod)
    {
        this.TargetMethod = targetMethod;
        CallId = attribute.CallId;
        Senders = attribute.Senders;
        Receivers = attribute.Receivers;
        Invocation = attribute.Invocation;
        Parameters = ParameterHelper.Verify(targetMethod.GetParameters());
        Type? declaringType = targetMethod.DeclaringType;
        if (declaringType == null)
            throw new ArgumentException($"Unable to Register: {targetMethod.Name}. Reason: VentFramework does not current allow for methods without declaring types");

        Assembly = declaringType.Assembly;
        hook = HookHelper.Generate(this);
        trampoline = hook.GenerateTrampoline();

        instanceSupplier = () =>
        {
            if (targetMethod.IsStatic) return null;
            if (!IRpcInstance.Instances.TryGetValue(declaringType, out IRpcInstance? instance))
                throw new NullReferenceException($"Cannot invoke non-static method because IRpcInstance.EnableInstance() was never called for {declaringType}");
            return instance;
        };
    }

    public void Send(int[]? clientIds, params object[] args)
    {
        Sender.Send(clientIds, args);
    }

    public void InvokeTrampoline(object[] args)
    {
        if (DebugConstants.LogTrampoline)
            TownOfHost.Logger.Info($"Calling trampoline \"{this.trampoline.FullDescription()}\" with args: {args.PrettyString()}", "RPCTrampoline");

        trampoline.Invoke(instanceSupplier(), args);
    }
}