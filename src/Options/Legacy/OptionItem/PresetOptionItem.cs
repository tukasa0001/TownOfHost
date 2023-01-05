using System;

namespace TownOfHost
{
    public class PresetOptionItem : OptionItem
    {
        // 必須情報
        public IntegerValueRule Rule;

        // コンストラクタ
        public PresetOptionItem(int defaultValue, TabGroup tab)
        : base(0, "Preset", defaultValue, tab, true)
        {
            Rule = (0, 4, 1);
        }
        public static PresetOptionItem Create(int defaultValue, TabGroup tab)
        {
            return new PresetOptionItem(defaultValue, tab);
        }

        // Getter
        public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()
        {
            throw new NotImplementedException("Decommissioning Method");
        }
        public override int GetValue()
            => Rule.RepeatIndex(base.GetValue());

        // Setter
        public override void SetValue(int value)
        {
            base.SetValue(Rule.RepeatIndex(value));
            SwitchPreset(Rule.RepeatIndex(value));
        }
    }
}