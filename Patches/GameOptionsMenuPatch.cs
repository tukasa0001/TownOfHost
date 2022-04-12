using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnhollowerBaseLib;
using Object = UnityEngine.Object;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.InitializeOptions))]
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
        private const string TownOfHostObjectName = "TOHSettings";

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

            if (GameObject.Find(TownOfHostObjectName) != null)
            {
                GameObject.Find(TownOfHostObjectName)
                    .transform
                    .FindChild("GameGroup")
                    .FindChild("Text")
                    .GetComponent<TMPro.TextMeshPro>()
                    .SetText("TownOfHost Settings");

                return;
            }

            var template = Object.FindObjectsOfType<StringOption>().FirstOrDefault();
            if (template == null) return;

            var gameSettings = GameObject.Find("Game Settings");
            var gameSettingMenu = Object.FindObjectsOfType<GameSettingMenu>().FirstOrDefault();
            if (gameSettingMenu == null) return;

            var tohSettings = Object.Instantiate(gameSettings, gameSettings.transform.parent);
            var tohMenu = tohSettings.transform
                .FindChild("GameGroup")
                .FindChild("SliderInner")
                .GetComponent<GameOptionsMenu>();
            tohSettings.name = TownOfHostObjectName;

            var roleTab = GameObject.Find("RoleTab");
            var gameTab = GameObject.Find("GameTab");

            var tohTab = Object.Instantiate(roleTab, roleTab.transform.parent);
            var tohTabHighlight = tohTab.transform.FindChild("Hat Button").FindChild("Tab Background")
                .GetComponent<SpriteRenderer>();
            tohTab.transform.FindChild("Hat Button").FindChild("Icon").GetComponent<SpriteRenderer>().sprite = Helpers.LoadSpriteFromResources("TownOfHost.Resources.TabIcon.png", 100f);

            gameTab.transform.position += Vector3.left * 0.5f;
            tohTab.transform.position += Vector3.right * 0.5f;
            roleTab.transform.position += Vector3.left * 0.5f;

            var tabs = new[] { gameTab, roleTab, tohTab };
            for (var i = 0; i < tabs.Length; i++)
            {
                var button = tabs[i].GetComponentInChildren<PassiveButton>();
                if (button == null) continue;
                var copiedIndex = i;
                button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
               {
                   gameSettingMenu.RegularGameSettings.SetActive(false);
                   gameSettingMenu.RolesSettings.gameObject.SetActive(false);
                   tohSettings.gameObject.SetActive(false);
                   gameSettingMenu.GameSettingsHightlight.enabled = false;
                   gameSettingMenu.RolesSettingsHightlight.enabled = false;
                   tohTabHighlight.enabled = false;

                   switch (copiedIndex)
                   {
                       case 0:
                           gameSettingMenu.RegularGameSettings.SetActive(true);
                           gameSettingMenu.GameSettingsHightlight.enabled = true;
                           break;
                       case 1:
                           gameSettingMenu.RolesSettings.gameObject.SetActive(true);
                           gameSettingMenu.RolesSettingsHightlight.enabled = true;
                           break;
                       case 2:
                           tohSettings.gameObject.SetActive(true);
                           tohTabHighlight.enabled = true;
                           break;
                   }
               }));
            }

            foreach (var option in tohMenu.GetComponentsInChildren<OptionBehaviour>())
            {
                Object.Destroy(option.gameObject);
            }


            var scOptions = new System.Collections.Generic.List<OptionBehaviour>();
            foreach (var option in CustomOption.Options)
            {
                if (option.OptionBehaviour == null)
                {
                    var stringOption = Object.Instantiate(template, tohMenu.transform);
                    scOptions.Add(stringOption);
                    stringOption.OnValueChanged = new System.Action<OptionBehaviour>((o) => { });
                    stringOption.TitleText.text = option.Name;
                    stringOption.Value = stringOption.oldValue = option.Selection;
                    stringOption.ValueText.text = option.Selections[option.Selection].ToString();

                    option.OptionBehaviour = stringOption;
                }

                option.OptionBehaviour.gameObject.SetActive(true);
            }

            tohMenu.Children = scOptions.ToArray();
            tohSettings.gameObject.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;

        public static void Postfix(GameOptionsMenu __instance)
        {
            if (__instance.Children.Length != CustomOption.Options.Count)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            float numItems = __instance.Children.Length;
            var offset = 2.75f;

            foreach (var option in CustomOption.Options)
            {
                if (option?.OptionBehaviour == null || option.OptionBehaviour.gameObject == null) continue;

                var enabled = true;
                var parent = option.Parent;

                if (AmongUsClient.Instance.AmHost == false)
                {
                    enabled = false;
                }

                if (option.IsHidden(Options.CurrentGameMode))
                {
                    enabled = false;
                }

                while (parent != null && enabled)
                {
                    enabled = parent.Enabled;
                    parent = parent.Parent;
                }

                option.OptionBehaviour.gameObject.SetActive(enabled);
                if (enabled)
                {
                    offset -= option.isHeader ? 0.75f : 0.5f;
                    option.OptionBehaviour.transform.localPosition = new Vector3(
                        option.OptionBehaviour.transform.localPosition.x,
                        offset,
                        option.OptionBehaviour.transform.localPosition.z);

                    if (option.isHeader)
                    {
                        numItems += 0.5f;
                    }
                }
                else
                {
                    numItems--;
                }
            }

            __instance.GetComponentInParent<Scroller>().ContentYBounds.max = (-offset) - 1.5f;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
    public class StringOptionEnablePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = CustomOption.Options.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            __instance.TitleText.text = option.GetName();
            __instance.Value = __instance.oldValue = option.Selection;
            __instance.ValueText.text = option.GetString();

            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = CustomOption.Options.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            option.UpdateSelection(option.Selection + 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = CustomOption.Options.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            option.UpdateSelection(option.Selection - 1);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            CustomOption.ShareOptionSelections();
        }
    }
    [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.Start))]
    public static class RolesSettingsMenuPatch
    {
        public static void Postfix(RolesSettingsMenu __instance)
        {
            foreach (var ob in __instance.Children)
            {
                switch (ob.Title)
                {
                    case StringNames.ShapeshifterCooldown:
                        ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
