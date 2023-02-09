using System.Collections.Generic;
using System.IO;
using System.Xml;
using AmongUs.GameOptions;
using System.Data;
using System.Linq;
using System.Text;
using System;
using TownOfHost.Modules.Extensions;

namespace TownOfHost.Modules.Settings;

public static class SaveSettings
{
    /// <summary>
    /// 設定ファイルの生成
    /// </summary>
    public static void Generate()
    {
        List<BoolOptionNames> boolOptions = new();
        List<Int32OptionNames> intOptions = new();
        List<FloatOptionNames> floatOptions = new();
        List<ByteOptionNames> byteOptions = new();
        foreach (var setting in Enum.GetValues(typeof(BoolOptionNames)).Cast<BoolOptionNames>())
        {
            if (setting is BoolOptionNames.Invalid) continue;
            boolOptions.Add(setting);
        }
        foreach (var setting in Enum.GetValues(typeof(Int32OptionNames)).Cast<Int32OptionNames>())
        {
            if (setting is Int32OptionNames.Invalid) continue;
            intOptions.Add(setting);
        }
        foreach (var setting in Enum.GetValues(typeof(FloatOptionNames)).Cast<FloatOptionNames>())
        {
            if (setting is FloatOptionNames.Invalid) continue;
            floatOptions.Add(setting);
        }
        foreach (var setting in Enum.GetValues(typeof(ByteOptionNames)).Cast<ByteOptionNames>())
        {
            if (setting is ByteOptionNames.Invalid) continue;
            byteOptions.Add(setting);
        }

        // 元のファイルデータクリア
        using (var fileStream = new FileStream(SettingsGeneral.PRESET_FILE_1, FileMode.Open)) fileStream.SetLength(0);
        using var writer = new StreamWriter(SettingsGeneral.PRESET_FILE_1, true, Encoding.Unicode);
        string text = string.Empty;
        foreach (var boolOption in boolOptions)
        {
            text = boolOption.ToString() + ":" + GameOptionsManager.Instance.currentGameOptions.GetBool(boolOption).ToString().ToLower();
            writer.WriteLine(text);
        }
        foreach (var intOption in intOptions)
        {
            text = intOption.ToString() + ":" + GameOptionsManager.Instance.currentGameOptions.GetInt(intOption).ToString();
            writer.WriteLine(text);
        }
        foreach (var floatOption in floatOptions)
        {
            text = floatOption.ToString() + ":" + GameOptionsManager.Instance.currentGameOptions.GetFloat(floatOption).ToString();
            writer.WriteLine(text);
        }
        foreach (var byteOption in byteOptions)
        {
            text = byteOption.ToString() + ":" + GameOptionsManager.Instance.currentGameOptions.GetByte(byteOption).ToString();
            writer.WriteLine(text);
        }
    }

    /// <summary>
    /// プリセットの保存を行う
    /// </summary>
    public static void Save()
    {
        // 設定フォルダが存在しない時生成する
        if (!Directory.Exists(SettingsGeneral.SETTINGS_FOLDER)) Directory.CreateDirectory(SettingsGeneral.SETTINGS_FOLDER);

        Generate();
        Logger.Info("保存に成功しました。", "SaveSettings");
        Logger.SendInGame(string.Format(Translator.GetString("SettingsSaved")), false);
    }
}