using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class SabotageMaster
    {
        private static readonly int Id = 20300;
        public static List<byte> playerIdList = new();

        public static CustomOption SkillLimit;
        public static CustomOption FixesDoors;
        public static CustomOption FixesReactors;
        public static CustomOption FixesOxygens;
        public static CustomOption FixesComms;
        public static CustomOption FixesElectrical;
        public static int UsedSkillCount;

        private static bool DoorsProgressing = false;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.SabotageMaster);
            SkillLimit = CustomOption.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "SabotageMasterSkillLimit", 1, 0, 99, 1, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesDoors = CustomOption.Create(Id + 11, TabGroup.CrewmateRoles, Color.white, "SabotageMasterFixesDoors", false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesReactors = CustomOption.Create(Id + 12, TabGroup.CrewmateRoles, Color.white, "SabotageMasterFixesReactors", false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesOxygens = CustomOption.Create(Id + 13, TabGroup.CrewmateRoles, Color.white, "SabotageMasterFixesOxygens", false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesComms = CustomOption.Create(Id + 14, TabGroup.CrewmateRoles, Color.white, "SabotageMasterFixesCommunications", false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesElectrical = CustomOption.Create(Id + 15, TabGroup.CrewmateRoles, Color.white, "SabotageMasterFixesElectrical", false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
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
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 67);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 66);
                        UsedSkillCount++;
                    }
                    if (amount is 16 or 17)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 19);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 18);
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
                    if (amount is 16 or 17)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 19);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 18);
                    }
                    UsedSkillCount++;
                    break;
                case SystemTypes.Doors:
                    if (!FixesDoors.GetBool()) break;
                    if (DoorsProgressing == true) break;

                    int mapId = PlayerControl.GameOptions.MapId;
                    if (AmongUsClient.Instance.GameMode == GameModes.FreePlay) mapId = AmongUsClient.Instance.TutorialMapId;

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