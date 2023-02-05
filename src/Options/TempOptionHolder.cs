/*using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using VentLib.Options;

namespace TownOfHost.Options;

public class TempOption
{
    private List<Option> temporaryOptions = new();


    public void Add(Option option)
    {
        temporaryOptions.Add(option);
        TOHPlugin.OptionManager.Add(option);
    }

    public List<Option> GetTempOptions() => temporaryOptions;

    public void DeleteAll()
    {
        List<Option> allHolders = temporaryOptions.SelectMany(o => o.GetHoldersRecursive()).ToList();
        TOHPlugin.OptionManager.Options().RemoveAll(p => allHolders.Contains(p));
        TOHPlugin.OptionManager.Options().RemoveAll(p => allHolders.Contains(p));
        allHolders.Do(h =>
        {
            if (h.Tab == null) return;
            h.Tab.GetHolders().Remove(h);
        });
        temporaryOptions.Clear();
    }
}*/