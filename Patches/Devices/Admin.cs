using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    public class AdminPatch
    {
        //参考元 : https://github.com/yukinogatari/TheOtherRoles-GM/blob/gm-main/TheOtherRoles/Patches/AdminPatch.cs
        public static bool DisableAdmin => PlayerControl.GameOptions.MapId switch
        {
            0 => Options.DisableSkeldAdmin.GetBool(),
            1 => Options.DisableMiraHQAdmin.GetBool(),
            2 => Options.DisablePolusAdmin.GetBool(),
            4 => Options.DisableAirshipCockpitAdmin.GetBool() || Options.DisableAirshipRecordsAdmin.GetBool(),
            _ => false
        };
        public static readonly Dictionary<string, Vector2> AdminPos = new()
        {
            ["SkeldAdmin"] = new(3.48f, -8.62f),
            ["MiraHQAdmin"] = new(21.02f, 19.09f),
            ["PolusLeftAdmin"] = new(23.14f, -21.52f),
            ["PolusRightAdmin"] = new(24.66f, -21.52f),
            ["AirshipCockpitAdmin"] = new(-22.32f, 0.91f),
            ["AirshipRecordsAdmin"] = new(19.89f, 12.60f)
        };
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    public class RemoveAdminPatch
    {
        public static void Postfix()
        {
            if (!AdminPatch.DisableAdmin) return;
            var map = GameObject.FindObjectsOfType<MapConsole>();
            if (map == null) return;
            switch (PlayerControl.GameOptions.MapId)
            {
                case 0:
                case 1:
                    map[0].gameObject.GetComponent<CircleCollider2D>().enabled = false;
                    break;
                case 2:
                    map.Do(x => x.gameObject.GetComponent<BoxCollider2D>().enabled = false);
                    break;
                case 4:
                    map.Do(x =>
                    {
                        if (Options.DisableAirshipCockpitAdmin.GetBool() && x.name == "panel_cockpit_map" ||
                            Options.DisableAirshipRecordsAdmin.GetBool() && x.name == "records_admin_map")
                            x.gameObject.GetComponent<BoxCollider2D>().enabled = false;
                    });
                    break;
            }
        }
    }
}