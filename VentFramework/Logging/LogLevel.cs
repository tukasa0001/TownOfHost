using System;
using System.Collections.Generic;

namespace VentLib.Logging;

public struct LogLevel : IComparable<LogLevel>
{
    private static readonly HashSet<LogLevel> Levels = new();
    internal static int LongestName = 7;

    public static LogLevel All = new("ALL", Int32.MinValue);
    public static LogLevel Trace = new("TRACE", -1);
    public static LogLevel Old = new("OLD", 0);
    public static LogLevel Debug = new("DEBUG", 1, ConsoleColor.DarkYellow);
    public static LogLevel Info = new("INFO", 2);
    public static LogLevel Warn = new("WARN", 3, ConsoleColor.Yellow);
    public static LogLevel Error = new("ERROR", 4, ConsoleColor.Red);
    public static LogLevel Fatal = new("FATAL", 6, ConsoleColor.DarkRed);

    public string Name;
    public ConsoleColor Color;
    public int Level { get; }

    public LogLevel(string name, uint level = 0, ConsoleColor color = ConsoleColor.DarkGray)
    {
        Name = name;
        Level = (int)level;
        Color = color;
        Levels.Add(this);
        LongestName = Math.Max(LongestName, name.Length);
    }

    public LogLevel(string name, uint level = 0)
    {
        Name = name;
        Level = (int)level;
        Color = VentLogger.Configuration.DefaultColor;
        Levels.Add(this);
        LongestName = Math.Max(LongestName, name.Length);
    }

    private LogLevel(string name, int level)
    {
        Name = name;
        Level = level;
        Color = ConsoleColor.DarkGray;
        Levels.Add(this);
        LongestName = Math.Max(LongestName, name.Length);
    }

    public LogLevel Similar(string name, ConsoleColor? color = null)
    {
        LogLevel level = this;
        level.Name = name;
        level.Color = color ?? VentLogger.Configuration.DefaultColor;
        return level;
    }

    public int CompareTo(LogLevel other) => Level.CompareTo(other.Level);

    public override bool Equals(object? obj)
    {
        if (obj is not LogLevel otherLevel) return false;
        return otherLevel.Name == Name && otherLevel.Level == Level;
    }

    public override int GetHashCode() => Name.GetHashCode() + Level.GetHashCode();
}