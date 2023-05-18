using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using UnityEngine;
using TownOfHost.Modules;

namespace TownOfHost
{
    public abstract class OptionItem
    {
        #region static
        public static IReadOnlyList<OptionItem> AllOptions => _allOptions;
        private static List<OptionItem> _allOptions = new();
        public static int CurrentPreset { get; set; }

        private static LogHandler logger = Logger.Handler(nameof(OptionItem));
        private const string Header = "%TOHOptions%";
        /// <summary>
        /// 現在の設定のうち，プリセットでなく，Valueが0でないものを<see cref="FromHex"/>で読み込める16進形式の文字列に変換します<br/>
        /// <see cref="Header"/>から始まり，'!'が各オプションを区切り，','がオプションIDとオプションの値を区切ります
        ///
        /// </summary>
        /// <returns>変換された文字列 ([Header][オプション1のID],[オプションの1の値]![オプション2のID],[オプション2の値]!...)</returns>
        public static string ToHex()
        {
            var builder = new StringBuilder(Header);
            foreach (var option in AllOptions)
            {
                if (option is PresetOptionItem)
                {
                    continue;
                }
                var value = option.GetValue();
                if (value == 0)
                {
                    continue;
                }
                builder.Append(option.Id.ToString("x")).Append(",").Append(option.GetValue().ToString("x")).Append("!");
            }
            return builder.ToString();
        }
        /// <summary>
        /// <see cref="ToHex"/>で変換された形式の文字列を読み込んで現在のプリセットを上書きします
        /// </summary>
        /// <param name="hex">16進形式のオプション</param>
        public static void FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                logger.Info("文字列が空");
                goto Failed;
            }
            // 規定された文字列から始まらなければTOHのオプションではない
            // "[16進数],[16進数]!"の1回以上の繰り返しじゃなければフォーマットが違う
            if (!Regex.IsMatch(hex, $"^{Header}([0-9a-fA-F]+,[0-9a-fA-F]+!)+$"))
            {
                logger.Info("フォーマットに不適合");
                goto Failed;
            }
            try
            {
                foreach (var option in AllOptions)
                {
                    if (option is PresetOptionItem)
                    {
                        continue;
                    }
                    option.SetValue(0);
                }

                hex = hex.Replace(Header, "");
                var hexOptions = hex.Split('!', StringSplitOptions.RemoveEmptyEntries);
                foreach (var hexOption in hexOptions)
                {
                    var split = hexOption.Split(',');
                    var id = Convert.ToInt32(split[0], 16);
                    var value = Convert.ToInt32(split[1], 16);

                    var option = AllOptions.FirstOrDefault(option => option.Id == id);
                    if (option != null)
                    {
                        option.SetValue(value);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Exception(ex);
                goto Failed;
            }
            return;

        Failed:
            Logger.SendInGame(Translator.GetString("Message.FailedToLoadOptions"));
        }
        #endregion

        // 必須情報 (コンストラクタで必ず設定させる必要がある値)
        public int Id { get; }
        public string Name { get; }
        public int DefaultValue { get; }
        public TabGroup Tab { get; }
        public bool IsSingleValue { get; }

        // 任意情報 (空・nullを許容する または、ほとんど初期値で問題ない値)
        public Color NameColor { get; protected set; }
        public OptionFormat ValueFormat { get; protected set; }
        public CustomGameMode GameMode { get; protected set; }
        public bool IsHeader { get; protected set; }
        public bool IsHidden { get; protected set; }
        public Dictionary<string, string> ReplacementDictionary
        {
            get => _replacementDictionary;
            set
            {
                if (value == null) _replacementDictionary?.Clear();
                else _replacementDictionary = value;
            }
        }
        private Dictionary<string, string> _replacementDictionary;

        // 設定値情報 (オプションの値に関わる情報)
        public int CurrentValue
        {
            get => GetValue();
            set => SetValue(value);
        }

        // 親子情報
        public OptionItem Parent { get; private set; }
        public List<OptionItem> Children;

        // 内部情報 (外部から参照することを想定していない情報)
        public ConfigEntry<int> CurrentEntry =>
            IsSingleValue ? singleEntry : AllConfigEntries[CurrentPreset];
        private ConfigEntry<int>[] AllConfigEntries;
        private ConfigEntry<int> singleEntry;

        public OptionBehaviour OptionBehaviour;

        // イベント
        // eventキーワードにより、クラス外からのこのフィールドに対する以下の操作は禁止されます。
        // - 代入 (+=, -=を除く)
        // - 直接的な呼び出し
        public event EventHandler<UpdateValueEventArgs> UpdateValueEvent;

        // コンストラクタ
        public OptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue)
        {
            // 必須情報の設定
            Id = id;
            Name = name;
            DefaultValue = defaultValue;
            Tab = tab;
            IsSingleValue = isSingleValue;

            // 任意情報の初期値設定
            NameColor = Color.white;
            ValueFormat = OptionFormat.None;
            GameMode = CustomGameMode.All;
            IsHeader = false;
            IsHidden = false;

            // オブジェクト初期化
            Children = new();

            // ConfigEntry初期化
            AllConfigEntries = new ConfigEntry<int>[5];
            if (Id == 0)
            {
                singleEntry = Main.Instance.Config.Bind("Current Preset", id.ToString(), DefaultValue);
                CurrentPreset = singleEntry.Value;
            }
            else if (IsSingleValue)
            {
                singleEntry = Main.Instance.Config.Bind("SingleEntryOptions", id.ToString(), DefaultValue);
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    AllConfigEntries[i] = Main.Instance.Config.Bind($"Preset{i + 1}", id.ToString(), DefaultValue);
                }
            }

            if (AllOptions.Any(op => op.Id == id))
            {
                Logger.Error($"ID:{id}が重複しています", "OptionItem");
            }
            _allOptions.Add(this);
        }

        // Setter
        public OptionItem Do(Action<OptionItem> action)
        {
            action(this);
            return this;
        }

        public OptionItem SetColor(Color value) => Do(i => i.NameColor = value);
        public OptionItem SetValueFormat(OptionFormat value) => Do(i => i.ValueFormat = value);
        public OptionItem SetGameMode(CustomGameMode value) => Do(i => i.GameMode = value);
        public OptionItem SetHeader(bool value) => Do(i => i.IsHeader = value);
        public OptionItem SetHidden(bool value) => Do(i => i.IsHidden = value);

        public OptionItem SetParent(OptionItem parent) => Do(i =>
        {
            i.Parent = parent;
            parent.SetChild(i);
        });
        public OptionItem SetChild(OptionItem child) => Do(i => i.Children.Add(child));
        public OptionItem RegisterUpdateValueEvent(EventHandler<UpdateValueEventArgs> handler)
            => Do(i => UpdateValueEvent += handler);

        // 置き換え辞書
        public OptionItem AddReplacement((string key, string value) kvp)
            => Do(i =>
            {
                ReplacementDictionary ??= new();
                ReplacementDictionary.Add(kvp.key, kvp.value);
            });
        public OptionItem RemoveReplacement(string key)
            => Do(i => ReplacementDictionary?.Remove(key));

        // Getter
        public virtual string GetName(bool disableColor = false)
        {
            return disableColor ?
                Translator.GetString(Name, ReplacementDictionary) :
                Utils.ColorString(NameColor, Translator.GetString(Name, ReplacementDictionary));
        }
        public virtual bool GetBool() => CurrentValue != 0 && (Parent == null || Parent.GetBool());
        public virtual int GetInt() => CurrentValue;
        public virtual float GetFloat() => CurrentValue;
        public virtual string GetString()
        {
            return ApplyFormat(CurrentValue.ToString());
        }
        public virtual int GetValue() => CurrentEntry.Value;

        // 旧IsHidden関数
        public virtual bool IsHiddenOn(CustomGameMode mode)
        {
            return IsHidden || (GameMode != CustomGameMode.All && GameMode != mode);
        }

        public string ApplyFormat(string value)
        {
            if (ValueFormat == OptionFormat.None) return value;
            return string.Format(Translator.GetString("Format." + ValueFormat), value);
        }

        // 外部からの操作
        public virtual void Refresh()
        {
            if (OptionBehaviour is not null and StringOption opt)
            {
                opt.TitleText.text = GetName();
                opt.ValueText.text = GetString();
                opt.oldValue = opt.Value = CurrentValue;
            }
        }
        public virtual void SetValue(int value)
        {
            int beforeValue = CurrentEntry.Value;
            int afterValue = CurrentEntry.Value = value;

            CallUpdateValueEvent(beforeValue, afterValue);
            Refresh();
            SyncAllOptions();
        }

        // 演算子オーバーロード
        public static OptionItem operator ++(OptionItem item)
            => item.Do(item => item.SetValue(item.CurrentValue + 1));
        public static OptionItem operator --(OptionItem item)
            => item.Do(item => item.SetValue(item.CurrentValue - 1));

        // 全体操作用
        public static void SwitchPreset(int newPreset)
        {
            CurrentPreset = Math.Clamp(newPreset, 0, 4);

            foreach (var op in AllOptions)
                op.Refresh();

            SyncAllOptions();
        }
        public static void SyncAllOptions()
        {
            if (
                Main.AllPlayerControls.Count() <= 1 ||
                AmongUsClient.Instance.AmHost == false ||
                PlayerControl.LocalPlayer == null
            ) return;

            RPC.SyncCustomSettingsRPC();
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