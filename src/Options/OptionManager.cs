using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TownOfHost.Patches.Menus;

namespace TownOfHost.Options;

public class OptionManager
{
    public List<GameOptionTab> Tabs { get; private set; } = new();
    private List<OptionHolder> options = new();
    public List<OptionHolder> AllHolders = new();
    public List<OptionHolder>? ReceivedOptions;
    public ConfigFile GeneralConfig;
    public List<ConfigFile> Presets;

    private bool Loaded => AllHolders.Count != 0;
    private readonly Dictionary<object, OptionValueHolder> _optionBindings = new();
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
    }

    public ConfigFile incrementPreset() => fileIndex + 1 < Presets.Count ? Presets[++fileIndex] : Presets[fileIndex = 0];
    public ConfigFile decrementPreset() => fileIndex - 1 > 0 ? Presets[--fileIndex] : Presets[fileIndex = Presets.Count];
    public ConfigFile GetPreset() => Presets[fileIndex];

    public List<OptionHolder> Options() => this.options;

    public List<OptionHolder> PreviewOptions() => AmongUsClient.Instance.AmHost ? this.options : this.ReceivedOptions ?? this.options;

    public void Add(OptionHolder holder)
    {
        this.options.Add(holder);
        if (Loaded)
            this.AllHolders.Add(holder);
        holder.GetHoldersRecursive().Do(h => h.valueHolder?.UpdateBinding());
    }

    public void SetTabs(IEnumerable<GameOptionTab> newTabs)
    {
        Tabs.Do(tab => tab.SetActive(false));
        Tabs = newTabs.ToList();
        Tabs.Do(tab => tab.SetActive(true));
        if (GameOptMenuStartPatch.Instance != null)
            GameOptMenuStartPatch.Postfix(__instance: GameOptMenuStartPatch.Instance);
    }

    public void AddTab(GameOptionTab tab)
    {
        Tabs.Add(tab);
    }

    private ConfigFile CreatePreset(string? exactPath = null)
    {
        BepInPlugin metadata = MetadataHelper.GetMetadata(TOHPlugin.Instance);
        string path = exactPath ?? Utility.CombinePaths(Paths.ConfigPath, metadata.GUID + "-preset" + (Presets.Count + 1) + ".cfg");
        return new ConfigFile(path, true, metadata);
    }
}