using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Interface.Menus;

namespace TownOfHost.ReduxOptions;

public class OptionManager
{

    /*public static OptionPage CrewmatePage = new("Crewmate Options");
    public static OptionPage ImpostorPage = new("Impostor Options");
    public static OptionPage NeutralPage = new("Neutral Options");
    public static OptionPage NeutEvilPage = new("Neutral Evil Options");*/

    public List<GameOptionTab> Tabs = new();
    private List<OptionHolder> options = new();
    public List<OptionHolder> AllHolders = new();
    public ConfigFile GeneralConfig;
    public List<ConfigFile> Presets = new();
    //public List<OptionPage> Pages = new();

    private Dictionary<object, OptionValueHolder> OptionBindings = new();
    private int fileIndex;

    public OptionManager()
    {
        BepInPlugin metadata = MetadataHelper.GetMetadata(TOHPlugin.Instance);
        string path = Utility.CombinePaths(Paths.ConfigPath, metadata.GUID + ".cfg");
        this.GeneralConfig = new ConfigFile(path, true, metadata);
        this.Presets = Utility.GetUniqueFilesInDirectories(new[] { Paths.ConfigPath }, "*-preset.cfg").Select(CreatePreset).ToList();
        if (Presets.Count != 0) return;
        for (int i = 0; i < 4; i++)
            Presets.Add(CreatePreset());

        /*Pages.Add(ImpostorPage);
        Pages.Add(CrewmatePage);
        Pages.Add(NeutralPage);
        Pages.Add(NeutEvilPage);*/
    }

    public ConfigFile incrementPreset() => fileIndex + 1 < Presets.Count ? Presets[++fileIndex] : Presets[fileIndex = 0];
    public ConfigFile decrementPreset() => fileIndex - 1 > 0 ? Presets[--fileIndex] : Presets[fileIndex = Presets.Count];
    public ConfigFile GetPreset() => Presets[fileIndex];

    public void BindValueHolder(object key, OptionValueHolder value)
    {
        if (OptionBindings.ContainsKey(key))
            throw new ArgumentException($"Key \"{key}\" is already bound to an option");
        OptionBindings.Add(key, value);
    }

    public IEnumerable<OptionHolder> Options() => this.options;

    public void Add(OptionHolder holder)
    {
        this.options.Add(holder);
        holder.GetHoldersRecursive().Do(h => h.valueHolder?.UpdateBinding());
    }

    public void AddTab(GameOptionTab tab)
    {
        Tabs.Add(tab);
    }

    private ConfigFile CreatePreset(string exactPath = null)
    {
        BepInPlugin metadata = MetadataHelper.GetMetadata(TOHPlugin.Instance);
        string path = exactPath ?? Utility.CombinePaths(Paths.ConfigPath, metadata.GUID + "-preset" + (Presets.Count + 1) + ".cfg");
        return new ConfigFile(path, true, metadata);
    }
}