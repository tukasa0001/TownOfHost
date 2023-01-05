using System;

namespace TownOfHost.Managers;

public class SpecialDates
{
    public static bool IsChristmas = DateTime.Now is { Month: 12, Day: 24 or 25 };
    public static bool IsInitialRelease = DateTime.Now is { Month: 12, Day: 4 };
}