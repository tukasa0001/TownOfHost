using System;
using System.IO;
using System.Collections.Generic;

namespace TownOfHost {
    public class Translator
    {
        public static Dictionary<string, Dictionary<int, string>> tr;
        public Translator()
        {
            Logger.info("Langage Dictionary Initalize...");
            load();
            Logger.info("Langage Dictionary Initalize Finished");
        }
        public void load()
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
                Dictionary<int, string> tmp = new Dictionary<int, string>();
                for(var i=1; i < values.Length; i++)
                {
                    tmp.Add(Int32.Parse(header[i]),values[i]);
                }
                tr.Add(values[0],tmp);
            }
        }

        public string getString(string s)
        {
            int langId = TranslationController._instance != null ? (int)TranslationController.Instance.CurrentLanguage.languageID:0;
            var res = "";
            if(tr.TryGetValue(s,out var dic))
            {
                if(dic.TryGetValue(langId,out res))
                {
                    return res;
                } else {
                    if(dic.TryGetValue(0,out res))
                    {
                        return res;
                    }else{
                        return $"<INVALID STRING:{s}>";
                    }
                }
            } else {
                return $"<INVALID STRING:{s}>";
            }
        }
    }
}
