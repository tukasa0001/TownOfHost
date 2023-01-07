#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using InnerNet;
using MonoMod.RuntimeDetour;
using TownOfHost.Extensions;
using TownOfHost.Roles;
using UnityEngine;

namespace VentFramework;

public class HookHelper
{
    internal static long globalSendCount;
    private static List<DetouredSender> _senders = new();

    private static readonly OpCode[] _ldc = { OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3, OpCodes.Ldc_I4_4, OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8 };
    private static readonly OpCode[] _ldarg = { OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3 };

    public static Hook Generate(MethodInfo executingMethod, ModRPC attribute)
    {
        Type[] parameters = executingMethod.GetParameters().Select(p => p.ParameterType).ToArray();

        System.Console.WriteLine("Calling Convention: " + executingMethod.CallingConvention);
        System.Console.WriteLine("Method Attributes: " + executingMethod.Attributes);
        DynamicMethod m = new(
            executingMethod.Name,
            executingMethod.ReturnType,
            parameters);

        int senderSize = _senders.Count;

        ILGenerator ilg = m.GetILGenerator();
        if (senderSize <= 8)
            ilg.Emit(_ldc[senderSize]);
        else
            ilg.Emit(OpCodes.Ldc_I4_S, senderSize);
        ilg.Emit(OpCodes.Call, AccessTools.Method(typeof(HookHelper), nameof(GetSender)));

        if (parameters.Length <= 8)
            ilg.Emit(_ldc[parameters.Length]);
        else
            ilg.Emit(OpCodes.Ldc_I4_S, parameters.Length);
        ilg.Emit(OpCodes.Newarr, typeof(object));

        for (int i = 0; i < parameters.Length; i++)
        {
            ilg.Emit(OpCodes.Dup);
            if (i <= 8)
                ilg.Emit(_ldc[i]);
            else
                ilg.Emit(OpCodes.Ldc_I4_S, i);

            if (i <= 3)
                ilg.Emit(_ldarg[i]);
            else
                ilg.Emit(OpCodes.Ldarg_S, i);
            if (parameters[i].IsPrimitive)
                ilg.Emit(OpCodes.Box, parameters[i]);
            ilg.Emit(OpCodes.Stelem_Ref);
        }

        ilg.Emit(OpCodes.Callvirt, AccessTools.Method(typeof(DetouredSender), nameof(DetouredSender.Send)));
        ilg.Emit(OpCodes.Ret);

        _senders.Add(new DetouredSender(attribute));
        return new Hook(executingMethod, m);
    }

    private static DetouredSender GetSender(int index) => _senders[index];

    private static void TestMethod(PlayerControl player, CustomRole role, bool t, int id)
    {
        GetSender(0).Send(player, role, t, id);
    }

}



public class DetouredSender
{
    private int uuid = UnityEngine.Random.RandomRangeInt(0, 999999);
    private int localSendCount;
    private ModRPC rpcInfo;
    private uint callId;
    private RpcActors senders;

    public DetouredSender(ModRPC modRPC)
    {
        this.rpcInfo = modRPC;
        this.callId = this.rpcInfo.RPCId;
        this.senders = modRPC.Senders;
    }

    public void Send(params object?[] args)
    {
        if (rpcInfo.Invocation is MethodInvocation.ExecuteBefore) rpcInfo.InvokeTrampoline(args);
        if (AmongUsClient.Instance == null || this.senders is RpcActors.None) return;
        if ((this.senders is RpcActors.Host && !AmongUsClient.Instance.AmHost) ||
            (this.senders is RpcActors.NonHosts && AmongUsClient.Instance.AmHost))
            return;

        string senderString = AmongUsClient.Instance.AmHost ? "Host" : "NonHost";
        TownOfHost.Logger.Msg($"Sending RPC ({callId}) as {senderString} | ({this.senders} | {args.PrettyString()} | {localSendCount}::{uuid}::{HookHelper.globalSendCount}", "DetouredSender");
        localSendCount++;
        HookHelper.globalSendCount++;
        RpcV2 v2 = RpcV2.Immediate(PlayerControl.LocalPlayer.NetId, 203).WritePacked(callId).RequireHost(false);
        v2.WritePacked(PlayerControl.LocalPlayer.NetId);
        args.Do(a => WriteArg(v2, a));
        v2.Send();
        if (rpcInfo.Invocation is MethodInvocation.ExecuteAfter) rpcInfo.InvokeTrampoline(args);
    }

    internal static void WriteArg(RpcV2 rpcV2, object arg)
    {
        RpcV2 _ = (arg) switch
        {
            bool data => rpcV2.Write(data),
            byte data => rpcV2.Write(data),
            float data => rpcV2.Write(data),
            int data => rpcV2.Write(data),
            sbyte data => rpcV2.Write(data),
            string data => rpcV2.Write(data),
            uint data => rpcV2.Write(data),
            ulong data => rpcV2.Write(data),
            ushort data => rpcV2.Write(data),
            Vector2 data => rpcV2.Write(data),
            InnerNetObject data => rpcV2.Write(data),
            IRpcWritable data => rpcV2.Write(data),
            _ => WriteArgNS(rpcV2, arg)
        };
    }

    private static RpcV2 WriteArgNS(RpcV2 rpcV2, object arg)
    {
        switch (arg)
        {
            case IEnumerable enumerable:
                List<object> list = enumerable.ToObjectList();
                rpcV2.WritePacked((uint)list.Count);
                foreach (object obj in list)
                    WriteArg(rpcV2, obj);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Invalid Argument: {arg}");
        }

        return rpcV2;
    }
}

