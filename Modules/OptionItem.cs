using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace TownOfHost
{
    public class OptionItem
    {
        public static readonly List<OptionItem> Options = new();
        public static int Preset = 0;

        public int Id;
        public TabGroup Tab;
        public Color Color;
        public string Name;
        public Dictionary<string, string> ReplacementDictionary;
        public OptionFormat Format;
        public System.Object[] Selections;

        public int DefaultSelection;
        public ConfigEntry<int> Entry;
        public int Selection;
        public OptionBehaviour OptionBehaviour;
        public OptionItem Parent;
        public List<OptionItem> Children;
        public bool isHeader;
        public bool isHidden;
        private bool isHiddenOnDisplay;
        public CustomGameMode GameMode;

        // eventキーワードにより、クラス外からのこのフィールドに対する以下の操作は禁止されます。
        // - 代入 (+=, -=を除く)
        // - 直接的な呼び出し
        public event EventHandler<UpdateValueEventArgs> UpdateValueEvent;

        public bool Enabled => this.GetBool();

        public OptionItem HiddenOnDisplay(bool hidden)
        {
            isHiddenOnDisplay = hidden;
            return this;
        }

        public OptionItem SetGameMode(CustomGameMode gameMode)
        {
            GameMode = gameMode;
            return this;
        }

        public OptionItem RegisterUpdateValueEvent(EventHandler<UpdateValueEventArgs> handler)
        {
            UpdateValueEvent += handler;
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
        private OptionItem()
        {
        }

        public OptionItem(int id,
            TabGroup tab,
            Color color,
            string name,
            System.Object[] selections,
            System.Object defaultValue,
            OptionItem parent,
            bool isHeader,
            bool isHidden,
            OptionFormat format,
            Dictionary<string, string> replacementDic)
        {
            Id = id;
            Tab = tab;
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

            Children = new List<OptionItem>();
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
            if (Options.Any(x => x.Id == id)) Logger.Warn($"ID:{id}が重複しています", "CustomOption");
            Options.Add(this);
            GameMode = CustomGameMode.Standard;
        }

        public static OptionItem Create(int id,
            TabGroup tab,
            Color color,
            string name,
            string[] selections,
            string defaultValue,
            OptionItem parent = null,
            bool isHeader = false,
            bool isHidden = false,
            OptionFormat format = OptionFormat.None,
            Dictionary<string, string> replacementDic = null)
        {
            return new OptionItem(id, tab, color, name, selections, defaultValue, parent, isHeader, isHidden, format, replacementDic);
        }

        public static OptionItem Create(int id,
            TabGroup tab,
            Color color,
            string name,
            float defaultValue,
            float min,
            float max,
            float step,
            OptionItem parent = null,
            bool isHeader = false,
            bool isHidden = false,
            OptionFormat format = OptionFormat.None,
            Dictionary<string, string> replacementDic = null)
        {
            var selections = new List<float>();
            for (var s = min; s <= max; s += step)
            {
                selections.Add(s);
            }

            return new OptionItem(id, tab, color, name, selections.Cast<object>().ToArray(), defaultValue, parent, isHeader, isHidden, format, replacementDic);
        }

        public static OptionItem Create(int id,
            TabGroup tab,
            Color color,
            string name,
            bool defaultValue,
            OptionItem parent = null,
            bool isHeader = false,
            bool isHidden = false,
            OptionFormat format = OptionFormat.None,
            Dictionary<string, string> replacementDic = null)
        {
            return new OptionItem(id, tab, color, name, new string[] { "ColoredOff", "ColoredOn" }, defaultValue ? "ColoredOn" : "ColoredOff", parent, isHeader, isHidden, format, replacementDic);
        }

        public static OptionItem Create(string name, float defaultValue, float min, float max, float step)
        {
            return new OptionItem();
        }

        // Static behaviour

        public static void SwitchPreset(int newPreset)
        {
            Preset = newPreset;
            foreach (var option in Options)
            {
                if (option.Id is <= 0 or >= 1_000_000) continue;

                if (AmongUsClient.Instance.AmHost)
                    option.Entry = Main.Instance.Config.Bind($"Preset{Preset}", option.Id.ToString(), option.DefaultSelection);
                int beforeValue = option.Selection;
                option.Selection = Mathf.Clamp(option.Entry.Value, 0, option.Selections.Length - 1);
                if (beforeValue != option.Selection) //UpdateValueEventの呼び出し
                    option.CallUpdateValueEvent(beforeValue, option.Selection);

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
        public void RecieveOptionSelection(int newSelection)
        {
            int beforeValue = Selection;
            Selection = newSelection;

            if (beforeValue != Selection)
                CallUpdateValueEvent(beforeValue, Selection);
        }

        // Getter

        public int GetSelection()
        {
            return Selection;
        }

        public bool GetBool()
        {
            return Selection > 0 && (Parent == null || Parent.GetBool());
        }

        public float GetFloat()
        {
            return (float)Selections[Selection];
        }
        public int GetInt()
        {
            return (int)(float)Selections[Selection];
        }
        public int GetChance()
        {
            //0%or100%の場合
            if (Selections.Length == 2) return Selection * 100;

            //0%～100%or5%～100%の場合
            var offset = 12 - Selections.Length;
            var index = Selection + offset;
            var rate = index <= 1 ? index * 5 : (index - 1) * 10;
            return rate;
        }

        public string GetString()
        {
            string sel = Selections[Selection].ToString();
            if (Format != OptionFormat.None) return string.Format(Translator.GetString("Format." + Format), sel);
            return float.TryParse(sel, out _) ? sel : Translator.GetString(sel);
        }

        public string GetName(bool disableColor = false)
        {
            return disableColor
                ? Translator.GetString(Name, ReplacementDictionary)
                : Utils.ColorString(Color, Translator.GetString(Name, ReplacementDictionary));
        }

        public virtual string GetName_v(bool display = false)
        {
            return Utils.ColorString(Color, Translator.GetString(Name, ReplacementDictionary));
        }

        public void UpdateSelection(bool enable)
        {
            UpdateSelection(enable ? 1 : 0);
        }

        public void UpdateSelection(int newSelection)
        {
            int beforeValue = Selection;
            Selection = newSelection < 0 ? Selections.Length - 1 : newSelection % Selections.Length;

            if (beforeValue != Selection) //UpdateValueEventの呼び出し
                CallUpdateValueEvent(beforeValue, Selection);

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
        public void SetPresetName(StringOption stringOption)
        {
            var nowPreset = (Preset + 1) switch
            {
                1 => Main.Preset1,
                2 => Main.Preset2,
                3 => Main.Preset3,
                4 => Main.Preset4,
                5 => Main.Preset5,
                _ => null,
            };
            if (nowPreset != null && nowPreset.Value != nowPreset.DefaultValue.ToString())
                stringOption.ValueText.text = Selections[Selection].ToString();
        }

        public void SetParent(OptionItem newParent)
        {
            Parent?.Children.Remove(this);

            Parent = newParent;
            Parent?.Children.Add(this);
        }

        // EventArgs
        private void CallUpdateValueEvent(int beforeValue, int currentValue)
        {
            if (UpdateValueEvent == null) return;
            try
            {
                UpdateValueEvent(this, new UpdateValueEventArgs(beforeValue, currentValue));
            }
            catch (Exception ex)
            {
                Logger.Error($"[{Name}] UpdateValueEventの呼び出し時に例外が発生しました", "OptionItem.UpdateValueEvent");
                Logger.Exception(ex, "OptionItem.UpdateValueEvent");
            }
        }

        public class UpdateValueEventArgs : EventArgs
        {
            public int CurrentValue { get; set; }
            public int BeforeValue { get; set; }
            public UpdateValueEventArgs(int beforeValue, int currentValue)
            {
                CurrentValue = currentValue;
                BeforeValue = beforeValue;
            }
        }
    }
    public enum TabGroup
    {
        MainSettings,
        ImpostorRoles,
        CrewmateRoles,
        NeutralRoles,
        Addons
    }
    public enum OptionFormat
    {
        None,
        Players,
        Seconds,
        Percent,
        Times,
        Multiplier,
        Votes,
        Pieces,
    }
}