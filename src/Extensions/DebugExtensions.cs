namespace TownOfHost.Extensions;

public static class DebugExtensions
{
    public static void DebugLog(this object obj, string prefixText = "", string tag = "DebugLog")
    {
        Logger.Info($"{prefixText}{obj}", tag);
    }
}