using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TownOfHost.Options;

public class TempOptionHolder
{
    private List<OptionHolder> temporaryOptions = new();


    public void Add(OptionHolder option)
    {
        temporaryOptions.Add(option);
        TOHPlugin.OptionManager.Add(option);
    }

    public List<OptionHolder> GetTempOptions() => temporaryOptions;

    public void DeleteAll()
    {
        List<OptionHolder> allHolders = temporaryOptions.SelectMany(o => o.GetHoldersRecursive()).ToList();
        TOHPlugin.OptionManager.Options().RemoveAll(p => allHolders.Contains(p));
        TOHPlugin.OptionManager.Options().RemoveAll(p => allHolders.Contains(p));
        allHolders.Do(h =>
        {
            if (h.Tab == null) return;
            h.Tab.GetHolders().Remove(h);
        });
        temporaryOptions.Clear();
    }
}