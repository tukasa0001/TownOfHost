using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class AssassinAndMarine
    {
        public static string DisplayRole(bool disableColor = false)
        {
            return disableColor
            ? $"{string.Format(Utils.GetRoleName(CustomRoles.AssassinAndMarine), Utils.GetRoleName(CustomRoles.Assassin), Utils.GetRoleName(CustomRoles.Marine))}"
            : $"<color={Utils.GetRoleColorCode(CustomRoles.AssassinAndMarine)}>{string.Format(Utils.GetRoleName(CustomRoles.AssassinAndMarine), Assassin.ColorString, Marine.ColorString)}";
        }
        static readonly int Id = 40000;
        public static void SetupCustomOption()
        {
            Options.SetupAssassinAndMarineOptions(Id);
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
        public static string ColorString => Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Assassin), Utils.GetRoleName(CustomRoles.Assassin));
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

        public static void BootAssassinTrigger(PlayerControl assassin, bool BeKilled = false)
        {
            bool HeldMeeting = false;
            Assassin.TriggerPlayerId = assassin.PlayerId;
            Utils.NotifyRoles();
            Logger.Info("アサシン会議開始", "Special Phase");
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                Main.AllPlayerSpeed[pc.PlayerId] = 0.00001f;
                new LateTask(() =>
                {
                    if (AmongUsClient.Instance.AmHost && !HeldMeeting)
                    {
                        IsAssassinMeeting = true;
                        AssassinAndMarine.IsAssassinMeetingToggle();
                        MeetingRoomManager.Instance.AssignSelf(assassin, null);
                        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(assassin);
                        assassin.RpcStartMeeting(null);
                        Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.PlayerSpeedMod;
                        HeldMeeting = true;
                    }
                }, PlayerControl.GameOptions.MapId == 4 && BeKilled ? 3f : 0, "StartAssassinMeeting"); //Airshipなら3sの遅延を追加
            }
        }
        public static void SendTriggerPlayerId(byte playerId)
        {
            Assassin.TriggerPlayerId = playerId;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TriggerAssassinId, Hazel.SendOption.Reliable, -1);
            writer.Write(playerId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static class Marine
    {
        public static string ColorString => Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Marine), Utils.GetRoleName(CustomRoles.Marine));
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