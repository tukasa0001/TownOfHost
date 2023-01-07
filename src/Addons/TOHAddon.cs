using System;
using System.Collections.Generic;
using System.Reflection;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Gamemodes;
using TownOfHost.Roles;

namespace TownOfHost.Addons;

public abstract class TOHAddon
{
    internal List<CustomRole> CustomRoles = new();
    internal List<Faction> Factions = new();
    internal List<IGamemode> Gamemodes = new();

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

    public void RegisterRole(CustomRole customRole) => CustomRoles.Add(customRole);

    public void RegisterGamemode(IGamemode gamemode) => Gamemodes.Add(gamemode);

    public void RegisterFaction(Faction faction) => Factions.Add(faction);

    public override string ToString() => GetName(true);
}

