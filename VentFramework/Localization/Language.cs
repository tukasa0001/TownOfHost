using System.Collections.Generic;
using System.IO;
using System.Text;
using VentLib.Logging;
using YamlDotNet.Serialization;

namespace VentLib.Localization;

public class Language
{
    public string Name;
    public List<string> Authors;
    public Dictionary<object, object> Translations;

    [YamlIgnore]
    internal FileInfo? File;

    internal void Dump()
    {
        if (File == null) return;
        VentLogger.Trace($"Saving Translation File: {File}");

        FileStream stream = File.Open(File.Exists ? FileMode.Truncate : FileMode.CreateNew);
        string yamlString = LanguageLoader.Serializer.Serialize(this);
        stream.Write(Encoding.ASCII.GetBytes(yamlString));
        stream.Close();
    }
}