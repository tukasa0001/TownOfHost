using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace TownOfHost
{
    public class CustomOption
    {
        public static readonly List<CustomOption> Options = new();
        public static int Preset = 0;

        public int Id;
        public Color Color;
        public string Name;
        public Dictionary<string, string> ReplacementDictionary;
        public string Format;
        public System.Object[] Selections;

        public int DefaultSelection;
        public ConfigEntry<int> Entry;
        public int Selection;
        public OptionBehaviour OptionBehaviour;
        public CustomOption Parent;
        public List<CustomOption> Children;
        public bool isHeader;
        public bool isHidden;
        private bool isHiddenOnDisplay;
        public CustomGameMode GameMode;

        public bool Enabled => this.GetBool();

        public CustomOption HiddenOnDisplay(bool hidden)
        {
            isHiddenOnDisplay = hidden;
            return this;
        }

        public CustomOption SetGameMode(CustomGameMode gameMode)
        {
            GameMode = gameMode;
            return this;
        }

        public bool IsHidden(CustomGameMode gameMode)
        {
            if (isHidden) return true;

            /*  自身に設定されたGameModeが All or 引数gameMode 以外なら非表示
                GameMode:Standard    & gameMode:Standard != 0
                GameMode:HideAndSeek & gameMode:Standard == 0
                GameMode:All         & gameMode:Standard != 0
            */
            return (int)(gameMode & GameMode) == 0;
        }

        public bool IsHiddenOnDisplay(CustomGameMode gameMode)
        {
            return isHiddenOnDisplay || IsHidden(gameMode);
        }

        // Option creation
        private CustomOption()
        {
        }

        public CustomOption(int id,
            Color color,
            string name,
            System.Object[] selections,
            System.Object defaultValue,
            CustomOption parent,
            bool isHeader,
            bool isHidden,
            string format,
            Dictionary<string, string> replacementDic)
        {
            Id = id;
            Color = color;
            Name = name;
            Selections = selections;
            var index = Array.IndexOf(selections, defaultValue);
            DefaultSelection = index >= 0 ? index : 0;
            Parent = parent;
            this.isHeader = isHeader;
            this.isHidden = isHidden;
            Format = format;
            ReplacementDictionary = replacementDic;

            isHiddenOnDisplay = false;

            Children = new List<CustomOption>();
            parent?.Children.Add(this);

            Selection = 0;
            if (id == 0)
            {
                Entry = Main.Instance.Config.Bind($"Current Preset", id.ToString(), DefaultSelection);
                Preset = Selection = Mathf.Clamp(Entry.Value, 0, selections.Length - 1);
            }
            if (id > 0)
            {
                Entry = Main.Instance.Config.Bind($"Preset{Preset}", id.ToString(), DefaultSelection);
                Selection = Mathf.Clamp(Entry.Value, 0, selections.Length - 1);
            }
            Options.Add(this);
            GameMode = CustomGameMode.Standard;
        }

        public static CustomOption Create(int id,
            Color color,
            string name,
            string[] selections,
            string defaultValue,
            CustomOption parent = null,
            bool isHeader = false,
            bool isHidden = false,
            string format = "",
            Dictionary<string, string> replacementDic = null)
        {
            return new CustomOption(id, color, name, selections, defaultValue, parent, isHeader, isHidden, format, replacementDic);
        }

        public static CustomOption Create(int id,
            Color color,
            string name,
            float defaultValue,
            float min,
            float max,
            float step,
            CustomOption parent = null,
            bool isHeader = false,
            bool isHidden = false,
            string format = "",
            Dictionary<string, string> replacementDic = null)
        {
            var selections = new List<float>();
            for (var s = min; s <= max; s += step)
            {
                selections.Add(s);
            }

            return new CustomOption(id, color, name, selections.Cast<object>().ToArray(), defaultValue, parent, isHeader, isHidden, format, replacementDic);
        }

        public static CustomOption Create(int id,
            Color color,
            string name,
            bool defaultValue,
            CustomOption parent = null,
            bool isHeader = false,
            bool isHidden = false,
            string format = "",
            Dictionary<string, string> replacementDic = null)
        {
            return new CustomOption(id, color, name, new string[] { "Off", "On" }, defaultValue ? "On" : "Off", parent, isHeader, isHidden, format, replacementDic);
        }

        public static CustomOption Create(string name, float defaultValue, float min, float max, float step)
        {
            return new CustomOption();
        }

        // Static behaviour

        public static void SwitchPreset(int newPreset)
        {
            Preset = newPreset;
            foreach (var option in Options)
            {
                if (option.Id <= 0) continue;

                option.Entry = Main.Instance.Config.Bind($"Preset{Preset}", option.Id.ToString(), option.DefaultSelection);
                option.Selection = Mathf.Clamp(option.Entry.Value, 0, option.Selections.Length - 1);
                if (option.OptionBehaviour is not null and StringOption stringOption)
                {
                    stringOption.oldValue = stringOption.Value = option.Selection;
                    stringOption.ValueText.text = option.GetString();
                }
            }
        }

        public static void Refresh()
        {
            foreach (var option in Options)
            {
                if (option.OptionBehaviour is not null and StringOption stringOption)
                {
                    stringOption.oldValue = stringOption.Value = option.Selection;
                    stringOption.ValueText.text = option.GetString();
                    stringOption.TitleText.text = option.GetName();
                }
            }
        }

        public static void ShareOptionSelections()
        {
            if (PlayerControl.AllPlayerControls.Count <= 1 || (AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null)) return;

            RPC.SyncCustomSettingsRPC();
        }

        // Getter

        public int GetSelection()
        {
            return Selection;
        }

        public bool GetBool()
        {
            return Selection > 0;
        }

        public float GetFloat()
        {
            return (float)Selections[Selection];
        }
        public int GetInt()
        {
            return (int)(float)Selections[Selection];
        }

        public string GetString()
        {
            string sel = Selections[Selection].ToString();
            if (Format != "") return string.Format(Translator.GetString(Format), sel);
            return float.TryParse(sel, out _) ? sel : Translator.GetString(sel);
        }

        public string GetName(bool disableColor = false)
        {
            return disableColor
                ? Translator.GetString(Name, ReplacementDictionary)
                : Helpers.ColorString(Color, Translator.GetString(Name, ReplacementDictionary));
        }

        public virtual string GetName_v(bool display = false)
        {
            return Helpers.ColorString(Color, Translator.GetString(Name, ReplacementDictionary));
        }

        public void UpdateSelection(bool enable)
        {
            UpdateSelection(enable ? 1 : 0);
        }

        public void UpdateSelection(int newSelection)
        {
            Selection = newSelection < 0 ? Selections.Length - 1 : newSelection % Selections.Length;

            if (OptionBehaviour is not null and StringOption stringOption)
            {
                stringOption.oldValue = stringOption.Value = Selection;
                stringOption.ValueText.text = GetString();

                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer)
                {
                    if (Entry != null) Entry.Value = Selection;
                    if (Id == TownOfHost.Options.PresetId)
                    {
                        SwitchPreset(Selection);
                    }
                    ShareOptionSelections();
                }
            }
        }

        public void SetParent(CustomOption newParent)
        {
            Parent?.Children.Remove(this);

            Parent = newParent;
            Parent?.Children.Add(this);
        }
    }
}