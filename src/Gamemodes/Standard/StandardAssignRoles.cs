using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;

namespace TownOfHost.Gamemodes.Standard;

class StandardAssignRoles
{
    internal static void StandardAssign(List<PlayerControl> unassignedPlayers)
    {
        int impostors = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;

        List<CustomRole> impostorRoles =
            RoleAssignments.RolesForGame(RoleAssignments.EnabledRoles(CustomRoleManager.Roles.Where(r => r.Factions.IsImpostor())), 0, impostors);


        while (impostorRoles.Count < impostors)
            impostorRoles.Add(CustomRoleManager.Static.Impostor);

        List<CustomRole> neutralKillingRoles =
            RoleAssignments.RolesForGame(RoleAssignments.EnabledRoles(CustomRoleManager.Roles.Where(r => r.SpecialType is SpecialType.NeutralKilling)), StaticOptions.MinNK, StaticOptions.MaxNK);

        List<CustomRole> neutralPassiveRoles =
            RoleAssignments.RolesForGame(RoleAssignments.EnabledRoles(CustomRoleManager.Roles.Where(r => r.SpecialType is SpecialType.Neutral)), StaticOptions.MinNonNK, StaticOptions.MaxNK);

        List<CustomRole> crewMateRoles =
            RoleAssignments.RolesForGame(RoleAssignments.EnabledRoles(CustomRoleManager.Roles.Where(r => r.Factions.IsCrewmate())), 0, ModConstants.MaxPlayers);

        List<CustomRole> joinedRoleSelection = new(impostorRoles);
        joinedRoleSelection.AddRange(neutralKillingRoles);
        joinedRoleSelection.AddRange(neutralPassiveRoles);
        joinedRoleSelection.AddRange(crewMateRoles);

        joinedRoleSelection.PrettyString().DebugLog("Remaining Roles: ");
        List<Tuple<PlayerControl, CustomRole>> assignments = new();

        int i = 0;
        while (i < unassignedPlayers.Count)
        {
            PlayerControl player = unassignedPlayers[i];
            CustomRole role = CustomRoleManager.Roles.FirstOrDefault(r => r.RoleName.RemoveHtmlTags().ToLower().StartsWith(player.GetRawName()?.ToLower() ?? "HEHXD"));
            if (role != null && role.GetType() != typeof(Crewmate))
            {
                role = CustomRoleManager.PlayersCustomRolesRedux[player.PlayerId] = role.Instantiate(player);
                role.SyncOptions();
                assignments.Add(new Tuple<PlayerControl, CustomRole>(player, role));
                unassignedPlayers.Pop(i);
            }
            else i++;
        }


        while (unassignedPlayers.Count > 0 && joinedRoleSelection.Count > 0)
        {
            PlayerControl assignedPlayer = unassignedPlayers.PopRandom();
            CustomRole role = joinedRoleSelection.Pop(0);
            //CustomRoleManager.PlayersCustomRolesRedux[assignedPlayer.PlayerId] = role;

            // We have to initialize the role past its "static" phase
            role = CustomRoleManager.PlayersCustomRolesRedux[assignedPlayer.PlayerId] = role.Instantiate(assignedPlayer);
            role.SyncOptions();
            assignments.Add(new Tuple<PlayerControl, CustomRole>(assignedPlayer, role));

        }

        while (unassignedPlayers.Count > 0)
        {
            PlayerControl unassigned = unassignedPlayers.Pop(0);
            CustomRole role = CustomRoleManager.PlayersCustomRolesRedux[unassigned.PlayerId] = CustomRoleManager.Static.Crewmate.Instantiate(unassigned);
            role.SyncOptions();
            assignments.Add(new System.Tuple<PlayerControl, CustomRole>(unassigned, role));
        }

        List<Subrole> subroles = CustomRoleManager.Roles.OfType<Subrole>().ToList();
        while (subroles.Count > 0)
        {
            Subrole subrole = subroles.PopRandom();
            bool hasSubrole = subrole.Chance > UnityEngine.Random.RandomRange(0, 100);
            if (!hasSubrole) continue;
            List<PlayerControl> victims = Game.GetAllPlayers().Where(p => p.GetSubrole() == null).ToList();
            if (victims.Count == 0) break;
            PlayerControl victim = victims.GetRandom();
            CustomRoleManager.AddPlayerSubrole(victim.PlayerId, (Subrole)subrole.Instantiate(victim));
        }

        Game.GetAllPlayers().Do(p => p.GetCustomRole().Assign());
    }
}