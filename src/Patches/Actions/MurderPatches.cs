using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Gamemodes;
using TOHTOR.Managers.History;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using VentLib.Logging;

namespace TOHTOR.Patches.Actions;

public static class MurderPatches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    public static class CheckMurderPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            var killer = __instance;
            VentLogger.Old($"{killer.GetNameWithRole()} => {target.GetNameWithRole()}", "CheckMurder");
            if (Game.CurrentGamemode.IgnoredActions().HasFlag(GameAction.KillPlayers)) return false;

            //死人はキルできない
            if (killer.Data.IsDead)
            {
                VentLogger.Old($"{killer.GetNameWithRole()}は死亡しているためキャンセルされました。", "CheckMurder");
                return false;
            }

            //不正キル防止処理
            if (target.Data == null || target.inVent || target.inMovingPlat)
            {
                VentLogger.Old("targetは現在キルできない状態です。", "CheckMurder");
                return false;
            }
            if (target.Data.IsDead) //同じtargetへの同時キルをブロック
            {
                VentLogger.Old("targetは既に死んでいたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }
            if (MeetingHud.Instance != null) //会議中でないかの判定
            {
                VentLogger.Old("会議が始まっていたため、キルをキャンセルしました。", "CheckMurder");
                return false;
            }

            if (killer.PlayerId == target.PlayerId) return false;
            ActionHandle handle = ActionHandle.NoInit();
            killer.Trigger(RoleActionType.AttemptKill, ref handle, target);
            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    public class MurderPlayerPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            VentLogger.Old($"{__instance.GetNameWithRole()} => {target.GetNameWithRole()}{(target.protectedByGuardian ? "(Protected)" : "")}", "MurderPlayer");
            Game.GameHistory.AddEvent(new DeathEvent(target, __instance.PlayerId == target.PlayerId ? null : __instance));
        }
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!target.Data.IsDead) return;
            ActionHandle ignored = ActionHandle.NoInit();
            target.Trigger(RoleActionType.MyDeath, ref ignored, __instance);
            Game.TriggerForAll(RoleActionType.AnyDeath, ref ignored, target, __instance);
        }
    }
}