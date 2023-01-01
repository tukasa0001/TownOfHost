using System;
using System.Collections.Generic;
using System.Data;
using HarmonyLib;

namespace TownOfHost.Factions;

// Used to validate that all factions have unique long IDs
public class FactionConstraintValidator
{
    public static Dictionary<ulong, string> uniqueFactionMap = new();

    static FactionConstraintValidator()
    {
        Enum.GetValues<Faction>().Do(f => uniqueFactionMap.Add((ulong)f, "BaseMod"));
    }

    public static void ValidateAndAdd(Faction faction, string addonName)
    {
        if (uniqueFactionMap.TryGetValue((ulong)faction, out string ownerName))
            throw new ConstraintException($"Faction ID: {(ulong)faction} has already been registered by \"{ownerName}\". All factions must have unique IDs. Please choose a random number between 0 - 4,294,967,295 for your faction. If you are not the developer of this Addon. Please contact the developer about this issue.");

        uniqueFactionMap.Add((ulong) faction, addonName);
    }

}