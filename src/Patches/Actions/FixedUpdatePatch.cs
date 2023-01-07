using TownOfHost.ReduxOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Roles;

namespace TownOfHost.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class FixedUpdatePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        DisplayModVersion(__instance);
        if (!AmongUsClient.Instance.AmHost) return;

        var player = __instance;
        ActionHandle handle = null;
        Game.RenderAllNames();
        Game.RenderAllForAll(state: GameState.Roaming);
        Game.TriggerForAll(RoleActionType.FixedUpdate, ref handle);


        if (Game.State is not GameState.Roaming) return; //実行クライアントがホストの場合のみ実行

        if (ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
        {
            var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
            ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
            Logger.Info($"{__instance.GetNameWithRole()}:通報可能になったため通報処理を行います", "ReportDeadbody");
            __instance.ReportDeadBody(info);
        }

        if (player.IsAlive() && StaticOptions.LadderDeath) FallFromLadder.FixedUpdate(player);
        if (player == PlayerControl.LocalPlayer) DisableDevice.FixedUpdate();
    }

    private static void DisplayModVersion(PlayerControl player)
    {
        if (Game.State is not GameState.InLobby) return;
        if (!TOHPlugin.playerVersion.TryGetValue(player.PlayerId, out var ver)) return;
        if (TOHPlugin.ForkId != ver.forkId) // フォークIDが違う場合
            player.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{player?.name}</color>";
        else if (TOHPlugin.version.CompareTo(ver.version) == 0)
            player.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{player.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{player?.name}</color>";
        else player.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{player?.name}</color>";
    }
}