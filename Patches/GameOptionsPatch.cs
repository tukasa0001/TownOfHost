using AmongUs.GameOptions;
using HarmonyLib;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(RoleOptionSetting), nameof(RoleOptionSetting.UpdateValuesAndText))]
    class ChanceChangePatch
    {
        public static void Postfix(RoleOptionSetting __instance)
        {
            // The Phantom does not work together with desynchronized impostor roles e.g. Sheriff so we need to disable it.
            // This may be removed in the future when we have implemented changing vanilla role or some other stuff.
            if (__instance.Role.Role is RoleTypes.GuardianAngel || (__instance.Role.Role is RoleTypes.Phantom && !DebugModeManager.IsDebugMode))
            {
                string disableText = $" ({GetString("Disabled")})";
                //+-ボタンを非表示
                foreach (var button in __instance.GetComponentsInChildren<PassiveButton>())
                {
                    button.gameObject.SetActive(false);
                }

                if (!__instance.titleText.text.Contains(disableText))
                    __instance.titleText.text += disableText;
                if (__instance.roleChance != 0 || __instance.roleMaxCount != 0)
                {
                    __instance.roleChance = 0;
                    __instance.roleMaxCount = 0;
                    __instance.OnValueChanged.Invoke(__instance);
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameOptionsManager), nameof(GameOptionsManager.SwitchGameMode))]
    class SwitchGameModePatch
    {
        public static void Postfix(GameModes gameMode)
        {
            if (gameMode == GameModes.HideNSeek)
            {
                ErrorText.Instance.HnSFlag = true;
                ErrorText.Instance.AddError(ErrorCode.HnsUnload);
                Harmony.UnpatchAll();
                Main.Instance.Unload();
            }
        }
    }
}