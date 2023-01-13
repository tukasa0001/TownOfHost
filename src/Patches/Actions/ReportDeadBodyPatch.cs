using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Gamemodes;
using TownOfHost.Managers;
using TownOfHost.Roles;
using VentLib.Logging;

namespace TownOfHost.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
public class ReportDeadBodyPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
    {
        VentLogger.Old($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
        if (Game.CurrentGamemode.IgnoredActions().HasFlag(GameAction.ReportBody)) return false;
        if (!AmongUsClient.Instance.AmHost) return true;
        if (target == null) return true;
        if (target != null && TOHPlugin.unreportableBodies.Contains(target.PlayerId)) return false;

        ActionHandle handle = ActionHandle.NoInit();
        __instance.Trigger(RoleActionType.SelfReportBody, ref handle, target);
        if (handle.IsCanceled) return false;
        Game.TriggerForAll(RoleActionType.AnyReportedBody, ref handle, __instance, target);
        if (handle.IsCanceled) return false;

        target.PlayerName = Utils.GetPlayerById(target.PlayerId).GetDynamicName().GetName(state: GameState.InIntro);
        Game.RenderAllForAll(state: GameState.InMeeting);
        Game.GetAllPlayers().Do(p => p.GetDynamicName().RenderFor(PlayerControl.LocalPlayer, state: GameState.InIntro));

        return !handle.IsCanceled;
    }
}
