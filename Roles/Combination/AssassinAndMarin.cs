using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class AssassinAndMarin
    {
        public static string DisplayRole(bool disableColor = false)
        {
            return disableColor
            ? $"{string.Format(Utils.GetRoleName(CustomRoles.AssassinAndMarin), Utils.GetRoleName(CustomRoles.Assassin), Utils.GetRoleName(CustomRoles.Marin))}"
            : $"<color={Utils.GetRoleColorCode(CustomRoles.AssassinAndMarin)}>{string.Format(Utils.GetRoleName(CustomRoles.AssassinAndMarin), Assassin.ColorString, Marin.ColorString)}";
        }
        static readonly int Id = 40000;
        public static void SetupCustomOption()
        {
            Options.SetupAssassinAndMarinOptions(Id);
            Assassin.HasWatcherAbility = CustomOption.Create(Id + 10, Color.white, "AssassinHasWatcherAbility", false, Options.CustomRoleSpawnChances[CustomRoles.AssassinAndMarin]);
            Marin.HasWatcherAbility = CustomOption.Create(Id + 11, Color.white, "MarinHasWatcherAbility", false, Options.CustomRoleSpawnChances[CustomRoles.AssassinAndMarin]);
            Marin.HasTasks = CustomOption.Create(Id + 12, Color.white, "MarinHasTasks", false, Options.CustomRoleSpawnChances[CustomRoles.AssassinAndMarin]);
            Marin.CanUseVent = CustomOption.Create(Id + 13, Color.white, "MarinCanUseVent", false, Options.CustomRoleSpawnChances[CustomRoles.AssassinAndMarin]);
        }
        public static bool IsEnable()
        {
            return CustomRoles.AssassinAndMarin.IsEnable();
        }
        public static void Init()
        {
            Assassin.Init();
            Marin.Init();
        }
        public static void IsAssassinMeetingToggle()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.IsAssassinMeeting, Hazel.SendOption.Reliable, -1);
            writer.Write(Assassin.IsAssassinMeeting);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void MarinSelectedInAssassinMeeting()
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MarinSelectedInAssassinMeeting, Hazel.SendOption.Reliable, -1);
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
        public static CustomOption HasWatcherAbility;
        public static byte TriggerPlayerId;
        public static bool IsAssassinMeeting;
        public static bool FinishAssassinMeetingTrigger;
        public static byte AssassinTargetId;
        public static CustomRoles TargetRole = CustomRoles.Crewmate;
        public static string TriggerPlayerName = "";
        public static string ExileText = "";
        public static void Init()
        {
            playerIdList = new();
            IsAssassinMeeting = false;
            FinishAssassinMeetingTrigger = false;
            TriggerPlayerId = 0x73; //各所でRPC送信必須
            AssassinTargetId = 0x74; //各所でRPC送信必須
            TargetRole = CustomRoles.Crewmate;
            TriggerPlayerName = ""; //各所でRPC送信必須
            ExileText = "";
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
            TriggerPlayerId = assassin.PlayerId;
            Utils.NotifyRoles();
            Logger.Info("アサシン会議開始", "Special Phase");
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                Main.AllPlayerSpeed[pc.PlayerId] = 0.00001f;
                new LateTask(() =>
                {
                    if (AmongUsClient.Instance.AmHost && !HeldMeeting)
                    {
                        TriggerPlayerName = assassin.Data.PlayerName;
                        IsAssassinMeeting = true;
                        AssassinAndMarin.IsAssassinMeetingToggle();

                        MeetingRoomManager.Instance.AssignSelf(assassin, null);
                        DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(assassin);
                        assassin.RpcStartMeeting(null);
                        HeldMeeting = true;
                    }
                }, PlayerControl.GameOptions.MapId == 4 && !BeKilled ? 7f : 0f, "StartAssassinMeeting"); //Airshipなら7sの遅延を追加
            }
        }
        public static void SendTriggerPlayerInfo(byte playerId)
        {
            TriggerPlayerId = playerId;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShareTriggerAssassin, Hazel.SendOption.Reliable, -1);
            writer.Write(TriggerPlayerId);
            writer.Write(TriggerPlayerName);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
    public static class Marin
    {
        public static string ColorString => Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Marin), Utils.GetRoleName(CustomRoles.Marin));
        public static CustomOption HasWatcherAbility;
        public static CustomOption HasTasks;
        public static CustomOption CanUseVent;
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