using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

namespace TownOfHost.Modules;

public static class OptionSerializer
{
    private static LogHandler logger = Logger.Handler(nameof(OptionSerializer));
    private const string Header = "%TOHOptions%", Footer = "%End%";
    private static readonly DirectoryInfo exportDir = new("./TOH_DATA/OptionOutputs");
    public static void SaveToClipboard()
    {
        GUIUtility.systemCopyBuffer = GenerateOptionsString();
        Logger.SendInGame(Utils.ColorString(Color.green, Translator.GetString("Message.CopiedOptions")));
    }
    public static void SaveToFile()
    {
        if (!exportDir.Exists)
        {
            exportDir.Create();
        }
        var output = $"{exportDir.FullName}/Preset{OptionItem.CurrentPreset}_{DateTime.Now.Ticks}.txt";
        File.WriteAllText(output, GenerateOptionsString());
        Utils.OpenDirectory(exportDir.FullName);
        Logger.SendInGame(Utils.ColorString(Color.green, Translator.GetString("Message.ExportedOptions")));
    }
    public static void LoadFromClipboard()
    {
        LoadOptionsString(GUIUtility.systemCopyBuffer);
    }
    /// <summary>
    /// <see cref="GenerateModOptionsString"/>と<see cref="GenerateVanillaOptionsString"/>を合成し，<see cref="LoadOptionsString"/>で読み込める文字列データを生成します<br/>
    /// <see cref="Header"/>から始まって<see cref="Footer"/>で終わります<br/>
    /// '&amp;'がMod設定とバニラ設定を区切ります
    /// </summary>
    /// <returns>生成された文字列</returns>
    public static string GenerateOptionsString()
    {
        var builder = new StringBuilder(Header, 1024);
        builder.Append(GenerateModOptionsString());
        builder.Append('&');
        builder.Append(GenerateVanillaOptionsString());
        builder.Append(Footer);
        return builder.ToString();
    }
    /// <summary>
    /// <see cref="LoadModOptionsString"/>で読み込める現在のプリセットのMod設定の文字列データを生成します<br/>
    /// '!'が各オプションを区切り，','が前のオプションIDとの差とオプションの値を区切ります<br/>
    /// 前のオプションIDとの差が1の場合，空文字列で表現します<br/>
    /// 文字数削減のため，オプション値 = 0はオプションデータ全体を記述しないこと，オプション値 = 1はオプション値のみを省略することで表現します<br/>
    /// [オプション1のID - 0],[オプションの1の値]![オプション2のID - オプション1のID],[オプション2の値]!...<br/>
    /// 整数型は文字数削減のため62進数に変換します
    /// </summary>
    /// <returns>生成された文字列</returns>
    public static string GenerateModOptionsString()
    {
        var builder = new StringBuilder(1024);
        var options = OptionItem.AllOptions.Where(option => option is not PresetOptionItem).OrderBy(option => option.Id);
        var lastId = 0;
        foreach (var option in options)
        {
            var value = option.GetValue();
            if (value == 0)
            {
                continue;
            }
            var idDelta = option.Id - lastId;
            builder.Append(idDelta == 1 ? "" : Base62.ToBase62(idDelta)).Append(',').Append(value == 1 ? "" : Base62.ToBase62(value)).Append('!');
            lastId = option.Id;
        }
        return builder.ToString();
    }
    /// <summary>
    /// <see cref="LoadVanillaOptionsString"/>で読み込める現在のバニラ設定の文字列データを生成します<br/>
    /// <see cref="GameOptionsFactory.ToBytes"/>で生成されたバイト列を<see cref="Convert.ToBase64String"/>でBase64文字列に変換します
    /// </summary>
    /// <returns>生成された文字列</returns>
    public static string GenerateVanillaOptionsString()
    {
        byte[] bytes = GameOptionsManager.Instance.gameOptionsFactory.ToBytes(GameOptionsManager.Instance.CurrentGameOptions);
        return Convert.ToBase64String(bytes);
    }
    /// <summary>
    /// <see cref="GenerateOptionsString"/>で生成された形式の文字列を読み込んで現在のプリセットを上書きします
    /// </summary>
    /// <param name="source">オプション情報の文字列</param>
    public static void LoadOptionsString(string source)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            Logger.SendInGame(Translator.GetString("Message.OnlyHostCanLoadOptions"));
            return;
        }

        if (string.IsNullOrEmpty(source))
        {
            logger.Info("文字列が空");
            goto Failed;
        }

        var headerAt = source.IndexOf(Header);
        if (headerAt < 0)
        {
            logger.Info("ヘッダがありません");
            goto Failed;
        }
        var footerAt = source.IndexOf(Footer, headerAt);
        if (footerAt < 0)
        {
            logger.Info("フッタがありません");
            goto Failed;
        }
        // ヘッダ以前とフッタ以降を削除
        source = source[(headerAt + Header.Length)..footerAt];

        try
        {
            var entries = source.Split('&');
            LoadModOptionsString(entries[0]);
            LoadVanillaOptionsString(entries[1]);
            Logger.SendInGame(Utils.ColorString(Color.green, Translator.GetString("Message.LoadedOptions")));
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
    /// <summary>
    /// <see cref="GenerateModOptionsString"/>で生成された形式の文字列を読み込んで現在のプリセットを上書きします
    /// </summary>
    /// <param name="source">オプション情報の文字列</param>
    public static void LoadModOptionsString(string source)
    {
        var modOptions = source.Split('!', StringSplitOptions.RemoveEmptyEntries);
        var parsedModOptions = new Dictionary<int, int>(modOptions.Length);
        var lastId = 0;
        foreach (var modOption in modOptions)
        {
            var split = modOption.Split(',');
            var idDelta = split[0] == "" ? 1 : Base62.ToInt(split[0]);
            lastId += idDelta;
            var value = split[1] == "" ? 1 : Base62.ToInt(split[1]);
            parsedModOptions[lastId] = value;
        }
        foreach (var option in OptionItem.AllOptions)
        {
            if (option is PresetOptionItem)
            {
                continue;
            }
            option.SetValue(parsedModOptions.TryGetValue(option.Id, out var value) ? value : 0, false);
        }
        OptionItem.SyncAllOptions();
    }
    /// <summary>
    /// <see cref="GenerateVanillaOptionsString"/>で生成された形式の文字列を読み込んで適用します
    /// </summary>
    /// <param name="source">オプション情報の文字列</param>
    public static void LoadVanillaOptionsString(string source)
    {
        var bytes = Convert.FromBase64String(source);
        var options = GameOptionsManager.Instance.gameOptionsFactory.FromBytes(bytes);
        GameOptionsManager.Instance.GameHostOptions = GameOptionsManager.Instance.CurrentGameOptions = options;
        GameManager.Instance.LogicOptions.SetGameOptions(options);
        GameManager.Instance.LogicOptions.SyncOptions();
    }
}
