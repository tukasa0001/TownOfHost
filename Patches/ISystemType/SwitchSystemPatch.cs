using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;
using UnityEngine;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.UpdateSystem))]
public static class SwitchSystemUpdateSystemPatch
{
    public static bool Prefix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }

        if (!AmongUsClient.Instance.AmHost)
        {
            return true;
        }
        // 停電サボタージュが鳴らされた場合は関係なし(ホスト名義で飛んでくるため誤爆注意)
        if (amount.HasBit(SwitchSystem.DamageSystem))
        {
            return true;
        }
        var isMadmate =
            player.Is(CustomRoleTypes.Madmate) ||
            // マッド属性化時に削除
            (player.GetRoleClass() is SchrodingerCat schrodingerCat && schrodingerCat.AmMadmate);
        if (isMadmate && !Options.MadmateCanFixLightsOut.GetBool())
        {
            // 直せないならキャンセル
            return false;
        }
        //Airshipの特定の停電を直せないならキャンセル
        if ((MapNames)Main.NormalOptions.MapId == MapNames.Airship)
        {
            var truePosition = player.GetTruePosition();
            if (Options.DisableAirshipViewingDeckLightsPanel.GetBool() && Vector2.Distance(truePosition, new(-12.93f, -11.28f)) <= 2f) return false;
            if (Options.DisableAirshipGapRoomLightsPanel.GetBool() && Vector2.Distance(truePosition, new(13.92f, 6.43f)) <= 2f) return false;
            if (Options.DisableAirshipCargoLightsPanel.GetBool() && Vector2.Distance(truePosition, new(30.56f, 2.12f)) <= 2f) return false;
        }

        // サボタージュによる破壊ではない && 配電盤を下げられなくするオプションがオン
        if (Options.BlockDisturbancesToSwitches.GetBool())
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

        // ロールの処理
        if (player.GetRoleClass() is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateSwitchSystem(__instance, amount))
        {
            return false;
        }
        return true;
    }
}
