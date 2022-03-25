using System;
using System.IO;
using System.Collections.Generic;
namespace TownOfHost
{
    public static class Translator
    {
        public static Dictionary<string, Dictionary<int, string>> tr;
        static Translator()
        {
            Logger.info("Language Dictionary Initialize...");
            loadLangs();
            Logger.info("Language Dictionary Initialize Finished");
        }
        public static void loadLangs()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("TownOfHost.Resources.string.csv");
            var sr = new StreamReader(stream);
            tr = new Dictionary<string, Dictionary<int, string>>();

            string[] header = sr.ReadLine().Split(',');

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] values = line.Split(',');
                List<string> fields = new List<string>(values);
                Dictionary<int, string> tmp = new Dictionary<int, string>();
                for (var i = 1; i < fields.Count; ++i)
                {
                    if (fields[i] != string.Empty && fields[i].TrimStart()[0] == '"')
                    {
                        while (fields[i].TrimEnd()[fields[i].TrimEnd().Length - 1] != '"')
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
                tr.Add(fields[0], tmp);
            }
        }

        public static string getString(string s)
        {
            var langId = TranslationController.InstanceExists ? TranslationController.Instance.CurrentLanguage.languageID : SupportedLangs.English;
            if (Options.forceJapanese) langId = SupportedLangs.Japanese;
            return getString(s, langId);
        }

        public static string getString(string s, SupportedLangs langId)
        {
            var res = "";
            if (tr.TryGetValue(s, out var dic))
            {
                if (dic.TryGetValue((int)langId, out res))
                {
                    return res;
                }
                else
                {
                    if (dic.TryGetValue(0, out res))
                    {
                        Logger.info($"Redirect to English: {res}");
                        return res;
                    }
                    else
                    {
                        return $"<INVALID:{s}>";
                    }
                }
            }
            else
            {
                return $"<INVALID:{s}>";
            }
        }
    }
}