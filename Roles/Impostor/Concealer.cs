using Hazel;
using System;
using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

public static class Concealer
{
    private static readonly int Id = 903534;
    public static List<byte> playerIdList = new();
    public static OptionItem SkillCooldown;
    private static OptionItem SkillDuration;
    public static long HiddenTimeStamp;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Concealer);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "ConcealerSkillCooldown", new(1f, 180f, 1f), 30f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Concealer])
            .SetValueFormat(OptionFormat.Seconds);
        SkillDuration = FloatOptionItem.Create(Id + 12, "ConcealerSkillDuration", new(1f, 180f, 1f), 10f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Concealer])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        HiddenTimeStamp = 0;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetConcealerTimer, SendOption.Reliable, -1);
        writer.Write(HiddenTimeStamp);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        HiddenTimeStamp = reader.ReadUInt32();
        Camouflage.CheckCamouflage();
    }
    public static bool IsHidding => HiddenTimeStamp + SkillDuration.GetFloat() > Utils.GetTimeStamp(DateTime.Now);
    public static void OnShapeshift(PlayerControl pc, bool shapeshifting)
    {
        if (shapeshifting)
            HiddenTimeStamp = Utils.GetTimeStamp(DateTime.Now);
        SendRPC();
        Camouflage.CheckCamouflage();
    }
    public static void OnFixedUpdate()
    {
        if (IsEnable && !IsHidding && HiddenTimeStamp > 0)
        {
            HiddenTimeStamp = 0;
            SendRPC();
            Camouflage.CheckCamouflage();
        }
    }
    public static void OnReportDeadBody()
    {
        HiddenTimeStamp = 0;
        SendRPC();
        Camouflage.CheckCamouflage();
    }
}