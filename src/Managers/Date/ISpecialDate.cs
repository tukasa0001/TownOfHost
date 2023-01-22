using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace TownOfHost.Managers.Date;

public interface ISpecialDate
{
    internal static List<ISpecialDate> SpecialDates = new();

    bool IsDate();

    void DoDuringDate();

    public void Create() {
        SpecialDates.Add(this);
    }

    internal static void CheckDates()
    {
        SpecialDates.Where(d => d.IsDate()).Do(d => d.DoDuringDate());
    }
}