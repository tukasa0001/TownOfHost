using System;
using HarmonyLib;
using TownOfHost.API;
using TownOfHost.Extensions;
using VentLib.Logging;
using VentLib.RPC;
using VentLib.Utilities;

namespace TownOfHost.Patches.Actions;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
public class MeetingVotePatch
{
    public static void Postfix(MeetingHud __instance)
    {
        PlayerControl player = Game.GetAllPlayers().Random(p => !p.IsHost());
        VentLogger.Fatal($"Calling meeting for: {player.GetRawName()}");
        PlayerControl.LocalPlayer.RpcSetName("DUMB PERSON!!");
        Async.Schedule(() => RpcV2.Immediate(player.PlayerId, RpcCalls.StartMeeting).Write(Byte.MaxValue).Send(), 1f);
    }

}