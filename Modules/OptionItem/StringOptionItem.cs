namespace TownOfHost
{
    public class StringOptionItem : OptionItem
    {
        // 必須情報
        public IntegerValueRule Rule;
        public string[] Selections;

        // コンストラクタ
        public StringOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, string[] selections)
        : base(id, name, defaultValue, tab, isSingleValue)
        {
            Rule = (0, selections.Length - 1, 1);
            Selections = selections;
        }
        public static StringOptionItem Create(
            int id, string name, string[] selections, int defaultIndex, TabGroup tab, bool isSingleValue
        )
        {
            return new StringOptionItem(
                id, name, defaultIndex, tab, isSingleValue, selections
            );
        }

        // Getter
        public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()
        {
            return Translator.GetString(Selections[Rule.GetValueByIndex(CurrentValue)]);
        }
        public override int GetValue()
            => Rule.RepeatIndex(base.GetValue());

        // Setter
        public override void SetValue(int value)
        {
            base.SetValue(Rule.RepeatIndex(value));
        }
    }
}