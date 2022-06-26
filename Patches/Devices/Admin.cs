using HarmonyLib;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public class AdminPatch
    {
        //参考元 : https://github.com/yukinogatari/TheOtherRoles-GM/blob/gm-main/TheOtherRoles/Patches/AdminPatch.cs
        public static bool DisableAdmin = DisableAllAdmins || DisableArchiveAdmin;
        public static bool DisableAllAdmins => Options.DisableAdmin.GetBool() && Options.WhichDisableAdmin.GetString() == GetString(Options.whichDisableAdmin[0]);
        public static bool DisableArchiveAdmin = Options.DisableAdmin.GetBool() && PlayerControl.GameOptions.MapId != 4 && Options.WhichDisableAdmin.GetString() == GetString(Options.whichDisableAdmin[1]);
        public static Vector2 ArchiveAdminPos = new(20.0f, 12.3f);
        [HarmonyPatch(typeof(MapConsole), nameof(MapConsole.Use))]
        public static class MapConsoleUsePatch
        {
            public static bool Prefix()
            {
                var PlayerPos = PlayerControl.LocalPlayer.GetTruePosition();
                var AdminDistance = Vector2.Distance(PlayerPos, DisableDevice.GetAdminTransform());
                if (DisableAdmin)
                    return AdminDistance <= DisableDevice.UsableDistance(PlayerControl.GameOptions.MapId);

                else if (DisableArchiveAdmin)
                {
                    var ArchiveAdminDistance = Vector2.Distance(PlayerPos, new Vector2(24.66107f, -21.523f));
                    return ArchiveAdminDistance <= DisableDevice.UsableDistance(PlayerControl.GameOptions.MapId);
                }

                return true;
            }
        }
    }
}