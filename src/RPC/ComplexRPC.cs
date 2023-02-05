using TownOfHost.Managers;
using VentLib.RPC;
using VentLib.Utilities;

namespace TownOfHost.RPC;

public static class ComplexRPC
{
    public static void ComplexVotingComplete(this MeetingHud meetingHud, MeetingHud.VoterState[] states, GameData.PlayerInfo? exiled, bool tie)
    {
        int fakeClientId = AntiBlackout.FakeExiled?.Object != null ? AntiBlackout.FakeExiled.Object.GetClientId() : 999;
        if (fakeClientId == 999)
            meetingHud.VotingComplete(states, exiled, tie);

        if (AmongUsClient.Instance.AmClient)
            meetingHud.VotingComplete(states, exiled, tie);

        RpcV2 rpcV2Normal = RpcV2.Immediate(meetingHud.NetId, 23).WritePacked(states.Length);
        RpcV2 rpcV2Exeption = RpcV2.Immediate(meetingHud.NetId, 23).WritePacked(states.Length);

        foreach (MeetingHud.VoterState state in states)
        {
            rpcV2Normal.WriteSerializable(state);
            rpcV2Exeption.WriteSerializable(state);
        }

        rpcV2Normal.Write(exiled?.PlayerId ?? byte.MaxValue).Write(tie);
        rpcV2Exeption.Write(byte.MaxValue).Write(tie);

        rpcV2Normal.SendExclusive(fakeClientId);
        rpcV2Exeption.Send(fakeClientId);
    }
}