using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class AssasinAndMarlin
    {
        static readonly int Id = 40000;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.AssasinAndMarlin);
        }
        public static bool IsEnable()
        {
            return CustomRoles.AssasinAndMarlin.IsEnable();
        }
        public static void Init()
        {
            Assasin.Init();
            Marlin.Init();
        }
    }
    public static class Assasin
    {
        static List<byte> playerIdList = new();
        public static PlayerControl TriggerPlayer = null;
        public static bool IsAssasinMeeting = false;
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

        public static void BootAssasinTrigger(PlayerControl assasin)
        {
            assasin?.RpcStartMeeting(null);
        }
    }
    public static class Marlin
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