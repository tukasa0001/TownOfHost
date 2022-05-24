using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class AssasinAndMarine
    {
        static readonly int Id = 40000;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.AssasinAndMarine);
        }
        public static bool IsEnable()
        {
            return CustomRoles.AssasinAndMarine.IsEnable();
        }
        public static void Init()
        {
            Assasin.Init();
            Marine.Init();
        }
    }
    public static class Assasin
    {
        static List<byte> playerIdList = new();
        public static PlayerControl TriggerPlayer = null;
        public static bool IsAssasinMeeting;
        public static void Init()
        {
            playerIdList = new();
            IsAssasinMeeting = false;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }

        public static void BootAssasinTrigger(PlayerControl assasin)
        {
            assasin?.ReportDeadBody(null);
            MeetingHud.Instance.Start();
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