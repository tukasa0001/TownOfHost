using System;
using System.Collections.Generic;
using System.Reflection;
using TownOfHost.Extensions;
using TownOfHost.Factions;

namespace TownOfHost.Addons;

public abstract class TOHAddon
{
    internal List<Type> customRoles = new();
    internal List<Faction> factions = new();
    internal Assembly bundledAssembly = Assembly.GetCallingAssembly();
    internal ulong UUID;

    public TOHAddon()
    {
        UUID = (bundledAssembly?.GetIdentity(false)?.SemiConsistentHash() ?? 0ul + AddonName().SemiConsistentHash());
    }

    internal string GetName(bool fullName = false) => !fullName
        ? AddonName()
        : $"{bundledAssembly.FullName}::{AddonName()}-{AddonVersion()}";

    public abstract void Initialize();

    public abstract string AddonName();

    public abstract string AddonVersion();

    public void RegisterRole(Type roleType) => customRoles.Add(roleType);

    public void RegisterFaction(Faction faction) => factions.Add(faction);

    public override string ToString() => GetName(true);
}

