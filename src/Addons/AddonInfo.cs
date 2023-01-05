using System;
using Hazel;
using VentFramework;

namespace TownOfHost.Addons;

public class AddonInfo: RpcSendable<AddonInfo>
{
    internal ulong UUID;
    internal string AssemblyName;
    internal string Name;
    internal string Version;
    internal Mismatch Mismatches = Mismatch.None;

    public override AddonInfo Read(MessageReader reader)
    {
        return new AddonInfo
        {
            UUID = reader.ReadUInt64(),
            AssemblyName = reader.ReadString(),
            Name = reader.ReadString(),
            Version = reader.ReadString(),
            Mismatches = (Mismatch)reader.ReadInt32(),
        };
    }

    public override void Write(MessageWriter writer)
    {
        writer.Write(UUID);
        writer.Write(AssemblyName);
        writer.Write(Name);
        writer.Write(Version);
        writer.Write((int)Mismatches);
    }

    public static AddonInfo From(TOHAddon addon)
    {
        return new AddonInfo
        {
            UUID = addon.UUID,
            AssemblyName = addon.bundledAssembly.GetName().Name,
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

    public override bool Equals(object obj)
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