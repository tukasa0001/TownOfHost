using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hazel;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    public static class AntiBlackout
    {
        private static Dictionary<byte, bool> isDeadCache = new();

        public static void SetIsDead(bool doSend = true)
        {
            isDeadCache.Clear();
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                isDeadCache[info.PlayerId] = info.IsDead;
                info.IsDead = false;
            }
            if (doSend) SendGameData();
        }
        public static void RestoreIsDead(bool doSend = true)
        {
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                if (isDeadCache.TryGetValue(info.PlayerId, out bool val)) info.IsDead = val;
            }
            isDeadCache.Clear();
            if (doSend) SendGameData();
        }

        public static void SendGameData()
        {
            MessageWriter writer = AmongUsClient.Instance.Streams[(int)SendOption.Reliable];
            writer.StartMessage(1);
            writer.WritePacked(GameData.Instance.NetId);
            GameData.Instance.Serialize(writer, true);
            writer.EndMessage();
        }
    }
}