using System;
using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Roles;
using VentLib.Utilities.Extensions;
using VentLib.Logging;

namespace TownOfHost.Gamemodes.Standard;

public static class RoleAssignments
{
    public static List<CustomRole> RolesForGame(List<CustomRole> enabledRoles, int minRoles, int maxRoles)
    {
        List<CustomRole> gameRoles = new();
        enabledRoles.StrJoin().DebugLog("Enabled Roles: ");
        List<CustomRole> distributionList = GetDistributionList(enabledRoles);
        distributionList.StrJoin().DebugLog("Distribution List: ");
        Random random = new();
        while (distributionList.Count > 0 && gameRoles.Count < maxRoles)
        {
            var subList = distributionList.GetRange(0, 10 < distributionList.Count ? 10 : distributionList.Count);
            int randomIndex = random.Next(gameRoles.Count < minRoles ? subList.Count : 10);

            // Optional amount of role
            if (randomIndex >= subList.Count)
                return gameRoles;

            gameRoles.Add(subList[randomIndex]);
            distributionList = distributionList.GetRange(subList.Count, distributionList.Count - subList.Count);
        }

        return gameRoles;
    }

    public static List<CustomRole> DebugRoles(PlayerControl host)
    {
        List<CustomRole> debugRoles = new();
        string name = host.GetRawName().ToLower();

        name.DebugLog("REAL NAME: ");
        CustomRole matchingRole =
            CustomRoleManager.AllRoles.FirstOrDefault(role =>
            {
                string roleString = role.ToString().ToLower();
                roleString = roleString.Length >= 5 ? roleString[..5] : roleString[..roleString.Length];
                roleString.DebugLog();
                return name.StartsWith(roleString);
            }, CustomRoleManager.Default);

        if (matchingRole is Crewmate)
            return debugRoles;

        int amount = !name.Contains("X") ? 1 : int.Parse(name.Split("X")[1]);
        VentLogger.Old($"Creating #{amount} of {matchingRole}", "Debug");

        for (int i = 0; i < amount * 10; i++)
            debugRoles.Add(matchingRole);

        return debugRoles;
    }

    public static List<CustomRole> EnabledRoles(IEnumerable<CustomRole> roles)
    {
        /*foreach (CustomRoles customRoles in roles)
            VentLogger.Old($"{customRoles} enabled = {customRoles.IsEnable()}", "DebugRoles");*/

        return roles.Where(role => role.IsEnabled()).ToList();
    }

    private static List<CustomRole> GetDistributionList(IReadOnlyCollection<CustomRole> enabledRoles)
    {
        List<CustomRole> distribution = new();
        List<CustomRole> guaranteedRoles = enabledRoles.Where(role => role.Chance >= 100).ToList();
        List<CustomRole> nonGuaranteedRoles = enabledRoles.Where(role => role.Chance < 100).ToList();
        while (guaranteedRoles.Count > 0)
        {
            int randomRoleIndex = UnityEngine.Random.RandomRangeInt(0, guaranteedRoles.Count);
            AddRoleBasedOnChanceToList(distribution, guaranteedRoles[randomRoleIndex]);
            guaranteedRoles.RemoveAt(randomRoleIndex);
        }

        while (nonGuaranteedRoles.Count > 0)
        {
            int randomRoleIndex = UnityEngine.Random.RandomRangeInt(0, nonGuaranteedRoles.Count);
            AddRoleBasedOnChanceToList(distribution, nonGuaranteedRoles[randomRoleIndex]);
            nonGuaranteedRoles.RemoveAt(randomRoleIndex);
        }

        return distribution;
    }

    private static void AddRoleBasedOnChanceToList(ICollection<CustomRole> list, CustomRole role)
    {
        for (int i = 0; i < role.Chance / 10; i++) list.Add(role);
    }
}