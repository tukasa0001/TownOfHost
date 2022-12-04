namespace TownOfHost
{
    public class BooleanOptionItem : OptionItem
    {
        public const string TEXT_true = "ColoredOn";
        public const string TEXT_false = "ColoredOff";

        // コンストラクタ
        public BooleanOptionItem(int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue)
        : base(id, name, defaultValue ? 1 : 0, tab, isSingleValue)
        {
        }
        public static BooleanOptionItem Create(
            int id, string name, bool defaultValue, TabGroup tab, bool isSingleValue
        )
        {
            return new BooleanOptionItem(
                id, name, defaultValue, tab, isSingleValue
            );
        }

        // Getter
        public override string GetString()
        {
            return Translator.GetString(GetBool() ? TEXT_true : TEXT_false);
        }

        // Setter
        public override void SetValue(int value)
        {
            base.SetValue(value % 2 == 0 ? 0 : 1);
        }
    }
}