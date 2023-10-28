using System;

namespace TownOfHost
{   //区切り用のItem
    public class DelimiterTextOptionItem : OptionItem
    {
        // 必須情報
        public IntegerValueRule Rule;

        // コンストラクタ
        public DelimiterTextOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue)
    : base(id, name, defaultValue, tab, isSingleValue)
        {
            IsWord = true;
            IsHeader = true;
        }
        public static DelimiterTextOptionItem Create(
        int id, string name, TabGroup tab, bool isSingleValue = false
    )
        {
            return new DelimiterTextOptionItem(
                id, name, 0, tab, isSingleValue
            );
        }
        public static DelimiterTextOptionItem Create(
        int id, Enum name, TabGroup tab, bool isSingleValue = false
    )
        {
            return new DelimiterTextOptionItem(
                id, name.ToString(), 0, tab, isSingleValue
            );
        }
        // Getter
        public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()
        {
            return Translator.GetString(Name);
        }
    }
}
