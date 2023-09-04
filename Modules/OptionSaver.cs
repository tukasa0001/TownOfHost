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
            SaveDataDirectoryInfo.Attributes |= FileAttributes.Hidden;
        }
        if (!OptionSaverFileInfo.Exists)
        {
            OptionSaverFileInfo.Create().Dispose();
        }
    }
    /// <summary>現在のオプションからjsonシリアライズ用のオブジェクトを生成</summary>
    private static SerializableOptionsData GenerateOptionsData()
    {
        Dictionary<int, int> singleOptions = new();
        Dictionary<int, int[]> presetOptions = new();
        foreach (var option in OptionItem.AllOptions)
        {
            if (option.IsSingleValue)
            {
                if (!singleOptions.TryAdd(option.Id, option.SingleValue))
                {
                    logger.Warn($"SingleOptionのID {option.Id} が重複");
                }
            }
            else if (!presetOptions.TryAdd(option.Id, option.AllValues))
            {
                logger.Warn($"プリセットオプションのID {option.Id} が重複");
            }
        }
        return new SerializableOptionsData
        {
            Version = Version,
            SingleOptions = singleOptions,
            PresetOptions = presetOptions,
        };
    }
    /// <summary>デシリアライズされたオブジェクトを読み込み，オプション値を設定</summary>
    private static void LoadOptionsData(SerializableOptionsData serializableOptionsData)
    {
        if (serializableOptionsData.Version != Version)
        {
            // 今後バージョン間の移行方法を用意する場合，ここでバージョンごとの変換メソッドに振り分ける
            logger.Info($"読み込まれたオプションのバージョン {serializableOptionsData.Version} が現在のバージョン {Version} と一致しないためデフォルト値で上書きします");
            Save();
            return;
        }
        Dictionary<int, int> singleOptions = serializableOptionsData.SingleOptions;
        Dictionary<int, int[]> presetOptions = serializableOptionsData.PresetOptions;
        foreach (var singleOption in singleOptions)
        {
            var id = singleOption.Key;
            var value = singleOption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetValue(value, doSave: false);
            }
        }
        foreach (var presetOption in presetOptions)
        {
            var id = presetOption.Key;
            var values = presetOption.Value;
            if (OptionItem.FastOptions.TryGetValue(id, out var optionItem))
            {
                optionItem.SetAllValues(values);
            }
        }
    }
    /// <summary>現在のオプションをjsonファイルに保存</summary>
    public static void Save()
    {
        // 接続済みで，ホストじゃなければ保存しない
        if (AmongUsClient.Instance != null && !AmongUsClient.Instance.AmHost)
        {
            return;
        }
        var jsonString = JsonSerializer.Serialize(GenerateOptionsData(), new JsonSerializerOptions { WriteIndented = true, });
        File.WriteAllText(OptionSaverFileInfo.FullName, jsonString);
    }
    /// <summary>jsonファイルからオプションを読み込み</summary>
    public static void Load()
    {
        var jsonString = File.ReadAllText(OptionSaverFileInfo.FullName);
        // 空なら読み込まず，デフォルト値をセーブする
        if (jsonString.Length <= 0)
        {
            logger.Info("オプションデータが空のためデフォルト値を保存");
            Save();
            return;
        }
        LoadOptionsData(JsonSerializer.Deserialize<SerializableOptionsData>(jsonString));
    }

    /// <summary>json保存に適したオプションデータ</summary>
    public class SerializableOptionsData
    {
        public int Version { get; init; }
        /// <summary>プリセット外のオプション</summary>
        public Dictionary<int, int> SingleOptions { get; init; }
        /// <summary>プリセット内のオプション</summary>
        public Dictionary<int, int[]> PresetOptions { get; init; }
    }

    /// <summary>オプションの形式に互換性のない変更(プリセット数変更など)を加えるときはここの数字を上げる</summary>
    public static readonly int Version = 0;
}
