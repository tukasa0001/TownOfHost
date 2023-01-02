using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TownOfHost.Extensions;
using TownOfHost.Interface.Menus;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TownOfHost.Menus;

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
public class GameOptMenuStartPatch
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
                    default:
                        break;
                }
            }
            var template = Object.FindObjectsOfType<StringOption>().FirstOrDefault();
            if (template == null) return;

            var gameSettings = GameObject.Find("Game Settings");
            if (gameSettings == null) return;
            gameSettings.transform.FindChild("GameGroup").GetComponent<Scroller>().ScrollWheelSpeed = 1f;

            var gameSettingMenu = Object.FindObjectsOfType<GameSettingMenu>().FirstOrDefault();
            if (gameSettingMenu == null) return;
            List<GameObject> menus = new() { gameSettingMenu.RegularGameSettings, gameSettingMenu.RolesSettings.gameObject };
            List<SpriteRenderer> highlights = new() { gameSettingMenu.GameSettingsHightlight, gameSettingMenu.RolesSettingsHightlight };

            var roleTab = GameObject.Find("RoleTab");
            var gameTab = GameObject.Find("GameTab");
            List<GameObject> tabs = new() { gameTab, roleTab };


            Main.OptionManager.Tabs.Count.DebugLog("Tab count: ");
            foreach (GameOptionTab tab in Main.OptionManager.Tabs)
            {
                var obj = gameSettings.transform.parent.Find(tab + "Tab");
                if (obj != null)
                {
                    obj.transform.FindChild("../../GameGroup/Text").GetComponent<TMPro.TextMeshPro>().SetText(Translator.GetString("TabGroup." + tab));
                    continue;
                }

                GameObject tohSettings = Object.Instantiate(gameSettings, gameSettings.transform.parent);
                tohSettings.name = tab.Name;
                tohSettings.transform.FindChild("BackPanel").transform.localScale =
                    tohSettings.transform.FindChild("Bottom Gradient").transform.localScale = new Vector3(1.2f, 1f, 1f);
                tohSettings.transform.FindChild("Background").transform.localScale = new Vector3(1.3f, 1f, 1f);
                tohSettings.transform.FindChild("UI_Scrollbar").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                tohSettings.transform.FindChild("UI_ScrollbarTrack").transform.localPosition += new Vector3(0.35f, 0f, 0f);
                tohSettings.transform.FindChild("GameGroup/SliderInner").transform.localPosition += new Vector3(-0.15f, 0f, 0f);
                GameOptionsMenu tohMenu = tohSettings.transform.FindChild("GameGroup/SliderInner").GetComponent<GameOptionsMenu>();

                tohMenu.GetComponentsInChildren<OptionBehaviour>().Do(x => Object.Destroy(x.gameObject));

                var behaviours = new List<OptionBehaviour>();
                behaviours.AddRange(Main.OptionManager.Options().Where(opt => opt.Tab == tab).SelectMany(opt => opt.CreateBehaviours(template, tohMenu.transform)));

                tohMenu.Children = behaviours.ToArray();
                tohSettings.gameObject.SetActive(false);
                menus.Add(tohSettings.gameObject);

                GameOptionTab initializedTab = tab.Instantiate(roleTab, roleTab.transform.parent);
                tabs.Add(initializedTab.GameObject);
                var tohTabHighlight = initializedTab.Transform.FindChild("Hat Button").FindChild("Tab Background").GetComponent<SpriteRenderer>();
                highlights.Add(tohTabHighlight);

                Main.OptionManager.AllHolders = Main.OptionManager.Options()
                    .SelectMany(opt => opt.GetHoldersRecursive()).ToList();
            }

            for (var i = 0; i < tabs.Count; i++)
            {
                tabs[i].transform.position = new(0.8f * (i - 1) - tabs.Count / 2f, tabs[i].transform.position.y, tabs[i].transform.position.z);
                var button = tabs[i].GetComponentInChildren<PassiveButton>();
                if (button == null) continue;
                var copiedIndex = i;
                button.OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                Action value = () =>
                {
                    for (var j = 0; j < menus.Count; j++)
                    {
                        menus[j].SetActive(j == copiedIndex);
                        highlights[j].enabled = j == copiedIndex;
                    }
                };
                button.OnClick.AddListener(value);
            }
        }
}