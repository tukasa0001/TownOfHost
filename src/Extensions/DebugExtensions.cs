using System;

namespace TownOfHost.Extensions;

public static class DebugExtensions
{
    public static void DebugLog(this object obj, string prefixText = "", string tag = "DebugLog", ConsoleColor color = ConsoleColor.DarkGray)
    {
        Logger.Color($"{prefixText}{obj}", tag, color);
    }
}