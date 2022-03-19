using HarmonyLib;
using UnityEngine;
using UnhollowerBaseLib;
namespace TownOfHost
{
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    public static class GameSettingMenuPatch
    {
        public static void Prefix(GameSettingMenu __instance)
        {
            // Unlocks map/impostor amount changing in online (for testing on your custom servers)
            // オンラインモードで部屋を立て直さなくてもマップを変更できるように変更
            __instance.HideForOnline = new Il2CppReferenceArray<Transform>(0);
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Start))]
    [HarmonyPriority(Priority.First)]
    public static class GameOptionsMenuPatch
    {
        public static void Postfix(GameOptionsMenu __instance)
        {
            foreach (var ob in __instance.Children)
            {
                switch (ob.Title)
                {
                    case StringNames.GameShortTasks:
                    case StringNames.GameLongTasks:
                    case StringNames.GameCommonTasks:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 99);
                        break;
                    case StringNames.GameKillCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    case StringNames.GameRecommendedSettings:
                        ob.enabled = false;
                        ob.gameObject.SetActive(false);
                        break;
                    /* case StringNames.GameMapName:
                        var options = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.KeyValuePair<string, int>>();
                        for (int i = 0; i < Constants.MapNames.Length; i++)
                        {
                            var kvp = new Il2CppSystem.Collections.Generic.KeyValuePair<string, int>();
                            kvp.key = Constants.MapNames[i];
                            kvp.value = i;
                            options.Add(kvp);
                        }
                        ob.GetComponent<KeyValueOption>().Values = options;
                        break; */
                    default:
                        break;
                }
            }
        }
    }
}
