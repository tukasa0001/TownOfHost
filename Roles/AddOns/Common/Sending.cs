using System.Collections.Generic;
using UnityEngine;
using TownOfHostForE.Roles.Core;

using static TownOfHostForE.Options;
using static TownOfHostForE.Translator;
using static TownOfHostForE.Utils;

using TownOfHostForE.Attributes;
using TownOfHostForE.Roles.Crewmate;
using Hazel;

namespace TownOfHostForE.Roles.AddOns.Common;

public static class Sending
{
    private static readonly int Id = 80600;
    private static Color RoleColor = GetRoleColor(CustomRoles.Sending);
    public static string SubRoleMark = ColorString(RoleColor, "Se");
    private static List<byte> playerIdList = new();
    private static OptionItem OptionSeeingCrew;

    private static PlayerControl ExiledPlayer = null;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Sending);
        OptionSeeingCrew = BooleanOptionItem.Create(Id + 10, "AddOptionSeeingCrew", true, TabGroup.Addons, false)
            .SetGameMode(CustomGameMode.Standard);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
        ExiledPlayer = null;
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static void OnExileWrapUp(PlayerControl exiled)
    {
        SendRPC(exiled.PlayerId);
        ExiledPlayer = exiled;
    }
    public static void OnStartMeeting()
    {
        ExiledPlayer = null;
    }
    public static string RealNameChange(string Name,PlayerControl pc)
    {
        if (ExiledPlayer == null) return Name;

        var ExiledPlayerName = ExiledPlayer.Data.PlayerName;
        if ((pc.Is(CustomRoles.Sending) && OptionSeeingCrew.GetBool()) ||
            (pc.Is(CustomRoles.SeeingOff) && SeeingOff.optionSeeingCrew))
        {
            if (ExiledPlayer.Is(CustomRoleTypes.Crewmate))
                return ColorString(RoleColor, string.Format(GetString("isCrewmate"), ExiledPlayerName));
            else
                return ColorString(RoleColor, string.Format(GetString("isNotCrewmate"), ExiledPlayerName));
        }

        if (ExiledPlayer.Is(CustomRoleTypes.Impostor))
            return ColorString(RoleColor, string.Format(GetString("isImpostor"), ExiledPlayerName));
        else
            return ColorString(RoleColor, string.Format(GetString("isNotImpostor"), ExiledPlayerName));
    }
    private static void SendRPC(byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SendingSync, SendOption.Reliable, -1);
        writer.Write(targetId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte targetId = reader.ReadByte();
        ExiledPlayer = GetPlayerById(targetId);
    }

    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}