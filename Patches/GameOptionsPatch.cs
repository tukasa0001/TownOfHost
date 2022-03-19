using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(RoleOptionSetting), nameof(RoleOptionSetting.UpdateValuesAndText))]
    class ChanceChangePatch
    {
        public static void Postfix(RoleOptionSetting __instance)
        {
            bool forced = false;
            if (__instance.Role.Role == RoleTypes.Engineer)
            {
                if (CustomRoles.Madmate.isEnable() || CustomRoles.Terrorist.isEnable()) forced = true;
            }
            if (__instance.Role.Role == RoleTypes.Shapeshifter)
            {
                if (CustomRoles.Mafia.isEnable() || CustomRoles.SerialKiller.isEnable() || CustomRoles.Warlock.isEnable() || CustomRoles.BountyHunter.isEnable() || CustomRoles.ShapeMaster.isEnable()) forced = true;
            }

            if (forced)
            {
                ((TMPro.TMP_Text)__instance.ChanceText).text = "Always";
            }
        }
    }
}
