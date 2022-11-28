namespace TownOfHost
{
    public class FloatOptionItem : OptionItem
    {
        // 必須情報
        public FloatValueRule Rule;

        // コンストラクタ
        public FloatOptionItem(int id, string name, float defaultValue, TabGroup tab, bool isSingleValue, FloatValueRule rule)
        : base(id, name, rule.GetNearestIndex(defaultValue), tab, isSingleValue)
        {
            Rule = rule;
        }
        public static FloatOptionItem Create(
            int id, string name, FloatValueRule rule, float defaultValue, TabGroup tab, bool isSingleValue
        )
        {
            return new FloatOptionItem(
                id, name, defaultValue, tab, isSingleValue, rule
            );
        }

        // Getter
        public override int GetInt() => (int)Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()
        {
            return ApplyFormat(Rule.GetValueByIndex(CurrentValue).ToString());
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