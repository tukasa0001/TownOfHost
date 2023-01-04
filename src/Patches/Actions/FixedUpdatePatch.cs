using TownOfHost.ReduxOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Roles;

namespace TownOfHost.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class FixedUpdatePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        var player = __instance;
        ActionHandle handle = null;
        Game.RenderAllNames();
        Game.TriggerForAll(RoleActionType.FixedUpdate, ref handle);


        if (AmongUsClient.Instance.AmHost && Game.State is GameState.Roaming)
        {//実行クライアントがホストの場合のみ実行

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

        if (Game.State is not GameState.InLobby) return;
        if (!TOHPlugin.playerVersion.TryGetValue(__instance.PlayerId, out var ver)) return;
        if (TOHPlugin.ForkId != ver.forkId) // フォークIDが違う場合
            __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
        else if (TOHPlugin.version.CompareTo(ver.version) == 0)
            __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<color=#87cefa>{__instance.name}</color>" : $"<color=#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
        else __instance.cosmetics.nameText.text = $"<color=#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
    }

    //FIXME: 役職クラス化のタイミングで、このメソッドは移動予定
    public static void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoleManager.Static.Lovers.IsEnable() && TOHPlugin.isLoversDead == false)
        {
            foreach (var loversPlayer in TOHPlugin.LoversPlayers)
            {
                //生きていて死ぬ予定でなければスキップ
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                TOHPlugin.isLoversDead = true;
                foreach (var partnerPlayer in TOHPlugin.LoversPlayers)
                {
                    //本人ならスキップ
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                    //残った恋人を全て殺す(2人以上可)
                    //生きていて死ぬ予定もない場合は心中
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        TOHPlugin.PlayerStates[partnerPlayer.PlayerId].deathReason = PlayerStateOLD.DeathReason.FollowingSuicide;
                        if (isExiled)
                            CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(partnerPlayer.PlayerId, PlayerStateOLD.DeathReason.FollowingSuicide);
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer);
                    }
                }
            }
        }
    }
}