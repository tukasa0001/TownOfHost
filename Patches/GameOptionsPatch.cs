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
            string DisableText = $" ({GetString("Disabled")})";
            if (__instance.Role.Role == RoleTypes.GuardianAngel)
            {
                //+-ボタンを非表示
                var tf = __instance.transform;
                foreach (var button in __instance.GetComponentsInChildren<PassiveButton>())
                {
                    button.gameObject.SetActive(false);
                }

                if (!__instance.titleText.text.Contains(DisableText))
                    __instance.titleText.text += DisableText;
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