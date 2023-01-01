using System;
using System.Collections.Generic;
using TownOfHost.Factions;

namespace TownOfHost.Addons;

public abstract class TOHAddon
{
    internal List<Type> customRoles = new();
    internal List<Faction> factions = new();

    public abstract void Initialize();

    public void RegisterRole(Type roleType) => customRoles.Add(roleType);

    public void RegisterFaction(Faction faction) => factions.Add(faction);
}


