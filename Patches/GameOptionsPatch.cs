using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(RoleOptionSetting), nameof(RoleOptionSetting.UpdateValuesAndText))]
    class ChanceChangePatch {
        public static void Postfix(RoleOptionSetting __instance) {
            bool forced = false;
            if(__instance.Role.Role == RoleTypes.Engineer) {
                if(main.MadmateCount > 0 || main.TerroristCount > 0) forced = true;
            }
            if(__instance.Role.Role == RoleTypes.Shapeshifter) {
                if(main.MafiaCount > 0 || main.SerialKillerCount > 0 || main.WarlockCount > 0 || main.BountyHunterCount > 0 || main.ShapeMasterCount > 0) forced = true;
            }

            if(forced) {
                ((TMPro.TMP_Text)__instance.ChanceText).text = "Always";
            }
        }
    }
}
