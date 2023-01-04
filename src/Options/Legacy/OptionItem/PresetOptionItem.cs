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
            return CurrentValue switch
            {
                0 => TOHPlugin.Preset1.Value == (string)TOHPlugin.Preset1.DefaultValue ? Translator.GetString("Preset_1") : TOHPlugin.Preset1.Value,
                1 => TOHPlugin.Preset2.Value == (string)TOHPlugin.Preset2.DefaultValue ? Translator.GetString("Preset_2") : TOHPlugin.Preset2.Value,
                2 => TOHPlugin.Preset3.Value == (string)TOHPlugin.Preset3.DefaultValue ? Translator.GetString("Preset_3") : TOHPlugin.Preset3.Value,
                3 => TOHPlugin.Preset4.Value == (string)TOHPlugin.Preset4.DefaultValue ? Translator.GetString("Preset_4") : TOHPlugin.Preset4.Value,
                4 => TOHPlugin.Preset5.Value == (string)TOHPlugin.Preset5.DefaultValue ? Translator.GetString("Preset_5") : TOHPlugin.Preset5.Value,
                _ => null,
            };
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