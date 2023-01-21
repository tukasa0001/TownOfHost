using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace TownOfHost
{
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    class OptionsMenuBehaviourStartPatch
    {
        private static Vector3? origin;
        private static ToggleButtonBehaviour ForceJapanese;
        private static ToggleButtonBehaviour JapaneseRoleName;
        public static float xOffset = 1.75f;
        public static float yOffset = -0.25f;
        private static void UpdateToggle(ToggleButtonBehaviour button, string text, bool on)
        {
            if (button == null || button.gameObject == null) return;

            Color color = on ? new Color(0f, 1f, 0.16470589f, 1f) : Color.white;
            button.Background.color = color;
            button.Text.text = $"{text}{(on ? "On" : "Off")}";
            if (button.Rollover) button.Rollover.ChangeOutColor(color);
        }
        private static ToggleButtonBehaviour CreateCustomToggle(string text, bool on, Vector3 offset, UnityEngine.Events.UnityAction onClick, OptionsMenuBehaviour __instance)
        {
            if (__instance.CensorChatButton != null)
            {
                var button = UnityEngine.Object.Instantiate(__instance.CensorChatButton, __instance.CensorChatButton.transform.parent);
                button.transform.localPosition = (origin ?? Vector3.zero) + offset;
                PassiveButton passiveButton = button.GetComponent<PassiveButton>();
                passiveButton.OnClick = new Button.ButtonClickedEvent();
                passiveButton.OnClick.AddListener(onClick);
                UpdateToggle(button, text, on);

                return button;
            }
            return null;
        }

        public static void Postfix(OptionsMenuBehaviour __instance)
        {
            if (__instance.CensorChatButton != null)
            {
                if (origin == null) origin = __instance.CensorChatButton.transform.localPosition;
                __instance.CensorChatButton.transform.localPosition = origin.Value + Vector3.left * 0.375f + Vector3.up * 0.08f;
                __instance.CensorChatButton.transform.localScale = Vector3.one * 0.7f;
            }
            if (__instance.EnableFriendInvitesButton != null)
            {
                if (origin == null) origin = __instance.EnableFriendInvitesButton.transform.localPosition;
                __instance.EnableFriendInvitesButton.transform.localPosition = origin.Value + Vector3.right * 3.125f + Vector3.up * 0.08f;
                __instance.EnableFriendInvitesButton.transform.localScale = Vector3.one * 0.7f;
            }
            if (__instance.ColorBlindButton != null)
            {
                if (origin == null) origin = __instance.ColorBlindButton.transform.localPosition;
                __instance.ColorBlindButton.transform.localPosition = origin.Value + Vector3.right * 3.125f + Vector3.up * 0.74f;
                __instance.ColorBlindButton.transform.localScale = Vector3.one * 0.7f;
            }
            if (__instance.StreamerModeButton != null)
            {
                if (origin == null) origin = __instance.StreamerModeButton.transform.localPosition;
                __instance.StreamerModeButton.transform.localPosition = origin.Value + Vector3.right * 1.375f + Vector3.up * 1.071f;
                __instance.StreamerModeButton.transform.localScale = Vector3.one * 0.7f;
            }

            if (ForceJapanese == null || ForceJapanese?.gameObject == null)
            {
                ForceJapanese = CreateCustomToggle("Force Japanese: ", Main.ForceJapanese.Value, new Vector3(-0.375f, yOffset, 0), (UnityEngine.Events.UnityAction)ForceJapaneseButtonToggle, __instance);

                void ForceJapaneseButtonToggle()
                {
                    Main.ForceJapanese.Value = !Main.ForceJapanese.Value;
                    UpdateToggle(ForceJapanese, "Force Japanese: ", Main.ForceJapanese.Value);
                }
            }
            if (JapaneseRoleName == null || JapaneseRoleName.gameObject == null)
            {
                JapaneseRoleName = CreateCustomToggle("Japanese Role Name: ", Main.JapaneseRoleName.Value, new Vector3(1.375f, yOffset, 0), (UnityEngine.Events.UnityAction)LangModeButtonToggle, __instance);

                void LangModeButtonToggle()
                {
                    Main.JapaneseRoleName.Value = !Main.JapaneseRoleName.Value;
                    UpdateToggle(JapaneseRoleName, "Japanese Role Name: ", Main.JapaneseRoleName.Value);
                }
            }
        }
    }
}