using System.Collections.Generic;
using System.Linq;

namespace TownOfHost.Factions;

public enum Faction: ulong
{
    Crewmates = 0,
    Solo = 1,
    Impostors = 2,
    Coven = 3
}

public static class FactionMethods
{
    public static bool IsAllied(this Faction faction, Faction[] factions)
    {
        if (faction is Faction.Solo) return false;
        return !factions.Contains(Faction.Solo) && factions.Contains(faction);
    }

    public static bool IsAllied(this Faction faction, Faction otherFaction)
    {
        if (faction is Faction.Solo || otherFaction is Faction.Solo) return false;
        return faction == otherFaction;
    }

    public static bool IsAllied(this IEnumerable<Faction> factions, Faction other)
    {
        IEnumerable<Faction> enumerable = factions as Faction[] ?? factions.ToArray();
        return other != Faction.Solo && !enumerable.Contains(Faction.Solo) && enumerable.Contains(other);
    }

    public static bool IsAllied(this IEnumerable<Faction> factions, IEnumerable<Faction> others)
    {
        IEnumerable<Faction> enumerableThis = factions as Faction[] ?? factions.ToArray();
        IEnumerable<Faction> enumerableOthers = factions as Faction[] ?? others.ToArray();
        return !enumerableThis.Contains(Faction.Solo) && !enumerableOthers.Contains(Faction.Solo) && (enumerableThis.Any(f => enumerableOthers.Contains(f) || enumerableOthers.Any(f => enumerableThis.Contains(f))));
    }

    public static bool IsSolo(this IEnumerable<Faction> factions) => factions.Contains(Faction.Solo);
    public static bool IsImpostor(this IEnumerable<Faction> factions) => factions.Contains(Faction.Impostors);
    public static bool IsCrewmate(this IEnumerable<Faction> factions) => factions.Contains(Faction.Crewmates);
}