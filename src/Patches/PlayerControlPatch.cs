using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Roles;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
    class CheckProtectPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole() + "=>" + target.GetNameWithRole(), "CheckProtect");
            if (__instance.Is(Sheriff.Ref<Sheriff>()))
            {
                if (__instance.Data.IsDead)
                {
                    Logger.Info("守護をブロックしました。", "CheckProtect");
                    return false;
                }
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static Dictionary<byte, bool> CanReport;
        public static Dictionary<byte, List<GameData.PlayerInfo>> WaitReport = new();
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            if (GameStates.IsMeeting) return false;
            Logger.Info($"{__instance.GetNameWithRole()} => {target?.Object?.GetNameWithRole() ?? "null"}", "ReportDeadBody");
            if (OldOptions.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
            if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek || OldOptions.IsStandardHAS) return false;
            if (!CanReport[__instance.PlayerId])
            {
                WaitReport[__instance.PlayerId].Add(target);
                Logger.Warn($"{__instance.GetNameWithRole()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");
                return false;
            }
            if (!AmongUsClient.Instance.AmHost) return true;
            Main.ArsonistTimer.Clear();
            if (target == null) //ボタン
            {
                if (__instance.Is(CustomRoles.Mayor))
                {
                    Main.MayorUsedButtonCount[__instance.PlayerId] += 1;
                }
            }

            if (OldOptions.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info("最大:" + OldOptions.SyncedButtonCount.GetInt() + ", 現在:" + OldOptions.UsedButtonCount, "ReportDeadBody");
                if (OldOptions.SyncedButtonCount.GetFloat() <= OldOptions.UsedButtonCount)
                {
                    Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    return false;
                }
                else OldOptions.UsedButtonCount++;
                if (OldOptions.SyncedButtonCount.GetFloat() == OldOptions.UsedButtonCount)
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
            PlayerControl.LocalPlayer.RpcSetNameEx(name);
            await System.Threading.Tasks.Task.Delay(time);
            PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    class PlayerStartPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
            roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
            roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            roleText.fontSize -= 1.2f;
            roleText.text = "RoleText";
            roleText.gameObject.name = "RoleText";
            roleText.enabled = false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
    class SetColorPatch
    {
        public static bool IsAntiGlitchDisabled = false;
        public static bool Prefix(PlayerControl __instance, int bodyColor)
        {
            //色変更バグ対策
            if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
            if (AmongUsClient.Instance.IsGameStarted && OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                //ゲーム中に色を変えた場合
                __instance.RpcMurderPlayer(__instance);
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetName))]
    class SetNamePatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] string name)
        {
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class PlayerControlCompleteTaskPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var pc = __instance;
            Logger.Info($"TaskComplete:{pc.PlayerId}", "CompleteTask");
            Main.PlayerStates[pc.PlayerId].UpdateTask(pc);
            Utils.NotifyRoles();
            if ((pc.GetPlayerTaskState().IsTaskFinished &&
                pc.GetCustomRole() is Observer or Doctor) ||
                pc.GetCustomRole() is Speedrunner)
            {
                //ライターもしくはスピードブースターもしくはドクターがいる試合のみタスク終了時にCustomSyncAllSettingsを実行する
                Utils.MarkEveryoneDirtySettings();
            }

        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ProtectPlayer))]
    class PlayerControlProtectPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            Logger.Info($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}", "ProtectPlayer");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RemoveProtection))]
    class PlayerControlRemoveProtectionPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            Logger.Info($"{__instance.GetNameWithRole()}", "RemoveProtection");
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    class PlayerControlSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType)
        {
            var target = __instance;
            Logger.Info($"{__instance.GetNameWithRole()} =>{roleType}", "PlayerControl.RpcSetRole");
            if (!ShipStatus.Instance.enabled) return true;
            if (roleType is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost)
            {
                foreach (var seer in PlayerControl.AllPlayerControls)
                {
                    var self = seer.PlayerId == target.PlayerId;
                    var seerIsKiller = seer.Is(Roles.RoleType.Impostor) || Main.ResetCamPlayerList.Contains(seer.PlayerId);
                    var targetIsKiller = target.Is(Roles.RoleType.Impostor) || Main.ResetCamPlayerList.Contains(target.PlayerId);
                    if ((self && targetIsKiller) || (!seerIsKiller && target.Is(Roles.RoleType.Impostor)))
                    {
                        Logger.Info($"Desync {target.GetNameWithRole()} =>ImpostorGhost for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                        target.RpcSetRoleDesync(RoleTypes.ImpostorGhost, seer.GetClientId());
                    }
                    else
                    {
                        Logger.Info($"Desync {target.GetNameWithRole()} =>CrewmateGhost for{seer.GetNameWithRole()}", "PlayerControl.RpcSetRole");
                        target.RpcSetRoleDesync(RoleTypes.CrewmateGhost, seer.GetClientId());
                    }
                }
                return false;
            }
            return true;
        }
    }
}