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
                __instance.TitleText.color = Utils.GetRoleColor(CustomRoles.Scientist);
                if (CustomRoles.Doctor.IsEnable()) forced = true;
            }
            if (__instance.Role.Role == RoleTypes.Engineer)
            {
                __instance.TitleText.color = Utils.GetRoleColor(CustomRoles.Engineer);
                if (CustomRoles.Madmate.IsEnable() || CustomRoles.Terrorist.IsEnable()) forced = true;
            }
            if (__instance.Role.Role == RoleTypes.GuardianAngel)
            {
                __instance.TitleText.color = Utils.GetRoleColor(CustomRoles.GuardianAngel);
            }
            if (__instance.Role.Role == RoleTypes.Shapeshifter)
            {
                __instance.TitleText.color = Utils.GetRoleColor(CustomRoles.Shapeshifter);
                if (CustomRoles.SerialKiller.IsEnable() || CustomRoles.Warlock.IsEnable() || CustomRoles.BountyHunter.IsEnable() || CustomRoles.ShapeMaster.IsEnable()) forced = true;
            }

            if (forced)
            {
                ((TMPro.TMP_Text)__instance.ChanceText).text = "Always";
            }
        }
    }
}