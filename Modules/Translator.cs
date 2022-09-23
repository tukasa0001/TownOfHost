using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace TownOfHost
{
    public static class Translator
    {
        public static Dictionary<string, Dictionary<int, string>> tr;
        public const string LANGUAGE_FOLDER_NAME = "Language";
        public static void Init()
        {
            Logger.Info("Language Dictionary Initialize...", "Translator");
            LoadLangs();
            Logger.Info("Language Dictionary Initialize Finished", "Translator");
        }
        public static void LoadLangs()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("TownOfHost.Resources.string.csv");
            var sr = new StreamReader(stream);
            tr = new Dictionary<string, Dictionary<int, string>>();

            string[] header = sr.ReadLine().Split(',');

            int currentLine = 1;

            while (!sr.EndOfStream)
            {
                currentLine++;
                string line = sr.ReadLine();
                if (line == "" || line[0] == '#') continue;
                string[] values = line.Split(',');
                List<string> fields = new(values);
                Dictionary<int, string> tmp = new();
                try
                {
                    for (var i = 1; i < fields.Count; ++i)
                    {
                        if (fields[i] != string.Empty && fields[i].TrimStart()[0] == '"')
                        {
                            while (fields[i].TrimEnd()[^1] != '"')
                            {
                                fields[i] = fields[i] + "," + fields[i + 1];
                                fields.RemoveAt(i + 1);
                            }
                        }
                    }
                    for (var i = 1; i < fields.Count; i++)
                    {
                        var tmp_str = fields[i].Replace("\\n", "\n").Trim('"');
                        tmp.Add(Int32.Parse(header[i]), tmp_str);
                    }
                    if (tr.ContainsKey(fields[0])) { Logger.Warn($"翻訳用CSVに重複があります。{currentLine}行目: \"{fields[0]}\"", "Translator"); continue; }
                    tr.Add(fields[0], tmp);
                }
                catch
                {
                    var err = $"翻訳用CSVファイルに誤りがあります。{currentLine}行目:";
                    foreach (var c in fields) err += $" [{c}]";
                    Logger.Error(err, "Translator");
                    continue;
                }
            }
            // カスタム翻訳ファイルの読み込み
            if (!Directory.Exists(LANGUAGE_FOLDER_NAME)) Directory.CreateDirectory(LANGUAGE_FOLDER_NAME);

            // 翻訳テンプレートの作成
            CreateTemplateFile();
            foreach (var lang in Enum.GetValues(typeof(SupportedLangs)))
            {
                if (File.Exists(@$"./{LANGUAGE_FOLDER_NAME}/{lang}.dat"))
                    LoadCustomTranslation($"{lang}.dat", (SupportedLangs)lang);
            }
        }

        public static string GetString(string s, Dictionary<string, string> replacementDic = null)
        {
            var langId = TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.English;
            if (Main.ForceJapanese.Value) langId = SupportedLangs.Japanese;
            string str = GetString(s, langId);
            if (replacementDic != null)
                foreach (var rd in replacementDic)
                {
                    str = str.Replace(rd.Key, rd.Value);
                }
            return str;
        }

        public static string GetString(string str, SupportedLangs langId)
        {
            var res = $"<INVALID:{str}>";
            if (tr.TryGetValue(str, out var dic) && (!dic.TryGetValue((int)langId, out res) || res == "")) //strに該当する&無効なlangIdかresが空
            {
                res = $"*{dic[0]}";
            }
            return res;
        }

        public static void LoadCustomTranslation(string filename, SupportedLangs lang)
        {
            string path = @$"./{LANGUAGE_FOLDER_NAME}/{filename}";
            if (File.Exists(path))
            {
                Logger.Info($"カスタム翻訳ファイル「{filename}」を読み込み", "LoadCustomTranslation");
                using StreamReader sr = new(path, Encoding.GetEncoding("UTF-8"));
                string text;
                string[] tmp = { };
                while ((text = sr.ReadLine()) != null)
                {
                    tmp = text.Split(":");
                    if (tmp.Length > 1 && tmp[1] != "")
                    {
                        try
                        {
                            tr[tmp[0]][(int)lang] = tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n").Replace("\\r", "\r");
                        }
                        catch (KeyNotFoundException)
                        {
                            Logger.Warn($"「{tmp[0]}」は有効なキーではありません。", "LoadCustomTranslation");
                        }
                    }
                }
            }
            else
            {
                Logger.Error($"カスタム翻訳ファイル「{filename}」が見つかりませんでした", "LoadCustomTranslation");
            }
        }

        private static void CreateTemplateFile()
        {
            var text = "";
            foreach (var title in tr) text += $"{title.Key}:\n";
            File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/template.dat", text);
            text = "";
            foreach (var title in tr) text += $"{title.Key}:{title.Value[0].Replace("\n", "\\n").Replace("\r", "\\r")}\n";
            File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/template_English.dat", text);
        }
    }
}