using System;
using TMPro;
using TownOfHost.Extensions;
using TownOfHost.Patches.Network;
using UnityEngine;

namespace TownOfHost.Managers.Date;

public class SpecialDate: ISpecialDate
{
    public static SpecialDate Christmas = new((12, 24), (12, 25));
    public static SpecialDate ShiftyBirthday = new((1, 24), (1, 26));

    static SpecialDate()
    {
        ((ISpecialDate)Christmas).Create();
        Christmas.text = "Merry Christmas!";
        Christmas.color = TOHPlugin.ModColor.ToColor();

        ((ISpecialDate)ShiftyBirthday).Create();
        ShiftyBirthday.text = "Happy Birthday\nShifty!";
        ShiftyBirthday.color = new Color(1f, 0.64f, 0.79f);
    }

    private (int, int) dayRange;
    private (int, int) monthRange;
    private int year = -1;

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
        DateTime startDate = new(year == -1 ? now.Year : year, monthRange.Item1, dayRange.Item1);
        DateTime endDate = new(year == -1 ? now.Year : year, monthRange.Item2, dayRange.Item2);
        return startDate.CompareTo(now) <= 0 && endDate.CompareTo(now) >= 0;
    }

    public void DoDuringDate()
    {
        string text = this.text;
        Color color = this.color;
        VersionShowerStartPatch.SpecialEventText!.text = text;
        VersionShowerStartPatch.SpecialEventText.color = color;
        VersionShowerStartPatch.SpecialEventText.fontStyle = FontStyles.Bold;
    }
}