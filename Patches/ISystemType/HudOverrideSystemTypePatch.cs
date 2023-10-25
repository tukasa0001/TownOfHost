using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Neutral;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(HudOverrideSystemType), nameof(HudOverrideSystemType.UpdateSystem))]
public static class HudOverrideSystemTypeUpdateSystemPatch
{
    public static bool Prefix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        var newReader = MessageReader.Get(msgReader);
        var amount = newReader.ReadByte();
        var isMadmate =
            player.Is(CustomRoleTypes.Madmate) ||
            // マッド属性化時に削除
            (player.GetRoleClass() is SchrodingerCat schrodingerCat && schrodingerCat.AmMadmate);
        if (isMadmate)
        {
            return !(!Options.MadmateCanFixComms.GetBool() && amount is 0 or 16 or 17);
        }
        return true;
    }
}
