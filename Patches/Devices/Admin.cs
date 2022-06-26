using HarmonyLib;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public class AdminPatch
    {
        //参考元 : https://github.com/yukinogatari/TheOtherRoles-GM/blob/gm-main/TheOtherRoles/Patches/AdminPatch.cs
        public static bool DisableAdmin => DisableAllAdmins || DisableArchiveAdmin;
        public static bool DisableAllAdmins => Options.DisableAdmin.GetBool() && Options.WhichDisableAdmin.GetString() == GetString(Options.whichDisableAdmin[0]);
        public static bool DisableArchiveAdmin => Options.DisableAdmin.GetBool() && PlayerControl.GameOptions.MapId == 4 && Options.WhichDisableAdmin.GetString() == GetString(Options.whichDisableAdmin[1]);
        public static Vector2 ArchiveAdminPos = new(20.0f, 12.3f);
        [HarmonyPatch(typeof(MapConsole), nameof(MapConsole.Use))]
        public static class MapConsoleUsePatch
        {
            public static bool Prefix()
            {
                bool isGuard = false;
                if (DisableAdmin)
                {
                    var PlayerPos = PlayerControl.LocalPlayer.GetTruePosition();
                    if (DisableAllAdmins)
                    {
                        var AdminDistance = Vector2.Distance(PlayerPos, DisableDevice.GetAdminTransform());
                        isGuard = AdminDistance <= DisableDevice.UsableDistance();

                        if (PlayerControl.GameOptions.MapId == 2) //Polus用のアドミンチェック。Polusはアドミンが2つあるから
                        {
                            var SecondaryPolusAdminDistance = Vector2.Distance(PlayerPos, new Vector2(24.66107f, -21.523f));
                            isGuard = SecondaryPolusAdminDistance <= DisableDevice.UsableDistance();
                        }
                    }

                    if (DisableAllAdmins || DisableArchiveAdmin) //憎きアーカイブのアドミンチェック
                    {
                        var ArchiveAdminDistance = Vector2.Distance(PlayerPos, ArchiveAdminPos);
                        isGuard = ArchiveAdminDistance <= DisableDevice.UsableDistance();
                    }
                }

                return isGuard;
            }
        }
    }
}