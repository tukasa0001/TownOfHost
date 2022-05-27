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
        public static void IsAssassinMeetingToggle()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.IsAssassinMeeting, Hazel.SendOption.Reliable, -1);
            writer.Write(Assassin.IsAssassinMeeting);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void MarineSelectedInAssassinMeeting()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MarineSelectedInAssassinMeeting, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void GameEndForAssassinMeeting()
        {
            Main.currentWinner = CustomWinner.Impostor;
            //new LateTask(() =>
            Main.CustomWinTrigger = true;//,
            //    0.2f, "Custom Win Trigger Task");
        }
    }
    public static class Assassin
    {
        static List<byte> playerIdList = new();
        public static byte TriggerPlayerId;
        public static bool IsAssassinMeeting;
        public static bool FinishAssassinMeetingTrigger;
        public static byte AssassinTargetId;
        public static CustomRoles TargetRole = CustomRoles.Crewmate;
        public static void Init()
        {
            playerIdList = new();
            IsAssassinMeeting = false;
            FinishAssassinMeetingTrigger = false;
            TriggerPlayerId = 0x73;
            AssassinTargetId = 0x74;
            TargetRole = CustomRoles.Crewmate;
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
            Assassin.TriggerPlayerId = assassin.PlayerId;
            Utils.NotifyRoles();
            new LateTask(() =>
            {
                IsAssassinMeeting = true;
                AssassinAndMarine.IsAssassinMeetingToggle();
                MeetingRoomManager.Instance.AssignSelf(assassin, null);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(assassin);
                assassin.RpcStartMeeting(null);
            }, 0.5f, "StartAssassinMeeting");
            Logger.Info("アサシン会議開始", "Special Phase");
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