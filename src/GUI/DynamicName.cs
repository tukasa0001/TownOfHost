#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AmongUs.Data;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Roles;
using TownOfHost.RPC;
using UnityEngine;
using VentFramework;

namespace TownOfHost.Interface.Menus.CustomNameMenu;

public class DynamicName
{
    public List<List<UI>> componentOrder { get; set; } = new();
    // For in-game rendering
    public string RawName;
    private string lastString;
    private DateTime lastRender;
    private PlayerControl? myPlayer;

    private Dictionary<GameState, Dictionary<byte, List<Tuple<UI, DynamicString>>>> renderRules = new()
    {
        { GameState.None, new Dictionary<byte, List<Tuple<UI, DynamicString>>>() },
        { GameState.InLobby, new Dictionary<byte, List<Tuple<UI, DynamicString>>>() },
        { GameState.InIntro, new Dictionary<byte, List<Tuple<UI, DynamicString>>>() },
        { GameState.InMeeting, new Dictionary<byte, List<Tuple<UI, DynamicString>>>() },
        { GameState.Roaming, new Dictionary<byte, List<Tuple<UI, DynamicString>>>() },
    };

    private Dictionary<UI, DynamicString> valueDictionary = new()
    {
        { UI.Name, new DynamicString("")},
        { UI.Role, new DynamicString("")},
        { UI.Subrole, new DynamicString("")},
        { UI.Cooldown, new DynamicString("")},
        { UI.Counter, new DynamicString("")},
        { UI.Misc, new DynamicString("")}
    };

    private Dictionary<UI, string> hlFormatDictionary = new()
    {
        { UI.Name, "{0}" },
        { UI.Role, "{0}" },
        { UI.Subrole, "{0}"},
        { UI.Cooldown, "{0}" },
        { UI.Counter, "{0}" },
        { UI.Misc, "{0}" }
    };

    public Dictionary<UI, float> sizeDictionary { get; set; } = new()
    {
        { UI.Name, 2.925f },
        { UI.Role, 2.925f },
        { UI.Subrole, 2.925f },
        { UI.Cooldown, 2.925f },
        { UI.Counter, 2.925f },
        { UI.Misc, 2.925f }
    };

    public Dictionary<UI, int> SpacesDictionary { get; set; } = new()
    {
        { UI.Name, 0 },
        { UI.Role, 0 },
        { UI.Subrole, 0 },
        { UI.Cooldown, 0 },
        { UI.Counter, 0 },
        { UI.Misc, 0 }
    };

    private Dictionary<UI, bool> visibleDictionary = new()
    {
        { UI.Name, true },
        { UI.Role, true },
        { UI.Subrole, true },
        { UI.Cooldown, true },
        { UI.Counter, true },
        { UI.Misc, true }
    };

    public DynamicName()
    {
        valueDictionary[UI.Name] = new DynamicString(DataManager.Player.Customization.Name);
        CustomRole role = CustomRoleManager.Roles.GetRandom();
        valueDictionary[UI.Role] = new DynamicString(role.RoleColor.Colorize(role.RoleName));
        valueDictionary[UI.Subrole] = new DynamicString(/*CustomRoleManager.Static.Lovers.RoleColor*/Color.magenta.Colorize("♡"));
        valueDictionary[UI.Counter] = new DynamicString("(" + Color.yellow.Colorize("3/16") + ")");
        valueDictionary[UI.Cooldown] = new DynamicString("<color=#ed9247>CD: </color>" + "15s");
        valueDictionary[UI.Misc] = new DynamicString(" Misc");
        SetupComponentOrder();
    }

    public static DynamicName For(PlayerControl player)
    {
        string playerName = (Game.players.TryGetValue(player.PlayerId, out PlayerPlus? pp)) && pp?.DynamicName != null ? pp.DynamicName.RawName : player.Data.PlayerName;

        DynamicName name = new() {
            valueDictionary = {
                [UI.Name] = new DynamicString(() => Color.white.Colorize(playerName)),
                [UI.Role] = new DynamicString(() => player.GetRoleColor().Colorize(player.GetRoleName())),
                [UI.Subrole] = new DynamicString(""),
                [UI.Counter] = new DynamicString(""),
                [UI.Cooldown] = new DynamicString(""),
                [UI.Misc] = new DynamicString("")
            },
            RawName = playerName,
            myPlayer = player
        };
        name.Deserialize();
        name.SetupComponentOrder();
        name.AddRule(GameState.Roaming, UI.Name);
        name.AddRule(GameState.InMeeting, UI.Name);
        name.AddRule(GameState.InLobby, UI.Name);
        name.AddRule(GameState.InIntro, UI.Name);

        return name;
    }


    public void HighlightComponent(UI component) => hlFormatDictionary[component] = "<color=#fcd703>{0}</color>";
    public void UnhighlightComponent(UI component) => hlFormatDictionary[component] = "{0}";

    public string PreviewName(bool serialize = true)
    {
        string render = "";

        int i = 0;
        foreach (List<UI> components in componentOrder)
        {
            foreach (UI component in components)
            {
                if (!visibleDictionary[component]) continue;
                int spaces = SpacesDictionary[component];
                string hlFormat = hlFormatDictionary[component];
                float size = sizeDictionary[component];
                string text = valueDictionary[component].Value;
                render += $"<size={size}>" + String.Format(hlFormat, hlFormat == "{0}" ? text : text.RemoveHtmlTags()) + "</size>" + " ".Repeat(spaces);
            }

            render += "\n";
            i++;
        }

        if (serialize)
            Serialize();
        return render.Trim('\n');
    }

    public string GetName(GameState? state = null, Color? forceColor = null)
    {
        string render = "";

        int i = 0;
        foreach (List<UI> components in componentOrder)
        {
            foreach (UI component in components)
            {
                if (state is GameState.InMeeting && component is not (UI.Name or UI.Role)) continue;
                if (state is GameState.InIntro && component is not UI.Name) continue;
                int spaces = SpacesDictionary[component];
                float size = sizeDictionary[component];
                string text = valueDictionary[component].Value;
                render += $"<size={size / 1.5}>" + text + "</size>" + " ".Repeat(spaces);
            }

            render += "\n";
            i++;
        }

        return forceColor != null ? forceColor.Value.Colorize(render.RemoveColorTags().Trim('\n')) : render.Trim('\n');
    }

    public void ShiftComponent(UI component, ShiftDirection direction)
    {
        int rowIndex = componentOrder.FindIndex(row => row.Contains(component));
        List<UI> row = componentOrder[rowIndex];

        if (direction is ShiftDirection.Left or ShiftDirection.Right)
        {
            int slotIndex = row.IndexOf(component);
            int newIndex = Mathf.Clamp(direction == ShiftDirection.Left ? slotIndex - 1 : slotIndex + 1, 0, row.Count - 1);
            UI tempComponent = row[newIndex];
            row[slotIndex] = tempComponent;
            row[newIndex] = component;
        }
        else
        {
            int newIndex = Mathf.Clamp(direction == ShiftDirection.Up ? rowIndex - 1 : rowIndex + 1, 0, 2);

            if (newIndex == rowIndex && rowIndex == 0 && componentOrder[2].Count == 0)
            {
                List<UI> temporary = componentOrder[2];
                componentOrder[2] = componentOrder[1];
                componentOrder[1] = componentOrder[0];
                componentOrder[0] = temporary;
            }
            else if (newIndex == rowIndex && rowIndex == 2 && componentOrder[0].Count == 0)
            {
                List<UI> temporary = componentOrder[0];
                componentOrder[0] = componentOrder[1];
                componentOrder[1] = componentOrder[2];
                componentOrder[2] = temporary;
            }
            else if (newIndex == rowIndex) return;

            row.Remove(component);
            componentOrder[newIndex].Add(component);
        }
    }

    public void SetComponentValue(UI component, DynamicString value) => valueDictionary[component] = value;

    public void IncrementSize(UI component) => sizeDictionary[component] += 0.25f;
    public void DecrementSize(UI component) => sizeDictionary[component] -= 0.25f;
    public void ToggleVisibility(UI component) => visibleDictionary[component] = !visibleDictionary[component];
    public void AddSpace(UI component) => SpacesDictionary[component]++;
    public void RemoveSpace(UI component) => SpacesDictionary[component]--;

    public void AddRule(GameState state, UI component, DynamicString? value = null, int playerId = -1)
    {
        value ??= new DynamicString("");

        if (playerId == -1)
        {
            (..15).ToEnumerator().Where(i => myPlayer == null || myPlayer.PlayerId != i).Do(i => AddRule(state, component, value, i));
            return;
        }

        if (!renderRules[state].TryGetValue((byte)playerId, out List<Tuple<UI, DynamicString>>? overrides))
            renderRules[state][(byte)playerId] = overrides = new List<Tuple<UI, DynamicString>>();
        overrides.RemoveAll(c => c.Item1 == component);
        overrides.Add(new Tuple<UI, DynamicString>(component, value.Value));
    }

    public void RemoveRule(GameState state, UI component, int playerId = -1)
    {
        if (playerId == -1)
        {
            (..15).ToEnumerator().Where(i => myPlayer == null || myPlayer.PlayerId != i).Do(i => RemoveRule(state, component, i));
            return;
        }

        if (!renderRules[state].TryGetValue((byte)playerId, out List<Tuple<UI, DynamicString>>? overrides)) return;
        overrides.RemoveAll(c => c.Item1 == component);
    }

    private void SetupComponentOrder()
    {
        while (componentOrder.Count < 3)
            componentOrder.Add(new List<UI>());

        if (!componentOrder.Any(r => r.Contains(UI.Name)))
            componentOrder[0].Add(UI.Name);

        if (!componentOrder.Any(r => r.Contains(UI.Role)))
            componentOrder[1].Add(UI.Role);
        if (!componentOrder.Any(r => r.Contains(UI.Subrole)))
            componentOrder[1].Add(UI.Subrole);
        if (!componentOrder.Any(r => r.Contains(UI.Counter)))
            componentOrder[1].Add(UI.Counter);

        if (!componentOrder.Any(r => r.Contains(UI.Cooldown)))
            componentOrder[2].Add(UI.Cooldown);
        if (!componentOrder.Any(r => r.Contains(UI.Misc)))
            componentOrder[2].Add(UI.Misc);
    }

    private void Serialize()
    {
        FileInfo componentInfo = new("./BepInEx/config/components.json");
        FileStream stream = File.Open(componentInfo.FullName, componentInfo.Exists ? FileMode.Truncate : FileMode.CreateNew);
        string jsonString = JsonSerializer.Serialize(this);
        stream.Write(Encoding.ASCII.GetBytes(jsonString));
        stream.Close();
    }

    public void Deserialize()
    {
        FileInfo componentInfo = new("./BepInEx/config/components.json");
        if (!componentInfo.Exists) return;
        StreamReader reader = componentInfo.OpenText();
        string jsonString = reader.ReadToEnd();
        reader.Close();
        DynamicName? deserialize = JsonSerializer.Deserialize<DynamicName>(jsonString);
        if (deserialize == null) return;
        deserialize.SetupComponentOrder();
        this.componentOrder = deserialize.componentOrder;
        this.sizeDictionary = deserialize.sizeDictionary;
        this.SpacesDictionary = deserialize.SpacesDictionary;
    }

    public void ResetToDefault()
    {
        FileInfo componentInfo = new("./BepInEx/config/components.json");
        FileStream stream = File.Open(componentInfo.FullName, componentInfo.Exists ? FileMode.Truncate : FileMode.CreateNew);
        stream.Write("{}"u8);
        stream.Close();
        if (!componentInfo.Exists) return;
        Serialize();
        DynamicName def = new();
        this.sizeDictionary = def.sizeDictionary;
        this.componentOrder = def.componentOrder;
    }

    public void Render(int specific = -2)
    {
        float durationSinceLast = (float)(DateTime.Now - lastRender).TotalSeconds;
        if (durationSinceLast < ModConstants.DynamicNameTimeBetweenRenders) return;
        string str = GetName();
        if (lastString != str)
            RpcV2.Immediate(myPlayer?.NetId ?? 0, RpcCalls.SetName).Write(str).Send(specific != -2 ? specific : myPlayer?.GetClientId() ?? -1);
        else return;
        lastRender = DateTime.Now;
        lastString = str;
    }

    public void RenderAsIf(GameState state, Color? forceColor = null, int specific = -2)
    {
        string str = GetName(state, forceColor);
        if (lastString != str)
            RpcV2.Immediate(myPlayer.NetId, RpcCalls.SetName).Write(str).Send(specific != -2 ? specific : myPlayer.GetClientId());
        lastRender = DateTime.Now;
        lastString = str;
    }

    public void RenderFor(PlayerControl player, GameState? state = null)
    {
        float durationSinceLast = (float)(DateTime.Now - lastRender).TotalSeconds;
        if (durationSinceLast < ModConstants.DynamicNameTimeBetweenRenders) return;
        if (myPlayer != null && myPlayer.PlayerId == player.PlayerId)
        {
            if (state == null) Render();
            else RenderAsIf(state.Value);
            return;
        }
        state ??= Game.State;
        if (!renderRules[state.Value].TryGetValue(player.PlayerId, out List<Tuple<UI, DynamicString>>? allowedComponents))
        {
            allowedComponents = new List<Tuple<UI, DynamicString>> { new(UI.Name, new DynamicString("")) };
            renderRules[state.Value][player.PlayerId] = allowedComponents;
        }


        string render = "";

        int i = 0;
        foreach (List<UI> components in componentOrder)
        {
            foreach (UI component in components)
            {
                Tuple<UI, DynamicString>? specificOverride = allowedComponents?.FirstOrDefault(p => p.Item1 == component);
                if (specificOverride == null) continue;
                string overrideValue = specificOverride.Item2.Value;
                int spaces = SpacesDictionary[component];
                float size = sizeDictionary[component];
                string text;
                if (overrideValue.Contains("{0}"))
                    text = String.Format(overrideValue, valueDictionary[component].Value.RemoveColorTags());
                else
                    text = overrideValue == "" ? valueDictionary[component].Value : overrideValue;
                render += $"<size={size / 1.5}>" + text + "</size>" + " ".Repeat(spaces);
            }

            render += "\n";
            i++;
        }

        RpcV2.Immediate(myPlayer?.NetId ?? 0, RpcCalls.SetName).Write(render).Send(player.GetClientId());
    }

    public string GetComponentValue(UI subrole) => valueDictionary[subrole].Value;
}

public struct DynamicString
{
    public DynamicString(string value) => _value = value;
    public DynamicString(Func<string> supplier) => _supplier = supplier;

    public string Value => _value ?? _supplier?.Invoke() ?? "N/A";

    public string? value { init => _value = value; }
    public Func<string>? supplier { init => _supplier = value; }

    private Func<string>? _supplier;
    private string? _value;
}

public enum ShiftDirection
{
    Up,
    Down,
    Left,
    Right
}

public enum UI
{
    Name,
    Role,
    Subrole,
    Cooldown,
    Counter,
    Misc
}