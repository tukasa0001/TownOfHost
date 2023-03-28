namespace TOHE;

public class TextOptionItem : OptionItem
{
    // 必須情報
    public IntegerValueRule Rule;

    // コンストラクタ
    public TextOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue)
    : base(id, name, defaultValue, tab, isSingleValue)
    {
        IsText = true;
        IsHeader = true;
    }
    public static TextOptionItem Create(
        int id, string name, TabGroup tab, bool isSingleValue = false
    )
    {
        return new TextOptionItem(
            id, name, 0, tab, isSingleValue
        );
    }

    // Getter
    public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
    public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
    public override string GetString()
    {
        return Translator.GetString(Name);
    }

    // Setter
    public override void SetValue(int value)
    {
    }
    public override void SetValueNoRpc(int value)
    {
    }
}