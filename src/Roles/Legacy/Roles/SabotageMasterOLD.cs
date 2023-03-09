/*using System.Collections.Generic;
using System.Linq;
using Hazel;
using TOHTOR.Extensions;
using UnityEngine;

namespace TOHTOR
{
    public static class SabotageMasterOLD
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
        public static int HackerUsedSkillCount;

        private static bool DoorsProgressing = false;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.SabotageMaster, AmongUsExtensions.OptionType.Crewmate);
            /*SkillLimit = CustomOption.Create(Id + 10, Color.white, "SabotageMasterSkillLimit", AmongUsExtensions.OptionType.Crewmate, 1, 0, 99, 1, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesDoors = CustomOption.Create(Id + 11, Color.white, "SabotageMasterFixesDoors", AmongUsExtensions.OptionType.Crewmate, false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesReactors = CustomOption.Create(Id + 12, Color.white, "SabotageMasterFixesReactors", AmongUsExtensions.OptionType.Crewmate, false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesOxygens = CustomOption.Create(Id + 13, Color.white, "SabotageMasterFixesOxygens", AmongUsExtensions.OptionType.Crewmate, false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesComms = CustomOption.Create(Id + 14, Color.white, "SabotageMasterFixesCommunications", AmongUsExtensions.OptionType.Crewmate, false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            FixesElectrical = CustomOption.Create(Id + 15, Color.white, "SabotageMasterFixesElectrical", AmongUsExtensions.OptionType.Crewmate, false, Options.CustomRoleSpawnChances[CustomRoles.SabotageMaster]);#1#
        }
        public static void Init()
        {
            playerIdList = new();
            UsedSkillCount = 0;
            HackerUsedSkillCount = 0;
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

                    int mapId = GameOptionsManager.Instance.CurrentGameOptions.MapId;
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
        public static void HackerRepairSystem(ShipStatus __instance, SystemTypes systemType, byte amount)
        {
            switch (systemType)
            {
                case SystemTypes.Reactor:
                    if (amount is 64 or 65)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 67);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 66);
                        HackerUsedSkillCount++;
                    }
                    if (amount is 16 or 17)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 19);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 18);
                        HackerUsedSkillCount++;
                    }
                    break;
                case SystemTypes.Laboratory:
                    if (amount is 64 or 65)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 67);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 66);
                        HackerUsedSkillCount++;
                    }
                    break;
                case SystemTypes.LifeSupp:
                    if (amount is 64 or 65)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 67);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 66);
                        HackerUsedSkillCount++;
                    }
                    break;
                case SystemTypes.Comms:
                    if (amount is 16 or 17)
                    {
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 19);
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 18);
                    }
                    HackerUsedSkillCount++;
                    break;
                case SystemTypes.Doors:
                    /*if (!FixesDoors.GetBool()) break;
                    if (DoorsProgressing == true) break;

                    int mapId = GameOptionsManager.Instance.CurrentGameOptions.MapId;
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
                    DoorsProgressing = false;#1#
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
        public static void HackerSwitchSystemRepair(SwitchSystem __instance, byte amount)
        {
            /*if (SkillLimit.GetFloat() > 0 &&
                HackerUsedSkillCount >= SkillLimit.GetFloat())
                return;#1#
            //HackerUsedSkillCount++;

            if (amount is >= 0 and <= 4)
            {
                __instance.ActualSwitches = 0;
                __instance.ExpectedSwitches = 0;
                HackerUsedSkillCount++;
            }
        }
    }
}*/