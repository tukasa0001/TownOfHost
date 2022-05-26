using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class AssassinAndMarine
    {
        static readonly int Id = 40000;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.AssassinAndMarine);
        }
        public static bool IsEnable()
        {
            return CustomRoles.AssassinAndMarine.IsEnable();
        }
        public static void Init()
        {
            Assassin.Init();
            Marine.Init();
        }
    }
    public static class Assassin
    {
        static List<byte> playerIdList = new();
        public static PlayerControl TriggerPlayer = null;
        public static bool IsAssassinMeeting;
        public static bool IsExileMarine;
        public static bool IsAssassinMeetingEnd;
        public static void Init()
        {
            playerIdList = new();
            IsAssassinMeeting = false;
            IsExileMarine = false;
            IsAssassinMeetingEnd = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }

        public static void BootAssassinTrigger(PlayerControl assassin)
        {
            MeetingRoomManager.Instance.AssignSelf(assassin, null);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(assassin);
            assassin.RpcStartMeeting(null);
            //assassin?.ReportDeadBody(null);
        }
    }
    public static class Marine
    {
        static List<byte> playerIdList = new();
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
    }
}