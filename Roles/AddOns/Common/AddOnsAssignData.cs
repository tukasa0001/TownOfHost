using System;
using System.Linq;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.AddOns.Common
{
    public class AddOnsAssignData
    {
        static Dictionary<CustomRoles, AddOnsAssignData> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        OptionItem CrewmateMaximum;
        OptionItem CrewmateFixedRole;
        OptionItem CrewmateAssignTarget;
        OptionItem ImpostorMaximum;
        OptionItem ImpostorFixedRole;
        OptionItem ImpostorAssignTarget;
        OptionItem NeutralMaximum;
        OptionItem NeutralFixedRole;
        OptionItem NeutralAssignTarget;
        static readonly CustomRoles[] InvalidRoles =
        {
            CustomRoles.GuardianAngel,
            CustomRoles.CSchrodingerCat,
            CustomRoles.SKMadmate,
            CustomRoles.MSchrodingerCat,
            CustomRoles.EgoSchrodingerCat,
            CustomRoles.JSchrodingerCat,
            CustomRoles.HASFox,
            CustomRoles.HASTroll,
            CustomRoles.GM,
        };
        static readonly IEnumerable<CustomRoles> ValidRoles = CustomRolesHelper.AllRoles.Where(role
            => role < CustomRoles.NotAssigned
            && !InvalidRoles.Contains(role)
            );
        static CustomRoles[] CrewmateRoles = ValidRoles.Where(role => role.IsCrewmate()).ToArray();
        static CustomRoles[] ImpostorRoles = ValidRoles.Where(role => role.IsImpostor()).ToArray();
        static CustomRoles[] NeutralRoles = ValidRoles.Where(role => role.IsNeutral()).ToArray();

        public AddOnsAssignData(int idStart, CustomRoles role, bool assignCrewmate, bool assignImpostor, bool assignNeutral)
        {
            this.IdStart = idStart;
            this.Role = role;
            if (assignCrewmate)
            {
                CrewmateMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players);
                CrewmateMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.CrewmateBlue, GetString("TeamCrewmate")) } };
                CrewmateFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(CrewmateMaximum);
                var crewmateStringArray = CrewmateRoles.Select(role => role.ToString()).ToArray();
                CrewmateAssignTarget = StringOptionItem.Create(idStart++, "Role", crewmateStringArray, 0, TabGroup.Addons, false)
                    .SetParent(CrewmateFixedRole);
            }

            if (assignImpostor)
            {
                ImpostorMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 3, 1), 3, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players);
                ImpostorMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.ImpostorRed, GetString("TeamImpostor")) } };
                ImpostorFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(ImpostorMaximum);
                var impostorStringArray = ImpostorRoles.Select(role => role.ToString()).ToArray();
                ImpostorAssignTarget = StringOptionItem.Create(idStart++, "Role", impostorStringArray, 0, TabGroup.Addons, false)
                    .SetParent(ImpostorFixedRole);
            }

            if (assignNeutral)
            {
                NeutralMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players);
                NeutralMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.AcceptedGreen, GetString("Neutral")) } };
                NeutralFixedRole = BooleanOptionItem.Create(idStart++, "FixedRole", false, TabGroup.Addons, false)
                    .SetParent(NeutralMaximum);
                var neutralStringsArray = NeutralRoles.Select(role => role.ToString()).ToArray();
                NeutralAssignTarget = StringOptionItem.Create(idStart++, "Role", neutralStringsArray, 0, TabGroup.Addons, false)
                    .SetParent(NeutralFixedRole);
            }

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするAddOnsAssignDataが作成されました", "AddOnsAssignData");
        }
        public static AddOnsAssignData Create(int idStart, CustomRoles role, bool assignCrewmate, bool assignImpostor, bool assignNeutral)
            => new(idStart, role, assignCrewmate, assignImpostor, assignNeutral);
        ///<summary>
        ///AddOnsAssignDataが存在する属性を一括で割り当て
        ///</summary>
        public static void AssignAddOnsFromList()
        {
            foreach (var kvp in AllData)
            {
                var (role, data) = kvp;
                if (!role.IsPresent()) continue;
                var assignTargetList = AssignTargetList(data);

                foreach (var pc in assignTargetList)
                {
                    PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(role);
                    Logger.Info("役職設定:" + pc?.Data?.PlayerName + " = " + pc.GetCustomRole().ToString() + " + " + role.ToString(), "AssignCustomSubRoles");
                }
            }
        }
        ///<summary>
        ///アサインするプレイヤーのList
        ///</summary>
        private static List<PlayerControl> AssignTargetList(AddOnsAssignData data)
        {
            var rnd = IRandom.Instance;
            var candidates = new List<PlayerControl>();
            var validPlayers = Main.AllPlayerControls.Where(pc => ValidRoles.Contains(pc.GetCustomRole()));

            if (data.CrewmateMaximum != null)
            {
                var crewmateMaximum = data.CrewmateMaximum.GetInt();
                if (crewmateMaximum > 0)
                {
                    var crewmates = validPlayers.Where(pc
                        => data.CrewmateFixedRole.GetBool() ? pc.Is(CrewmateRoles[data.CrewmateAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Crewmate)).ToList();
                    for (var i = 0; i < crewmateMaximum; i++)
                    {
                        if (crewmates.Count == 0) break;
                        var selectedCrewmate = crewmates[rnd.Next(crewmates.Count)];
                        candidates.Add(selectedCrewmate);
                        crewmates.Remove(selectedCrewmate);
                    }
                }
            }

            if (data.ImpostorMaximum != null)
            {
                var impostorMaximum = data.ImpostorMaximum.GetInt();
                if (impostorMaximum > 0)
                {
                    var impostors = validPlayers.Where(pc
                        => data.ImpostorFixedRole.GetBool() ? pc.Is(ImpostorRoles[data.ImpostorAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Impostor)).ToList();
                    for (var i = 0; i < impostorMaximum; i++)
                    {
                        if (impostors.Count == 0) break;
                        var selectedImpostor = impostors[rnd.Next(impostors.Count)];
                        candidates.Add(selectedImpostor);
                        impostors.Remove(selectedImpostor);
                    }
                }
            }

            if (data.NeutralMaximum != null)
            {
                var neutralMaximum = data.NeutralMaximum.GetInt();
                if (neutralMaximum > 0)
                {
                    var neutrals = validPlayers.Where(pc
                        => data.NeutralFixedRole.GetBool() ? pc.Is(NeutralRoles[data.NeutralAssignTarget.GetValue()]) : pc.Is(CustomRoleTypes.Neutral)).ToList();
                    for (var i = 0; i < neutralMaximum; i++)
                    {
                        if (neutrals.Count == 0) break;
                        var selectedNeutral = neutrals[rnd.Next(neutrals.Count)];
                        candidates.Add(selectedNeutral);
                        neutrals.Remove(selectedNeutral);
                    }
                }
            }

            while (candidates.Count > data.Role.GetRealCount())
                candidates.RemoveAt(rnd.Next(candidates.Count));

            return candidates;
        }
    }
}