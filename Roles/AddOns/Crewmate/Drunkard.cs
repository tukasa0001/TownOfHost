//using System;
//using System.Collections.Generic;
//using UnityEngine;

//using TownOfHostForE.Roles.Core;
//using static TownOfHostForE.Options;

//using TownOfHostForE.Attributes;
//using TownOfHostForE.Roles.Crewmate;

//namespace TownOfHostForE.Roles.AddOns.Crewmate
//{
//    public static class Drunkard
//    {
//        private static readonly int Id = 85500;
//        public static Color RoleColor = Utils.GetRoleColor(CustomRoles.Drunkard);
//        public static List<byte> playerIdList = new();
//        private static OptionItem OptionDrunkardTasks;
//        public static int optionDrunkardTasks;
//        public static void SetupCustomOption()
//        {
//            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Drunkard);
//            OptionDrunkardTasks = IntegerOptionItem.Create(Id + 10, "DrunkardTasks", new(1, 10, 5), 1, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Workhorse])
//                .SetValueFormat(OptionFormat.Pieces);
//        }
//        [GameModuleInitializer]
//        public static void Init()
//        {
//            playerIdList = new();

//            optionDrunkardTasks = OptionDrunkardTasks.GetInt();
//        }
//        public static void Add(byte playerId)
//        {
//            playerIdList.Add(playerId);
//        }
//        public static bool IsEnable => playerIdList.Count > 0;
//        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

//        public static void OnCompleteTask(PlayerControl player)
//        {
//            if (!IsThisRole(player.PlayerId)) return;
//            CheckTask(player);
//        }

//        private static void CheckTask(PlayerControl pc)
//        {
//            var drunkId = pc.PlayerId;
//            var drunkTasks = pc.GetPlayerTaskState();

//            if (drunkTasks.RemainingTasksCount <= optionDrunkardTasks)
//            {
                
//            }
//        }
//    }
//}