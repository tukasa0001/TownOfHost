using System;
using Hazel;
using VentLib.Logging;
using VentLib.Networking.Batches;
using VentLib.Networking.Interfaces;
using VentLib.Networking.RPC.Attributes;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;

namespace TOHTOR;

public class Test : IRpcSendable<Test>
{
    private string name;
    private int number;
    private bool truth;

    private Test() {
    }

    public Test(string name, int number, bool truth)
    {
        this.name = name;
        this.number = number;
        this.truth = truth;
    }


    public BatchEnd Write(BatchWriter writer)
    {
        return writer
            .Write(this.name)
            .NextBatch()
            .Write(this.number)
            .NextBatch()
            .Write(this.truth)
            .EndBatch();
    }

    public Test Read(BatchReader reader)
    {
        name = reader.GetNext().ReadString();
        number = reader.GetNext().ReadInt32();
        truth = reader.GetNext().ReadBoolean();
        return this;
    }

    [ModRPC(13, RpcActors.Host)]
    public static void TestRpc(String message, BatchList<Test> test, Test obj)
    {
        VentLogger.Fatal($"Received: {test.StrJoin()}");
        VentLogger.Fatal($"My Message and object: {message}, {obj}");
    }

    public Test Read(MessageReader reader)
    {
        this.name = reader.ReadString();
        this.number = reader.ReadInt32();
        this.truth = reader.ReadBoolean();
        return this;
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(name);
        writer.Write(number);
        writer.Write(truth);
    }

    public override string ToString()
    {
        return $"{nameof(name)}: {name}, {nameof(number)}: {number}, {nameof(truth)}: {truth}";
    }
}
