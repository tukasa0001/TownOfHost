using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using TownOfHost.Extensions;
using UnityEngine;

namespace TownOfHost.RPC;

public class RpcV2
{
    private uint netId;
    private byte callId;
    private bool immediate;
    private bool requireHost;
    private bool sendToAll;
    private bool sendToHost;
    private int sendTo = -1;
    private readonly List<Tuple<object, WriteType>> writes = new();
    private readonly SendOption sendOption = SendOption.Reliable;

    public static uint GetHostNetId()
    {
        PlayerControl host = PlayerControl.AllPlayerControls.ToArray()
            .FirstOrDefault(player => player.GetClientId() == AmongUsClient.Instance.HostId);
        return host == null ? 0 : host.NetId;
    }

    public static RpcV2 Standard(byte netId, byte callId)
    {
        return new RpcV2
        {
            netId = netId,
            callId = callId,
            immediate = false
        };
    }

    public static RpcV2 Immediate(uint netId, RpcCalls call) => RpcV2.Immediate(netId, (byte)call);

    public static RpcV2 Immediate(uint netId, byte callId)
    {
        return new RpcV2
        {
            netId = netId,
            callId = callId,
            immediate = true
        };
    }

    public RpcV2 Write(bool value) => this.WriteAny(value);
    public RpcV2 Write(byte value) => this.WriteAny(value);
    public RpcV2 Write(float value) => this.WriteAny(value);
    public RpcV2 Write(int value) => this.WriteAny(value);
    public RpcV2 Write(sbyte value) => this.WriteAny(value);
    public RpcV2 Write(string value) => this.WriteAny(value);
    public RpcV2 Write(uint value) => this.WriteAny(value);
    public RpcV2 Write(ulong value) => this.WriteAny(value);
    public RpcV2 Write(ushort value) => this.WriteAny(value);
    public RpcV2 Write(Vector2 vector) => this.WriteAny(vector, WriteType.Vector);
    public RpcV2 Write(InnerNetObject value) => this.WriteAny(value, WriteType.NetObject);
    public RpcV2 WritePacked(int value) => this.WriteAny(value, WriteType.Packed);
    public RpcV2 WritePacked(uint value) => this.WriteAny(value, WriteType.Packed);

    public RpcV2 WriteOptions(IGameOptions options) => this.WriteAny(options, WriteType.Options);

    public RpcV2 RequireHost(bool requireHost)
    {
        this.requireHost = requireHost;
        return this;
    }

    public void SendToFollowing(params int[] include)
    {
        PlayerControl.AllPlayerControls.ToArray()
            .Where(pc => include.Contains(pc.GetClientId()))
            .Do(pc => this.Send(pc.GetClientId()));
    }

    public void SendToAll(params int[] exclude)
    {
        PlayerControl.AllPlayerControls.ToArray()
            .Where(pc => !exclude.Contains(pc.GetClientId()))
            .Do(pc => this.Send(pc.GetClientId()));
    }

    public void SendToHost()
    {
        this.netId = (byte)GetHostNetId();
        this.Send(PlayerControl.LocalPlayer.GetClientId());
    }

    public void Send(int clientId = -1)
    {
        if (requireHost && AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = !immediate
            ? AmongUsClient.Instance.StartRpc(netId, callId, sendOption)
            : AmongUsClient.Instance.StartRpcImmediately(netId, callId, sendOption, clientId);

        foreach (Tuple<object, WriteType> write in writes)
            switch (write.Item2)
            {
                case WriteType.NetObject:
                    writer.WriteNetObject((InnerNetObject)write.Item1);
                    continue;
                case WriteType.Packed:
                {
                    if (write.Item1 is uint item1) writer.WritePacked(item1);
                    else writer.WritePacked((int)write.Item1);
                    continue;
                }
                case WriteType.Options:
                    writer.WriteBytesAndSize(((IGameOptions) write.Item1).ToBytes());
                    continue;
                case WriteType.Vector:
                    NetHelpers.WriteVector2((Vector2)write.Item1, writer);
                    continue;
                case WriteType.Normal:
                default:
                    switch (write.Item1)
                    {
                        case bool data:
                            writer.Write(data);
                            break;
                        case byte data:
                            writer.Write(data);
                            break;
                        case float data:
                            writer.Write(data);
                            break;
                        case int data:
                            writer.Write(data);
                            break;
                        case sbyte data:
                            writer.Write(data);
                            break;
                        case string data:
                            writer.Write(data);
                            break;
                        case uint data:
                            writer.Write(data);
                            break;
                        case ulong data:
                            writer.Write(data);
                            break;
                        case ushort data:
                            writer.Write(data);
                            break;
                    }

                    break;
            }

        if (!immediate)
            writer.EndMessage();
        else
            AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    private RpcV2 WriteAny(object value, WriteType writeType = WriteType.Normal)
    {
        writes.Add(new Tuple<object, WriteType>(value, writeType));
        return this;
    }

    private enum WriteType: byte
    {
        Normal,
        Packed,
        NetObject,
        Options,
        Vector
    }
}