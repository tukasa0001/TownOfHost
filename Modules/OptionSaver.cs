using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TownOfHost.Modules;

public static class OptionSaver
{
    private static readonly DirectoryInfo SaveDataDirectoryInfo = new("./TOH_DATA/SaveData/");
    private static readonly FileInfo OptionSaverFileInfo = new($"{SaveDataDirectoryInfo.FullName}/Options.json");
    private static readonly LogHandler logger = Logger.Handler(nameof(OptionSaver));

    public static void Initialize()
    {
        if (!SaveDataDirectoryInfo.Exists)
        {
            SaveDataDirectoryInfo.Create();
        }
        if (!OptionSaverFileInfo.Exists)
        {
            OptionSaverFileInfo.Create().Dispose();
        }
    }
    /// <summary>現在のオプションからjsonシリアライズ用の辞書を生成</summary>
    private static Dictionary<int, int[]> GenerateOptionsDictionary()
    {
        Dictionary<int, int[]> options = new();
        foreach (var option in OptionItem.AllOptions)
        {
            // プリセット外のオプションは未対応
            if (option.IsSingleValue)
            {
                continue;
            }
            if (!options.TryAdd(option.Id, option.AllValues))
            {
                logger.Warn($"ID重複: {option.Id}");
            }
        }
        return options;
    }
    /// <summary>デシリアライズされた辞書を読み込み，オプション値を設定</summary>
    private static void LoadOptionsDictionary(Dictionary<int, int[]> options)
    {
        foreach (var option in options)
        {
            var id = option.Key;
            var values = option.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetAllValues(values);
            }
        }
    }
    /// <summary>現在のオプションをjsonファイルに保存</summary>
    public static void Save()
    {
        var jsonString = JsonSerializer.Serialize(GenerateOptionsDictionary(), new JsonSerializerOptions { WriteIndented = true, });
        File.WriteAllText(OptionSaverFileInfo.FullName, jsonString);
    }
    /// <summary>jsonファイルからオプションを読み込み</summary>
    public static void Load()
    {
        var jsonString = File.ReadAllText(OptionSaverFileInfo.FullName);
        // 空なら読み込まず，デフォルト値をセーブする
        if (jsonString.Length <= 0)
        {
            Save();
            return;
        }
        LoadOptionsDictionary(JsonSerializer.Deserialize<Dictionary<int, int[]>>(jsonString));
    }
}
