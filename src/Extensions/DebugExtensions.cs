using System;
using VentLib.Logging;

namespace TownOfHost.Extensions;

public static class DebugExtensions
{
    public static void DebugLog(this object obj, string prefixText = "", string tag = "DebugLog", ConsoleColor color = ConsoleColor.DarkGray)
    {
        LogLevel tempLevel = new("OBJ", 0, color);
        VentLogger.Log(tempLevel,$"{prefixText}{obj}", tag);
    }
}