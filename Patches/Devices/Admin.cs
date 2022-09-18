using HarmonyLib;
using UnityEngine;
using static TownOfHost.Translator;

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
                if (PlayerControl.LocalPlayer.IsAlive())
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