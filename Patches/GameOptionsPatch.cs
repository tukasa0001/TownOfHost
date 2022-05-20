using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(RoleOptionSetting), nameof(RoleOptionSetting.UpdateValuesAndText))]
    class ChanceChangePatch
    {
        public static void Postfix(RoleOptionSetting __instance)
        {
            bool forced = false;
            if (__instance.Role.Role == RoleTypes.Scientist)
            {
                if (CustomRoles.Doctor.IsEnable()) forced = true;
            }
            if (__instance.Role.Role == RoleTypes.Engineer)
            {
                if (CustomRoles.Madmate.IsEnable() || CustomRoles.Terrorist.IsEnable()) forced = true;
            }
            if (__instance.Role.Role == RoleTypes.Shapeshifter)
            {
                if (CustomRoles.Mafia.IsEnable() || CustomRoles.SerialKiller.IsEnable() || CustomRoles.Warlock.IsEnable() || CustomRoles.BountyHunter.IsEnable() || CustomRoles.ShapeMaster.IsEnable()) forced = true;
            }

            if (forced)
            {
                ((TMPro.TMP_Text)__instance.ChanceText).text = "Always";
            }
        }
    }
}