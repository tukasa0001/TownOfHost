using System;
using System.Linq;
using System.Text;

namespace TownOfHost.Modules;

public static class OptionSerializer
{
    private static LogHandler logger = Logger.Handler(nameof(OptionSerializer));
    private const string Header = "%TOHOptions%";
    /// <summary>
    /// 現在の設定のうち，プリセットでなく，Valueが0でないものを<see cref="FromHex"/>で読み込める16進形式の文字列に変換します<br/>
    /// <see cref="Header"/>から始まり，'!'が各オプションを区切り，','がオプションIDとオプションの値を区切ります
    /// </summary>
    /// <returns>変換された文字列 ([Header][オプション1のID],[オプションの1の値]![オプション2のID],[オプション2の値]!...)</returns>
    public static string ToHex()
    {
        var builder = new StringBuilder(Header);
        foreach (var option in OptionItem.AllOptions)
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
        if (!hex.StartsWith(Header))
        {
            logger.Info("フォーマットに不適合");
            goto Failed;
        }
        try
        {
            foreach (var option in OptionItem.AllOptions)
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

                var option = OptionItem.AllOptions.FirstOrDefault(option => option.Id == id);
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
}
