/*#nullable enable
using System.Collections.Generic;

namespace TownOfHost.ReduxOptions;

public class OptionPage
{
    public StringOption Behaviour;
    public List<OptionHolder> Holders = new();
    public string Name;
    private int pageIndex;

    public OptionPage(string name)
    {
        this.Name = name;
    }

    public OptionHolder NextPage()
    {
        Holders[pageIndex].EnabledOptions();
        return pageIndex + 1 < Holders.Count ? Holders[++pageIndex] : Holders[pageIndex = 0];
    }

    public OptionHolder PreviousPage()
    {
        Holders[pageIndex].EnabledOptions();
        return pageIndex - 1 >= 0 ? Holders[--pageIndex] : Holders[pageIndex = Holders.Count - 1];
    }

    public OptionHolder? CurrentPage()
    {
        if (Holders.Count == 0) return null;
        Holders[pageIndex].Behaviour.gameObject.SetActive(true);
        return Holders[pageIndex];
    }

    public void Add(OptionHolder holder)
    {
        this.Holders.Add(holder);
    }

    public void Clear() => this.Holders.Clear();
}*/