using System;
using TownOfHost.Extensions;
using TownOfHost.Patches.Network;
using UnityEngine;
using VentLib.Utilities;

namespace TownOfHost.Managers.Date;

public struct SpecialDate: ISpecialDate
{
    public static SpecialDate Christmas = new((12, 24), (12, 25));

    static SpecialDate()
    {
        ((ISpecialDate)Christmas).Create();
        Christmas.text = "Merry Christmas!";
        Christmas.color = TOHPlugin.ModColor.ToColor();
    }

    private (int, int) dayRange;
    private (int, int) monthRange;
    private int year;

    internal string text;
    internal Color color;

    public SpecialDate(int month, int day, int year = -1)
    {
        dayRange = (day, day);
        monthRange = (month, month);
        this.year = year;
    }

    public SpecialDate((int month, int day) startDate, (int month, int day) endDate)
    {
        dayRange = (startDate.day, endDate.day);
        monthRange = (startDate.month, endDate.month);
        this.year = -1;
    }

    public bool IsDate()
    {
        DateTime now = DateTime.Now;
        if (this.year != -1 && now.Year != this.year) return false;
        if (monthRange.Item1 > now.Month) return false; // If the start month is greater than the current month false
        if (monthRange.Item2 < now.Month) return false; // If the end month is less than the current month return false
        if (dayRange.Item1 > now.Day) return false; // If the start day is greater than the current day false
        return dayRange.Item2 >= now.Day; // If the end day is greater than or equal to the current day, true
    }

    public static bool IsChristmas = DateTime.Now is { Month: 12, Day: 24 or 25 };
    /*public static bool IsInitialRelease = DateTime.Now is { Month: 12, Day: 4 };*/

    public void DoDuringDate()
    {
        string text = this.text;
        Color color = this.color;
        Async.ScheduleThreaded(() =>
        {
            VersionShowerStartPatch.SpecialEventText!.text = text;
            VersionShowerStartPatch.SpecialEventText.color = color;
        }, 5);
    }
}