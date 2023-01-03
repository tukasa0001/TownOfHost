using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.ReduxOptions;
using TownOfHost.Extensions;
using TownOfHost.Roles;

namespace TownOfHost.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
public class ReportDeadBodyPatch
{
    public static Dictionary<byte, bool> CanReport;
    public static Dictionary<byte, List<GameData.PlayerInfo>> WaitReport = new();
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
    {
        if (GameStates.IsMeeting) return false;
        Logger.Info($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
        if (StaticOptions.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
        if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek || StaticOptions.IsStandardHAS) return false;
        if (!CanReport[__instance.PlayerId])
        {
            WaitReport[__instance.PlayerId].Add(target);
            Logger.Warn($"{__instance.GetNameWithRole()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");
            return false;
        }
        if (!AmongUsClient.Instance.AmHost) return true;

        ActionHandle handle = ActionHandle.NoInit();
        __instance.Trigger(RoleActionType.SelfReportBody, ref handle, target);
        if (handle.IsCanceled) return false;
        Game.TriggerForAll(RoleActionType.AnyReportedBody, ref handle, __instance, target);
        if (handle.IsCanceled) return false;

        if (target == null) //ボタン
        {
            if (__instance.Is(CustomRoles.Mayor))
            {
                Main.MayorUsedButtonCount[__instance.PlayerId] += 1;
            }
        }

        if (StaticOptions.SyncButtonMode && target == null)
        {
            Logger.Info("最大:" + StaticOptions.SyncedButtonCount + ", 現在:" + OldOptions.UsedButtonCount, "ReportDeadBody");
            if (StaticOptions.SyncedButtonCount <= OldOptions.UsedButtonCount)
            {
                Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                return false;
            }
            else OldOptions.UsedButtonCount++;
            if (StaticOptions.SyncedButtonCount == OldOptions.UsedButtonCount)
            {
                Logger.Info("使用可能ボタン回数が最大数に達しました。", "ReportDeadBody");
            }
        }

        foreach (var bp in Main.BitPlayers)
        {
            var vampireID = bp.Value.Item1;
            var bitten = Utils.GetPlayerById(bp.Key);

            if (bitten != null && !bitten.Data.IsDead)
            {
                Main.PlayerStates[bitten.PlayerId].deathReason = PlayerStateOLD.DeathReason.Bite;
                /*bitten.SetRealKiller(Utils.GetPlayerById(vampireID));*/
                //Protectは強制的にはがす
                if (bitten.protectedByGuardian)
                    bitten.RpcMurderPlayer(bitten);
                bitten.RpcMurderPlayer(bitten);
                OldRPC.PlaySoundRPC(vampireID, Sounds.KillSound);
                Logger.Info("Vampireに噛まれている" + bitten?.Data?.PlayerName + "を自爆させました。", "ReportDeadBody");
            }
            else
                Logger.Info("Vampireに噛まれている" + bitten?.Data?.PlayerName + "はすでに死んでいました。", "ReportDeadBody");
        }
        Main.BitPlayers = new Dictionary<byte, (byte, float)>();
        Main.PuppeteerList.Clear();

        if (__instance.Data.IsDead) return true;
        //=============================================
        //以下、ボタンが押されることが確定したものとする。
        //=============================================


        PlayerControl.AllPlayerControls.ToArray()
            .Where(pc => Main.CheckShapeshift.ContainsKey(pc.PlayerId))
            .Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true));

        Utils.MarkEveryoneDirtySettings();
        return true;
    }
    public static async void ChangeLocalNameAndRevert(string name, int time)
    {
        //async Taskじゃ警告出るから仕方ないよね。
        var revertName = PlayerControl.LocalPlayer.name;
        await System.Threading.Tasks.Task.Delay(time);
    }
}
