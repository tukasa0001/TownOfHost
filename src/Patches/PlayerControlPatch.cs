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


    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class PlayerControlCompleteTaskPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var pc = __instance;
            Logger.Info($"TaskComplete:{pc.PlayerId}", "CompleteTask");
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
                    var seerIsKiller = seer.Is(Roles.RoleType.Impostor) || TOHPlugin.ResetCamPlayerList.Contains(seer.PlayerId);
                    var targetIsKiller = target.Is(Roles.RoleType.Impostor) || TOHPlugin.ResetCamPlayerList.Contains(target.PlayerId);
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