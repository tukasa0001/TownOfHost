using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;

namespace VentLib.Logging;

public static class VentLogger
{
    public static void Trace(string message, string? tag = null) => Log(LogLevel.Trace, message, tag);
    public static void Old(string message, string? tag = null) => Log(LogLevel.Old, message, tag);
    public static void Debug(string message, string? tag = null) => Log(LogLevel.Debug, message, tag);
    public static void Info(string message, string? tag = null) => Log(LogLevel.Info, message, tag);
    public static void Warn(string message, string? tag = null) => Log(LogLevel.Warn, message, tag);
    public static void Error(string message, string? tag = null) => Log(LogLevel.Error, message, tag);
    public static void Exception(Exception exception, string? message = "", string? tag = null) => Log(LogLevel.Error, message + exception, tag);
    public static void Fatal(string message, string? tag = null) => Log(LogLevel.Fatal, message, tag);

    public static void SendInGame(string message)
    {
        VentLogger.Debug($"Sending In Game: {message}");
        if (DestroyableSingleton<HudManager>.Instance) DestroyableSingleton<HudManager>.Instance.Notifier.AddItem(message);
    }

    public static void Log(LogLevel level, string message, string? tag = null)
    {
        if (level.Level < Configuration.AllowedLevel.Level) return;
        string levelPrefix = level.Name.PadRight(LogLevel.LongestName);
        string sourcePrefix = !Configuration.ShowSourceName ? "" : ":" + VentFramework.AssemblyNames.GetValueOrDefault(Assembly.GetCallingAssembly(), "Unknown");

        ConsoleManager.SetConsoleColor(level.Color);
        string tagPrefix = tag == null ? "" : $"[{tag}]";
        string fullMessage = $"[{levelPrefix}{sourcePrefix}] [{DateTime.Now:hh:mm:ss}]{tagPrefix} {message}";
        if (Configuration.Output is LogOutput.StandardOut)
            ConsoleManager.StandardOutStream?.WriteLine(fullMessage);
        else
            ConsoleManager.ConsoleStream?.WriteLine(fullMessage);
        ConsoleManager.SetConsoleColor(Configuration.DefaultColor);
    }

    public static class Configuration
    {
        public static LogLevel AllowedLevel { get; private set; } = LogLevel.All;
        public static ConsoleColor DefaultColor { get; private set; } = ConsoleColor.DarkGray;
        public static LogOutput Output { get; private set; } = LogOutput.ConsoleOut;
        public static bool ShowSourceName { get; private set; } = true;

        public static void SetLevel(LogLevel level)
        {
            AllowedLevel = level;
        }

        public static void SetDefaultColor(ConsoleColor color)
        {
            DefaultColor = color;
        }

        public static void SetOutput(LogOutput output)
        {
            Output = output;
        }

        public static void ShowSource(bool showSource)
        {
            ShowSourceName = showSource;
        }
    }
}