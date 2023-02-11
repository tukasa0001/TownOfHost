using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Options;
using UnityEngine;
using VentLib.Options.OptionElement;

namespace TownOfHost.GUI.Menus.Patches;

[HarmonyPriority(Priority.Last)]
[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
public class GameOptionsMenuUpdatePatch
{
    private static float _timer = 1f;
    public static Color[] colors = { Color.green, Color.red, Color.blue };

    public static void Postfix(GameOptionsMenu __instance)
    {
        _timer += Time.deltaTime;
        if (_timer < 0.1f) return;
        _timer = 0f;
        float offset = 2.75f;

        if (__instance.transform.parent.parent.name == "Game Settings")
        {
            float gamemodeOffset = 2.35f;
            Option gamemodeOption = TOHPlugin.GamemodeManager.GamemodeOption;
            ShowOption(TOHPlugin.GamemodeManager.GamemodeOption, ref gamemodeOffset);
            Vector2 position = gamemodeOption.Behaviour!.transform.localPosition;
            foreach (OptionBehaviour behaviour in __instance.Children.Skip(2))
            {
                position = new Vector2(position.x, position.y - 0.5f);
                behaviour.transform.localPosition = position;
            }
            return;
        }

        string realTabName = __instance.transform.parent.parent.name;

        GameOptionTab? tab = TOHPlugin.OptionManager.Tabs.FirstOrDefault(t => t.Name == realTabName);
        if (tab == null) return;

        tab.Options.Do(option => ShowAllOptions(option, ref offset));

        __instance.GetComponentInParent<Scroller>().ContentYBounds.max = (-offset) - 1.5f;
    }

    private static void ShowAllOptions(Option holder, ref float offset)
    {
        foreach (Option option in holder.ActiveOptions()) ShowOption(option, ref offset);
    }

    private static void ShowOption(Option option, ref float offset)
    {
        option.Behaviour!.gameObject.SetActive(true);
        Transform transform = option.Behaviour.transform;
        SpriteRenderer render = option.Behaviour.transform.Find("Background").GetComponent<SpriteRenderer>();
        if (option.Level > 0)
        {
            render.color = colors[Mathf.Clamp(((option.Level - 1) % 3), 0, 2)];
            render.size = new Vector2((float)(4.8f - ((option.Level - 1) * 0.2)), 0.45f);
            option.Behaviour.transform.Find("Title_TMP").transform.localPosition = new Vector3(-0.95f + (0.23f * (Mathf.Clamp(option.Level - 1, 0, Int32.MaxValue))), 0f);
            option.Behaviour.transform.FindChild("Title_TMP").GetComponent<RectTransform>().sizeDelta = new Vector2(3.4f, 0.37f);
            render.transform.localPosition = new Vector3(0.1f + (0.11f * (option.Level - 1)), 0f);
        }

        Vector3 pos = transform.localPosition;
        offset -= (bool)option.Attributes.GetValueOrDefault("IsHeader", false) ? 0.75f : 0.5f;
        transform.localPosition = new Vector3(pos.x, offset, pos.z);
        option.Render(ref offset);
    }
}
