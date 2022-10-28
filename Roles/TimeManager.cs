using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class TimeManager
    {
        static readonly int Id = 100000;
        static List<byte> playerIdList = new();
        public static Dictionary<byte, int> TimeManagerKillCount = new();
        public static CustomOption IncreaseMeetingTime;
        public static CustomOption MeetingTimeLimit;
        public static void SetupCustomOption()
        {
            IncreaseMeetingTime = CustomOption.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "TimeManagerIncreaseMeetingTime", 20, 0, 100, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeManager]);
            MeetingTimeLimit = CustomOption.Create(Id + 11, TabGroup.CrewmateRoles, Color.white, "TimeManagerLimitMeetingTime", 400, 150, 600, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeManager]);
        }
        public static void Init()
        {
            TimeManagerKillCount = new();
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TimeManagerKillCount[playerId] = 0;
            Utils.GetPlayerById(playerId)?.RpcSetKillCount();
        }
        public static bool IsEnable() => playerIdList.Count > 0;
    }
}