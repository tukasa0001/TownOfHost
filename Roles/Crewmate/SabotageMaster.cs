using System.Collections.Generic;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Crewmate
{
    public static class SabotageMaster
    {
        private static readonly int Id = 20300;
        public static List<byte> playerIdList = new();

        public static OptionItem SkillLimit;
        public static OptionItem FixesDoors;
        public static OptionItem FixesReactors;
        public static OptionItem FixesOxygens;
        public static OptionItem FixesComms;
        public static OptionItem FixesElectrical;
        public static int UsedSkillCount;

        private static bool DoorsProgressing = false;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.SabotageMaster);
            SkillLimit = IntegerOptionItem.Create(Id + 10, "SabotageMasterSkillLimit", new(0, 99, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SabotageMaster])
                .SetValueFormat(OptionFormat.Times);
            FixesDoors = BooleanOptionItem.Create(Id + 11, "SabotageMasterFixesDoors", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesReactors = BooleanOptionItem.Create(Id + 12, "SabotageMasterFixesReactors", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesOxygens = BooleanOptionItem.Create(Id + 13, "SabotageMasterFixesOxygens", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesComms = BooleanOptionItem.Create(Id + 14, "SabotageMasterFixesCommunications", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesElectrical = BooleanOptionItem.Create(Id + 15, "SabotageMasterFixesElectrical", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
        }
        public static void Init()
        {
            playerIdList = new();
            UsedSkillCount = 0;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void RepairSystem(ShipStatus __instance, SystemTypes systemType, byte amount)
        {
            switch (systemType)
            {
                case SystemTypes.Reactor:
                    if (!FixesReactors.GetBool()) break;
                    if (SkillLimit.GetFloat() > 0 && UsedSkillCount >= SkillLimit.GetFloat()) break;
                    if (amount is 64 or 65)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 16);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 17);
                        UsedSkillCount++;
                    }
                    break;
                case SystemTypes.Laboratory:
                    if (!FixesReactors.GetBool()) break;
                    if (SkillLimit.GetFloat() > 0 && UsedSkillCount >= SkillLimit.GetFloat()) break;
                    if (amount is 64 or 65)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 67);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 66);
                        UsedSkillCount++;
                    }
                    break;
                case SystemTypes.LifeSupp:
                    if (!FixesOxygens.GetBool()) break;
                    if (SkillLimit.GetFloat() > 0 && UsedSkillCount >= SkillLimit.GetFloat()) break;
                    if (amount is 64 or 65)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 67);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 66);
                        UsedSkillCount++;
                    }
                    break;
                case SystemTypes.Comms:
                    if (!FixesComms.GetBool()) break;
                    if (SkillLimit.GetFloat() > 0 && UsedSkillCount >= SkillLimit.GetFloat()) break;
                    if (amount is 64 or 65)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 16);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 17);
                        UsedSkillCount++;
                    }
                    break;
                case SystemTypes.Doors:
                    if (!FixesDoors.GetBool()) break;
                    if (DoorsProgressing == true) break;

                    int mapId = Main.NormalOptions.MapId;
                    if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay) mapId = AmongUsClient.Instance.TutorialMapId;

                    DoorsProgressing = true;
                    if (mapId == 2)
                    {
                        //Polus
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 71, 72);
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 67, 68);
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 64, 66);
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 73, 74);
                    }
                    else if (mapId == 4)
                    {
                        //Airship
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 64, 67);
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 71, 73);
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 74, 75);
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 76, 78);
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 68, 70);
                        RepairSystemPatch.CheckAndOpenDoorsRange(__instance, amount, 83, 84);
                    }
                    DoorsProgressing = false;
                    break;
            }
        }
        public static void SwitchSystemRepair(SwitchSystem __instance, byte amount)
        {
            if (!FixesElectrical.GetBool()) return;
            if (SkillLimit.GetFloat() > 0 &&
                UsedSkillCount >= SkillLimit.GetFloat())
                return;

            if (amount is >= 0 and <= 4)
            {
                __instance.ActualSwitches = 0;
                __instance.ExpectedSwitches = 0;
                UsedSkillCount++;
            }
        }
    }
}