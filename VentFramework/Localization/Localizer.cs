using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using VentLib.Logging;
using static VentLib.Localization.LocalizedAttribute;

namespace VentLib.Localization;

public class Localizer
{
    /// <summary>
    /// Get or set the folder where languages for the Localizer exist
    /// </summary>
    public static string LanguageFolder = "./Languages/";
    public static string DefaultLanguage = "English";
    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set {
            _currentLanguage = value;
            translations = _loader.Get(_currentLanguage);
        }
    }

    private static Dictionary<string, Language> translations;
    private static string _currentLanguage = DefaultLanguage;
    private static LanguageLoader _loader = null!;
    private static string root;


    public static string Get(string keyPath, string? assemblyName = null)
    {
        assemblyName ??= Assembly.GetCallingAssembly().GetName().Name!;
        assemblyName = root == assemblyName ? "root" : assemblyName;
        if (translations.TryGetValue(assemblyName, out Language? language))
            return GetValueFromPath(language, keyPath);
        VentLogger.Fatal($"No Translations Exist for {keyPath}! Attempting to use Root Translations");
        language = translations["root"];
        return GetValueFromPath(language, keyPath);
    }

    public static void Initialize()
    {
        _loader = LanguageLoader.Load(LanguageFolder);
        translations = _loader.Get(CurrentLanguage);
    }

    public static void Load(Assembly assembly, bool rootAssembly = false)
    {
        string assemblyName = rootAssembly ? "root" : assembly.GetName().Name!;
        if (rootAssembly)
            root =  assembly.GetName().Name!;
        VentLogger.Info($"Loading Translations for {assemblyName}");
        if (!_loader.SupportedLanguages.ContainsKey(assemblyName) && assemblyName != "root")
            new DirectoryInfo(LanguageFolder).CreateSubdirectory(assemblyName);

        if (!translations.TryGetValue(assemblyName, out Language? language))
        {
            VentLogger.Fatal($"No Translations Exist for {assembly.GetName().Name}! Attempting to use Root Translations");
            language = translations["root"];
        }

        assembly.GetTypes().Do(cls => ReflectionLoader.RegisterClass(cls));
        Inject(language);
    }

    private static void Inject(Language language)
    {
        List<LocalizedAttribute> sortedAttributes = Attributes.Keys.ToList();
        sortedAttributes.Sort();

        foreach (LocalizedAttribute attribute in sortedAttributes)
        {
            ReflectionObject reflectionObject = Attributes[attribute];
            if (attribute.Key == null) continue;
            string value = GetValueFromPath(language, attribute.GetPath());
            reflectionObject.SetValue(value);
        }

        language.Dump();
    }

    private static string GetValueFromPath(Language language, string path, bool createIfNull = true)
    {
        Dictionary<object, object> dictionary = language.Translations;
        bool created = false;

        string[] subPaths = path.Split(".");
        for (int i = 0; i < subPaths.Length - 1; i++)
        {
            string subPath = subPaths[i];
            if (createIfNull && !dictionary.ContainsKey(subPath))
            {
                dictionary[subPath] = new Dictionary<object, object>();
                created = true;
            }
            dictionary = (Dictionary<object, object>)dictionary[subPath];
        }

        string finalPath = subPaths[^1];

        if (createIfNull && !dictionary.ContainsKey(finalPath)) {
            created = true;
            dictionary[finalPath] = "N/A";
        }

        if (created) language.Dump();

        return (string)dictionary[finalPath];
    }
}