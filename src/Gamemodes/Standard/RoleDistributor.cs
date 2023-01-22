using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Roles;
using VentLib.Extensions;

namespace TownOfHost.Gamemodes.Standard;

public static class RoleDistributor
{
    public static void DistributeImpostors(Queue<CustomRole> roles, int impostors)
    {
        List<CustomRole> enabledImpostorRoles = CustomRoleManager.AllRoles.Where(r => r.IsEnabled() && r.Factions.IsImpostor()).ToList();
        List<CustomRole> eir2 = enabledImpostorRoles.ToList();
        eir2.Do(r => {
            if (r.AdditionalChance != 100) return;
            for (int i = 0; i < (eir2.Count - 1); i++) enabledImpostorRoles.Add(r);
        });

        List<CustomRole> oneHundredPercentRoles = enabledImpostorRoles.Where(r => r.Chance == 100).ToList();
        List<CustomRole> lotteryRoles = enabledImpostorRoles.Except(oneHundredPercentRoles).ToList();

        while (oneHundredPercentRoles.Count > 0 && roles.Count < impostors)
            roles.Enqueue(oneHundredPercentRoles.PopRandom());

        if (roles.Count >= impostors) return;

        DoLottery(lotteryRoles, roles, impostors);
        while (roles.Count < impostors) roles.Enqueue(CustomRoleManager.Static.Impostor);
    }

    public static void DistributeNonImpostors(Queue<CustomRole> roles, int players)
    {
        List<CustomRole> enabledOtherRoles = CustomRoleManager.AllRoles.Where(r => r.IsEnabled() && !r.Factions.IsImpostor()).ToList();
        List<CustomRole> eor2 = enabledOtherRoles.ToList();
        eor2.Do(r => {
            if (r.AdditionalChance != 100) return;
            for (int i = 0; i < (eor2.Count - 1); i++) enabledOtherRoles.Add(r);
        });

        List<CustomRole> oneHundredPercentRoles = enabledOtherRoles.Where(r => r.Chance == 100).ToList();
        List<CustomRole> lotteryRoles = enabledOtherRoles.Except(oneHundredPercentRoles).ToList();

        while (oneHundredPercentRoles.Count > 0 && roles.Count < players)
            roles.Enqueue(oneHundredPercentRoles.PopRandom());

        DoLottery(lotteryRoles, roles, players);
    }

    private static void DoLottery(List<CustomRole> lotteryRoles, Queue<CustomRole> roles, int maximum)
    {
        Dictionary<int, CustomRole> roleLottery = new();
        List<int> tickets = new();
        lotteryRoles.Do(role => {
            int ticket1 = roleLottery.Count;
            roleLottery.Add(ticket1, role);
            for (int i = 0; i < 100; i += 10)
                if (i < role.Chance) tickets.Add(ticket1);
                else tickets.Add(-1 * ticket1);

            for (int i = 0; i < (role.Count - 1); i++) {
                int ticket2 = roleLottery.Count;
                roleLottery.Add(ticket2, role);
                for (int j = 0; j < 100; j += 10)
                    if (j < role.AdditionalChance) tickets.Add(ticket2);
                    else tickets.Add(-1 * ticket2);
            }
        });
        tickets.Shuffle();

        int roleCount = roles.Count;
        while (roleCount < maximum && tickets.Count > 0)
        {
            int ticket = tickets.PopRandom();
            tickets.RemoveAll(t => t == ticket);
            if (ticket < 0) {
                roleCount++;
                continue;
            }
            roles.Enqueue(roleLottery[ticket]);
            roleCount++;
        }
    }
}