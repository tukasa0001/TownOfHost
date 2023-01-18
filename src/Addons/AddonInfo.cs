using System;
using Hazel;
using VentLib.RPC.Interfaces;

namespace TownOfHost.Addons;

public class AddonInfo: IRpcSendable<AddonInfo>
{
    internal ulong UUID;
    internal string AssemblyShortName;
    internal string AssemblyFullName;
    internal string Name;
    internal string Version;
    internal Mismatch Mismatches = Mismatch.None;

    public AddonInfo Read(MessageReader reader)
    {
        return new AddonInfo
        {
            UUID = reader.ReadUInt64(),
            AssemblyShortName = reader.ReadString(),
            AssemblyFullName = reader.ReadString(),
            Name = reader.ReadString(),
            Version = reader.ReadString(),
            Mismatches = (Mismatch)reader.ReadInt32(),
        };
    }

    public void Write(MessageWriter writer)
    {
        writer.Write(UUID);
        writer.Write(AssemblyShortName);
        writer.Write(AssemblyFullName);
        writer.Write(Name);
        writer.Write(Version);
        writer.Write((int)Mismatches);
    }

    public static AddonInfo From(TOHAddon addon)
    {
        return new AddonInfo
        {
            UUID = addon.UUID,
            AssemblyShortName = addon.bundledAssembly.GetName().Name,
            AssemblyFullName = addon.bundledAssembly.GetName().FullName,
            Name = addon.AddonName(),
            Version = addon.AddonVersion()
        };
    }

    internal void CheckVersion(AddonInfo other)
    {
        if (other.Version != Version)
            Mismatches = (Mismatches | Mismatch.Version) & ~Mismatch.None;
    }

    public static bool operator ==(AddonInfo addon1, AddonInfo addon2) => addon1?.Equals(addon2) ?? addon2 is null;
    public static bool operator !=(AddonInfo addon1, AddonInfo addon2) => !addon1?.Equals(addon2) ?? addon2 is not null;

    public override bool Equals(object? obj)
    {
        if (obj is not AddonInfo addon) return false;
        return addon.UUID == UUID;
    }

    public override string ToString() => $"AddonInfo({Name}:{Version} (UUID: {UUID}))";

    public override int GetHashCode() => UUID.GetHashCode();
}

[Flags]
internal enum Mismatch
{
    None = 1,
    Version = 2,
    ClientMissingAddon = 4,
    HostMissingAddon = 8,
}