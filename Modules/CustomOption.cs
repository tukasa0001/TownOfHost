using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public class CustomOption
    {
        public static readonly List<CustomOption> Options = new List<CustomOption>();
        public static int Preset = 0;

        public int Id;
        public Color Color;
        public string Name;
        public string Format;
        public string Prefix, Suffix;
        public System.Object[] Selections;

        public int DefaultSelection;
        public ConfigEntry<int> Entry;
        public int Selection;
        public OptionBehaviour OptionBehaviour;
        public CustomOption Parent;
        public List<CustomOption> Children;
        public bool IsHeader;
        public bool _isHidden;
        private bool _isHiddenOnDisplay;
        public CustomGameMode GameMode;

        public List<CustomOption> PrerequisiteOptions;
        public List<CustomOption> PrerequisiteOptionsInv;

        public bool Enabled => this.GetBool();

        public CustomOption HiddenOnDisplay(bool hidden)
        {
            _isHiddenOnDisplay = hidden;
            return this;
        }

        public CustomOption SetGameMode(CustomGameMode gameMode)
        {
            GameMode = gameMode;
            return this;
        }

        public bool IsHidden(CustomGameMode gameMode)
        {
            return _isHidden || (0 == (int)(gameMode & GameMode))
                             || PrerequisiteOptions.Count > 0 && PrerequisiteOptions.Any((option) => !option.Enabled || option.IsHidden(gameMode))
                             || PrerequisiteOptionsInv.Count > 0 && PrerequisiteOptionsInv.Any((option) => option.Enabled || option.IsHidden(gameMode));
        }

        public bool IsHiddenOnDisplay(CustomGameMode gameMode)
        {
            return _isHiddenOnDisplay || IsHidden(gameMode);
        }

        // Option creation
        private CustomOption()
        {
        }

        public CustomOption(int id, Color color, string name, System.Object[] selections, System.Object defaultValue, CustomOption parent, bool isHeader, bool isHidden, string format)
        {
            Id = id;
            Color = color;
            Name = name;
            Format = format;
            Selections = selections;
            var index = Array.IndexOf(selections, defaultValue);
            DefaultSelection = index >= 0 ? index : 0;
            Parent = parent;
            IsHeader = isHeader;
            _isHidden = isHidden;

            Prefix = null;
            Suffix = null;

            _isHiddenOnDisplay = false;

            Children = new List<CustomOption>();
            parent?.Children.Add(this);

            Selection = 0;
            if (id > 0)
            {
                Entry = main.Instance.Config.Bind($"Preset{Preset}", id.ToString(), DefaultSelection);
                Selection = Mathf.Clamp(Entry.Value, 0, selections.Length - 1);
            }
            Options.Add(this);

            PrerequisiteOptions = new List<CustomOption>();
            PrerequisiteOptionsInv = new List<CustomOption>();
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
            string format = "")
        {
            return new CustomOption(id, color, name, selections, defaultValue, parent, isHeader, isHidden, format);
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
            string format = "")
        {
            var selections = new List<float>();
            for (var s = min; s <= max; s += step)
            {
                selections.Add(s);
            }

            return new CustomOption(id, color, name, selections.Cast<object>().ToArray(), defaultValue, parent, isHeader, isHidden, format);
        }

        public static CustomOption Create(int id, 
            Color color, 
            string name,
            bool defaultValue, 
            CustomOption parent = null, 
            bool isHeader = false,
            bool isHidden = false, 
            string format = "")
        {
            return new CustomOption(id, color, name, new string[] { "Off", "On" }, defaultValue ? "On" : "Off", parent, isHeader, isHidden, format);
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

                option.Entry = main.Instance.Config.Bind($"Preset{Preset}", option.Id.ToString(), option.DefaultSelection);
                option.Selection = Mathf.Clamp(option.Entry.Value, 0, option.Selections.Length - 1);
                if (option.OptionBehaviour != null && option.OptionBehaviour is StringOption stringOption)
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
                if (option.Id <= 0) continue;

                option.Selection = Mathf.Clamp(option.Entry.Value, 0, option.Selections.Length - 1);
                if (option.OptionBehaviour != null && option.OptionBehaviour is StringOption stringOption)
                {
                    stringOption.oldValue = stringOption.Value = option.Selection;
                    stringOption.ValueText.text = option.GetString();
                    stringOption.TitleText.text = option.GetName();
                }
            }
        }

        public static void ShareOptionSelections()
        {
            if (PlayerControl.AllPlayerControls.Count <= 1 || AmongUsClient.Instance.AmHost == false && PlayerControl.LocalPlayer == null) return;

            RPC.SyncCustomSettingsRPC();
            // MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncCustomSettings, Hazel.SendOption.Reliable);
            // messageWriter.WritePacked((uint)CustomOption.Options.Count);
            // foreach (CustomOption option in CustomOption.Options)
            // {
            //     messageWriter.WritePacked((uint)option.Id);
            //     messageWriter.WritePacked((uint)Convert.ToUInt32(option.Selection));
            // }
            // messageWriter.EndMessage();
        }

        public void AddPrerequisite(CustomOption option)
        {
            PrerequisiteOptions.Add(option);
        }

        public void AddInvPrerequisite(CustomOption option)
        {
            PrerequisiteOptionsInv.Add(option);
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

        protected string GetStringSelection()
        {
            string sel = Selections[Selection].ToString();
            if (Format != "")
            {
                return string.Format(Translator.getString(Format), sel);
            }

            if (float.TryParse(sel, out _))
            {
                return sel;
            }

            return Translator.getString(sel);
        }

        public string GetString()
        {
            var text = GetStringSelection();

            if (Prefix != null)
            {
                text = Translator.getString("option.prefix." + Prefix) + text;
            }
            
            if (Suffix != null)
            {
                text += Translator.getString("option.suffix." + Suffix);
            }

            return text;
        }

        public string GetName()
        {
            return Helpers.ColorString(Color, Translator.getString(Name));
        }

        public virtual string getName(bool display=false)
        {
            return Helpers.ColorString(Color, Translator.getString(Name));
        }

        public void UpdateSelection(bool enable)
        {
            UpdateSelection(enable ? 1 : 0);
        }
        
        public void UpdateSelection(int newSelection)
        {
            if (newSelection < 0)
            {
                Selection = Selections.Length - 1;
            }
            else
            {
                Selection = newSelection % Selections.Length;
            }


            if (OptionBehaviour != null && OptionBehaviour is StringOption stringOption)
            {
                stringOption.oldValue = stringOption.Value = Selection;
                stringOption.ValueText.text = GetString();

                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer)
                {
                    if (Id == TownOfHost.Options.PresetId)
                    {
                        SwitchPreset(Selection);
                    }
                    else if (Entry != null) Entry.Value = Selection;

                    if (Id == TownOfHost.Options.ForceJapaneseOptionId)
                    {
                        Refresh();
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