using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Gamemodes;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using VentLib.Logging;
using VentLib.Utilities;

namespace TOHTOR.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
public class ReportDeadBodyPatch
{
    private static bool alreadyReported;

    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
    {
        if (alreadyReported)
        {
            alreadyReported = false;
            return true;
        }
        VentLogger.Old($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
        if (Game.CurrentGamemode.IgnoredActions().HasFlag(GameAction.ReportBody) && target != null) return false;
        if (Game.CurrentGamemode.IgnoredActions().HasFlag(GameAction.CallMeeting) && target == null) return false;
        if (!AmongUsClient.Instance.AmHost) return true;
        if (target == null) return true;
        if (Game.GameStates.UnreportableBodies.Contains(target.PlayerId)) return false;

        ActionHandle handle = ActionHandle.NoInit();
        __instance.Trigger(RoleActionType.SelfReportBody, ref handle, target);
        if (handle.IsCanceled) return false;
        Game.TriggerForAll(RoleActionType.AnyReportedBody, ref handle, __instance, target);
        if (handle.IsCanceled) return false;

        target.PlayerName = Utils.GetPlayerById(target.PlayerId)!.GetDynamicName().GetName(state: GameState.InIntro);
        Game.State = GameState.InMeeting;
        Game.RenderAllForAll(state: GameState.InMeeting, force: true);
        Game.GetAllPlayers().Do(p => p.GetDynamicName().RenderFor(PlayerControl.LocalPlayer, state: GameState.InMeeting, force: true));

        Async.Schedule(() => __instance.CmdReportDeadBody(target), 0.3f);

        alreadyReported = true;
        return false;
    }
}
