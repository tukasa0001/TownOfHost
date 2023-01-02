using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost.Menus;

[HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.Start))]
public static class RolesSettingsMenuPatch
{
    public static void Postfix(RolesSettingsMenu __instance)
    {
        foreach (var ob in __instance.Children)
        {
            switch (ob.Title)
            {
                case StringNames.EngineerCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.ShapeshifterCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                default:
                    break;
            }
        }
    }
}
[HarmonyPatch(typeof(NormalGameOptionsV07), nameof(NormalGameOptionsV07.SetRecommendations))]
public static class SetRecommendationsPatch
{
    public static bool Prefix(NormalGameOptionsV07 __instance, int numPlayers, bool isOnline)
    {
        numPlayers = Mathf.Clamp(numPlayers, 4, 15);
        __instance.PlayerSpeedMod = __instance.MapId == 4 ? 1.25f : 1f; //AirShipなら1.25、それ以外は1
        __instance.CrewLightMod = 0.5f;
        __instance.ImpostorLightMod = 1.75f;
        __instance.KillCooldown = 25f;
        __instance.NumCommonTasks = 2;
        __instance.NumLongTasks = 4;
        __instance.NumShortTasks = 6;
        __instance.NumEmergencyMeetings = 1;
        if (!isOnline)
            __instance.NumImpostors = NormalGameOptionsV07.RecommendedImpostors[numPlayers];
        __instance.KillDistance = 0;
        __instance.DiscussionTime = 0;
        __instance.VotingTime = 150;
        __instance.IsDefaults = true;
        __instance.ConfirmImpostor = false;
        __instance.VisualTasks = false;

        __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
        __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Shapeshifter);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Scientist);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.GuardianAngel);
        __instance.roleOptions.SetRoleRecommended(RoleTypes.Engineer);

        if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek) //HideAndSeek
        {
            __instance.PlayerSpeedMod = 1.75f;
            __instance.CrewLightMod = 5f;
            __instance.ImpostorLightMod = 0.25f;
            __instance.NumImpostors = 1;
            __instance.NumCommonTasks = 0;
            __instance.NumLongTasks = 0;
            __instance.NumShortTasks = 10;
            __instance.KillCooldown = 10f;
        }
        if (OldOptions.IsStandardHAS) //StandardHAS
        {
            __instance.PlayerSpeedMod = 1.75f;
            __instance.CrewLightMod = 5f;
            __instance.ImpostorLightMod = 0.25f;
            __instance.NumImpostors = 1;
            __instance.NumCommonTasks = 0;
            __instance.NumLongTasks = 0;
            __instance.NumShortTasks = 10;
            __instance.KillCooldown = 10f;
        }
        return false;
    }
}