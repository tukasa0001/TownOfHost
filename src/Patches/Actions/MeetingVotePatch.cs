using System.Linq;
using HarmonyLib;
using Hazel;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using VentLib.Logging;
using VentLib.Utilities;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Patches.Actions;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
public class MeetingVotePatch
{

    public static void Postfix(MeetingHud __instance, byte srcPlayerId, byte suspectPlayerId)
    {
        PlayerControl voter = Utils.GetPlayerById(srcPlayerId)!;
        Optional<PlayerControl> voted = Optional<PlayerControl>.Of(Utils.GetPlayerById(suspectPlayerId));

        ActionHandle handle = ActionHandle.NoInit();
        voter.Trigger(RoleActionType.MyVote, ref handle, voted);
        Game.TriggerForAll(RoleActionType.AnyVote, ref handle, voter, voted);

        VentLogger.High($"Canceled: {handle.IsCanceled}");
        if (!handle.IsCanceled) return;
        Async.Schedule(() => ClearVote(__instance, voter), NetUtils.DeriveDelay(0.4f));
        Async.Schedule(() => ClearVote(__instance, voter), NetUtils.DeriveDelay(0.6f));
    }

    private static void ClearVote(MeetingHud hud, PlayerControl target)
    {
        VentLogger.Trace($"Clearing vote for: {target.GetNameWithRole()}");
        PlayerVoteArea voteArea = hud.playerStates.ToArray().FirstOrDefault(state => state.TargetPlayerId == target.PlayerId)!;
        voteArea.UnsetVote();
        MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
        writer.StartMessage(6);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.WritePacked(target.GetClientId());
        {
            writer.StartMessage(2);
            writer.WritePacked(hud.NetId);
            writer.WritePacked((uint)RpcCalls.ClearVote);
        }

        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
}