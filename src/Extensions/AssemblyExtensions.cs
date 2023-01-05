using System.Reflection;

namespace TownOfHost.Extensions;

public static class AssemblyExtensions
{
    public static string GetIdentity(this Assembly assembly, bool includeVersion = true)
    {
        AssemblyName name = assembly.GetName();
        string versionText = includeVersion ? $"Version={name.Version}, " : "";
        return $"{name.Name}, {versionText}PublicKey={name.GetPublicKey()}";
    }
}