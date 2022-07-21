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
        public static Vector2 SecondaryPolusAdminPos = new(24.66107f, -21.523f);
        private static TMPro.TextMeshPro DisabledText;
        [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
        public static class MapCountOverlayUpdatePatch
        {
            public static bool Prefix(MapCountOverlay __instance)
            {
                if (DisabledText == null)
                    DisabledText = Object.Instantiate(__instance.SabotageText, __instance.SabotageText.transform.parent);
                bool isGuard = false;

                DisabledText.gameObject.SetActive(false);
                if (!PlayerControl.LocalPlayer.IsAlive())
                {
                    if (DisableAdmin)
                    {
                        var PlayerPos = PlayerControl.LocalPlayer.GetTruePosition();
                        if (DisableAllAdmins)
                        {
                            var AdminDistance = Vector2.Distance(PlayerPos, DisableDevice.GetAdminTransform());
                            isGuard = AdminDistance <= DisableDevice.UsableDistance();

                            if (!isGuard && PlayerControl.GameOptions.MapId == 2) //Polus用のアドミンチェック。Polusはアドミンが2つあるから
                            {
                                var SecondaryPolusAdminDistance = Vector2.Distance(PlayerPos, SecondaryPolusAdminPos);
                                isGuard = SecondaryPolusAdminDistance <= DisableDevice.UsableDistance();
                            }
                        }

                        if (!isGuard && (DisableAllAdmins || DisableArchiveAdmin)) //憎きアーカイブのアドミンチェック
                        {
                            var ArchiveAdminDistance = Vector2.Distance(PlayerPos, ArchiveAdminPos);
                            isGuard = ArchiveAdminDistance <= DisableDevice.UsableDistance();
                        }
                    }
                    if (Options.StandardHAS.GetBool()) isGuard = true;
                }
                if (isGuard)
                {
                    __instance.SabotageText.gameObject.SetActive(false);

                    __instance.BackgroundColor.SetColor(Palette.DisabledGrey);
                    __instance.SabotageText.gameObject.SetActive(false);
                    DisabledText.gameObject.SetActive(true);
                    DisabledText.text = GetString("DisabledBySettings");
                    return false;
                }

                return true;
            }
        }
    }
}