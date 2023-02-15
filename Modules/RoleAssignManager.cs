using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

namespace TownOfHost.Modules
{
    public static class RoleAssignManager
    {
        private static readonly int idStart = 500;
        private static Dictionary<RoleType, int> AssignCount;
        private static List<CustomRoles> AssignRoleList;
        private static OptionItem ImpostorMin;
        private static OptionItem ImpostorMax;
        private static OptionItem MadmateMin;
        private static OptionItem MadmateMax;
        private static OptionItem CrewmateMin;
        private static OptionItem CrewmateMax;
        private static OptionItem NeutralMin;
        private static OptionItem NeutralMax;

        private static CustomRoles[] RolesArray = Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().ToArray();
        private static RoleType[] RoleTypeArray = Enum.GetValues(typeof(RoleType)).Cast<RoleType>().ToArray();

        public static void SetupCustomOption()
        {
            ImpostorMin = IntegerOptionItem.Create(idStart, "ImpostorRolesMin", new(0, 3, 1), 3, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Players);
            ImpostorMax = IntegerOptionItem.Create(idStart + 1, "ImpostorRolesMax", new(0, 3, 1), 3, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Players);
            MadmateMin = IntegerOptionItem.Create(idStart + 6, "MadRolesMin", new(0, 15, 1), 15, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Players);
            MadmateMax = IntegerOptionItem.Create(idStart + 7, "MadRolesMax", new(0, 15, 1), 15, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Players);

            CrewmateMin = IntegerOptionItem.Create(idStart + 2, "CrewmateRolesMin", new(0, 15, 1), 15, TabGroup.CrewmateRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Players);
            CrewmateMax = IntegerOptionItem.Create(idStart + 3, "CrewmateRolesMax", new(0, 15, 1), 15, TabGroup.CrewmateRoles, false)
                .SetValueFormat(OptionFormat.Players);

            NeutralMin = IntegerOptionItem.Create(idStart + 4, "NeutralRolesMin", new(0, 15, 1), 15, TabGroup.NeutralRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Players);
            NeutralMax = IntegerOptionItem.Create(idStart + 5, "NeutralRolesMax", new(0, 15, 1), 15, TabGroup.NeutralRoles, false)
                .SetValueFormat(OptionFormat.Players);
        }
    }
}