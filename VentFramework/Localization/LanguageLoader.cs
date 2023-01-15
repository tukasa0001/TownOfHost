using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using VentLib.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VentLib.Localization;

public class LanguageLoader
{
    internal static ISerializer Serializer = new SerializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
    public Dictionary<string, HashSet<string>> SupportedLanguages = new();
    private DirectoryInfo langDirectory;
    private Dictionary<string, Dictionary<string, Language>> languageAssemblyDictionary = new();

    private IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();

    public static LanguageLoader Load(string directory)
    {
        DirectoryInfo langDirectory = new(directory);
        if (!langDirectory.Exists) langDirectory.Create();
        return new LanguageLoader(langDirectory);
    }

    public Dictionary<string, Language> Get(string language)
    {
        if (languageAssemblyDictionary.TryGetValue(language, out Dictionary<string, Language>? assemblyDictionary))
            return assemblyDictionary;
        VentLogger.Fatal($"Current Language {language} Does Not Exist! Defaulting to {Localizer.DefaultLanguage}");
        if (languageAssemblyDictionary.TryGetValue(Localizer.DefaultLanguage, out assemblyDictionary))
            return assemblyDictionary;

        languageAssemblyDictionary[Localizer.DefaultLanguage] = new Dictionary<string, Language>();


        Language newLang = languageAssemblyDictionary[Localizer.DefaultLanguage]["root"] = new Language
        {
            Name = Localizer.DefaultLanguage,
            Authors = new List<string> { "Default" },
            Translations = new Dictionary<object, object>(),
            File = new FileInfo(Path.Join(Localizer.LanguageFolder, $"lang_{Localizer.DefaultLanguage}.yaml"))
        };

        newLang.Dump();

        return languageAssemblyDictionary[Localizer.DefaultLanguage];
    }

    private LanguageLoader(DirectoryInfo directory)
    {
        langDirectory = directory;
        LoadAll(langDirectory, true);
    }

    private void LoadAll(DirectoryInfo directory, bool root = false)
    {
        directory.EnumerateFiles("lang_*").Do(f =>
        {
            VentLogger.Info($"Loading Language File: {f}");
            try
            {
                string space = root ? "root" : directory.Name;
                Language lang = LoadFile(f, root ? "root" : directory.Name);
                if (!SupportedLanguages.TryGetValue(space, out HashSet<string>? langs))
                    langs = new HashSet<string>();
                langs.Add(lang.Name);
                SupportedLanguages[space] = langs;
            } catch (Exception e) {
                VentLogger.Exception(e, "Unable to load Language File: ");
            }
        });
    }

    private Language LoadFile(FileInfo file, string space)
    {
        StreamReader reader = file.OpenText();
        string yamlString = reader.ReadToEnd();
        reader.Close();
        Language language = deserializer.Deserialize<Language>(yamlString);
        if (!languageAssemblyDictionary.ContainsKey(language.Name))
            languageAssemblyDictionary[language.Name] = new Dictionary<string, Language>();
        languageAssemblyDictionary[language.Name][space] = language;
        language.File = file;
        return language;
    }
}