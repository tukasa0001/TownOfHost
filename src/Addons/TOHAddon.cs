using System;
using System.Collections.Generic;
using System.Reflection;
using TOHTOR.Extensions;
using TOHTOR.Factions;
using TOHTOR.Roles;
using VentLib.Options.Announcement;

namespace TOHTOR.Addons;

public abstract class TOHAddon
{
    internal readonly List<CustomRole> CustomRoles = new();
    internal readonly List<Faction> Factions = new();
    internal readonly List<Type> Gamemodes = new();

    internal readonly Assembly BundledAssembly = Assembly.GetCallingAssembly();
    internal readonly ulong Uuid;

    public TOHAddon()
    {
        Uuid = (BundledAssembly.GetIdentity(false)?.SemiConsistentHash() ?? 0ul + AddonName().SemiConsistentHash());
    }

    internal string GetName(bool fullName = false) => !fullName
        ? AddonName()
        : $"{BundledAssembly.FullName}::{AddonName()}-{AddonVersion()}";

    public abstract void Initialize();

    public abstract string AddonName();

    public abstract string AddonVersion();

    public abstract List<AnnouncementOption> PluginOptions();

    public void RegisterRole(CustomRole customRole) => CustomRoles.Add(customRole);

    public void RegisterGamemode(Type gamemode) => Gamemodes.Add(gamemode);

    public void RegisterFaction(Faction faction) => Factions.Add(faction);

    public override string ToString() => GetName(true);
}

