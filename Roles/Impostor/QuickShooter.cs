using Hazel;
using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHE.Roles.Crewmate;
using UnityEngine;

namespace TOHE.Roles.Impostor
{
    internal static class QuickShooter
    {
        private static readonly int Id = 902522;
        public static List<byte> playerIdList = new();
        private static OptionItem KillCooldown;
        private static OptionItem MeetReserved;
        public static Dictionary<byte, float> ShotLimit = new();
        
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.QuickShooter);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 990f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
                .SetValueFormat(OptionFormat.Seconds);
            MeetReserved = FloatOptionItem.Create(Id + 10, "MeetReserved", new(0, 100, 1), 1, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
                .SetValueFormat(OptionFormat.Times);
        }
        public static void Init()
        {
            playerIdList = new();
            ShotLimit = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
            ShotLimit.TryAdd(playerId, 0);
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "Sheriff");
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSheriffShotLimit, SendOption.Reliable, -1);
            writer.Write(playerId);
            writer.Write(ShotLimit[playerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte SheriffId = reader.ReadByte();
            float Limit = reader.ReadSingle();
            if (ShotLimit.ContainsKey(SheriffId))
                ShotLimit[SheriffId] = Limit;
            else
                ShotLimit.Add(SheriffId, 0);
        }
        public static void KillStorage(PlayerControl pc, bool shapeshifting) 
        {
            if (pc.killTimer == 0 && shapeshifting) 
                ShotLimit[pc.PlayerId]++;
        }
        public static void SetKillCooldown(byte id) 
        {
            if(ShotLimit[id] > 0) 
            {
                Main.AllPlayerKillCooldown[id] = 0f;
            }
            else 
            {
                Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
            }
        }
        public static void MeetCleanLimit(PlayerControl pc) 
        {
            if (ShotLimit[pc.PlayerId] > MeetReserved.GetFloat()) 
            {
                ShotLimit = new();
                ShotLimit[pc.PlayerId] = MeetReserved.GetFloat();
            }
        }
        public static void QuickShooterKill(PlayerControl killer)
        {
            if (ShotLimit.ContainsKey(killer.PlayerId))
                ShotLimit[killer.PlayerId]--;
            else
                ShotLimit.TryAdd(killer.PlayerId, 0);
        }
        public static string GetShotLimit(byte playerId) => Utils.ColorString(ShotLimit[playerId]>0 ? Utils.GetRoleColor(CustomRoles.QuickShooter) : Color.gray, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
    }
}
