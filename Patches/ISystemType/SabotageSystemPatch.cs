using HarmonyLib;
using Hazel;
using TownOfHost.Attributes;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost
{
    [HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.UpdateSystem))]
    public static class SabotageSystemTypeUpdateSystemPatch
    {
        private static bool isCooldownModificationEnabled;
        private static float modifiedCooldownSec;
        private static readonly LogHandler logger = Logger.Handler(nameof(SabotageSystemType));

        [GameModuleInitializer]
        public static void Initialize()
        {
            isCooldownModificationEnabled = Options.ModifySabotageCooldown.GetBool();
            modifiedCooldownSec = Options.SabotageCooldown.GetFloat();
        }

        public static bool Prefix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
        {
            var newReader = MessageReader.Get(msgReader);
            var amount = newReader.ReadByte();
            var nextSabotage = (SystemTypes)amount;
            logger.Info($"PlayerName: {player.GetNameWithRole()}, SabotageType: {nextSabotage}");

            //HASモードではサボタージュ不可
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) return false;
            var roleClass = player.GetRoleClass();
            if (roleClass is IKiller killer)
            {
                //そもそもサボタージュボタン使用不可ならサボタージュ不可
                if (!killer.CanUseSabotageButton()) return false;
                //その他処理が必要であれば処理
                return roleClass.OnInvokeSabotage(nextSabotage);
            }
            else
            {
                return CanSabotage(player);
            }
        }
        private static bool CanSabotage(PlayerControl player)
        {
            //サボタージュ出来ないキラー役職はサボタージュ自体をキャンセル
            if (!player.Is(CustomRoleTypes.Impostor))
            {
                return false;
            }
            return true;
        }
        public static void Postfix(SabotageSystemType __instance)
        {
            if (!isCooldownModificationEnabled || !AmongUsClient.Instance.AmHost)
            {
                return;
            }
            __instance.Timer = modifiedCooldownSec;
            __instance.IsDirty = true;
        }
    }

    [HarmonyPatch(typeof(ElectricTask), nameof(ElectricTask.Initialize))]
    public static class ElectricTaskInitializePatch
    {
        public static void Postfix()
        {
            Utils.MarkEveryoneDirtySettings();
            if (!GameStates.IsMeeting)
                Utils.NotifyRoles(ForceLoop: true);
        }
    }
    [HarmonyPatch(typeof(ElectricTask), nameof(ElectricTask.Complete))]
    public static class ElectricTaskCompletePatch
    {
        public static void Postfix()
        {
            Utils.MarkEveryoneDirtySettings();
            if (!GameStates.IsMeeting)
                Utils.NotifyRoles(ForceLoop: true);
        }
    }

}