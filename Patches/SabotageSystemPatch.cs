using HarmonyLib;
using TownOfHost.Attributes;

namespace TownOfHost
{
    //参考
    //https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/MeltDownBoost.cs

    [HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.Deteriorate))]
    public static class ReactorSystemTypePatch
    {
        public static void Prefix(ReactorSystemType __instance)
        {
            if (!__instance.IsActive || !Options.SabotageTimeControl.GetBool())
                return;
            if (ShipStatus.Instance.Type == ShipStatus.MapType.Pb)
            {
                if (__instance.Countdown >= Options.PolusReactorTimeLimit.GetFloat())
                    __instance.Countdown = Options.PolusReactorTimeLimit.GetFloat();
                return;
            }
            return;
        }
    }
    [HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.Deteriorate))]
    public static class HeliSabotageSystemPatch
    {
        public static void Prefix(HeliSabotageSystem __instance)
        {
            if (!__instance.IsActive || !Options.SabotageTimeControl.GetBool())
                return;
            if (AirshipStatus.Instance != null)
                if (__instance.Countdown >= Options.AirshipReactorTimeLimit.GetFloat())
                    __instance.Countdown = Options.AirshipReactorTimeLimit.GetFloat();
        }
    }
    [HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.RepairDamage))]
    public static class SwitchSystemRepairDamagePatch
    {
        public static bool Prefix(SwitchSystem __instance, [HarmonyArgument(1)] byte amount)
        {
            if (!AmongUsClient.Instance.AmHost)
            {
                return true;
            }

            // サボタージュによる破壊ではない && 配電盤を下げられなくするオプションがオン
            if (!amount.HasBit(SwitchSystem.DamageSystem) && Options.BlockDisturbancesToSwitches.GetBool())
            {
                // amount分だけ1を左にずらす
                // 各桁が各ツマミに対応する
                // 一番左のツマミが操作されたら(amount: 0) 00001
                // 一番右のツマミが操作されたら(amount: 4) 10000
                // ref: SwitchSystem.RepairDamage, SwitchMinigame.FixedUpdate
                var switchedKnob = (byte)(0b_00001 << amount);
                // ExpectedSwitches: すべてONになっているときのスイッチの上下状態
                // ActualSwitches: 実際のスイッチの上下状態
                // 操作されたツマミについて，ExpectedとActualで同じならそのツマミは既に直ってる
                if ((__instance.ActualSwitches & switchedKnob) == (__instance.ExpectedSwitches & switchedKnob))
                {
                    return false;
                }
            }
            return true;
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

    // サボタージュを発生させたときに呼び出されるメソッド
    [HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.RepairDamage))]
    public static class SabotageSystemTypeRepairDamagePatch
    {
        private static bool isCooldownModificationEnabled;
        private static float modifiedCooldownSec;

        [GameModuleInitializer]
        public static void Initialize()
        {
            isCooldownModificationEnabled = Options.ModifySabotageCooldown.GetBool();
            modifiedCooldownSec = Options.SabotageCooldown.GetFloat();
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
}