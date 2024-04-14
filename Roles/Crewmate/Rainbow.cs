using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;
using UnityEngine;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class Rainbow : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Rainbow),
            player => new Rainbow(player),
            CustomRoles.Rainbow,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            40600,
            SetupOptionItem,
            "にじいろスター",
            "#ffff00"
        );
    public Rainbow(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        DontSeeTaskTurn = OptionDontSeeTaskTurn.GetBool();
    }

    private static OptionItem OptionDontSeeTaskTurn;
    enum OptionName
    {
        RainbowDontSeeTaskTurn,
    }

    private static bool DontSeeTaskTurn;

    float colorchange;

    private static void SetupOptionItem()
    {
        OptionDontSeeTaskTurn = BooleanOptionItem.Create(RoleInfo, 10, OptionName.RainbowDontSeeTaskTurn, false, false);
    }

    public override void Add()
    {
        int chance = IRandom.Instance.Next(0, 17);
        colorchange = chance;
        Player.SetNamePlate("nameplate_flagRainbow");
        CustomRpcSender.Create(name: $"RpcSetNamePlate({Player.Data.PlayerName})").AutoStartRpc(Player.NetId, (byte)RpcCalls.SetNamePlateStr)
            .Write("nameplate_flagRainbow")
            .EndRpc();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask || !player.IsAlive()) return;

        colorchange %= 18;
        if (colorchange is >= 0 and < 1) player.RpcSetColor(8);
        else if (colorchange is >= 1 and < 2) player.RpcSetColor(1);
        else if (colorchange is >= 2 and < 3) player.RpcSetColor(10);
        else if (colorchange is >= 3 and < 4) player.RpcSetColor(2);
        else if (colorchange is >= 4 and < 5) player.RpcSetColor(11);
        else if (colorchange is >= 5 and < 6) player.RpcSetColor(14);
        else if (colorchange is >= 6 and < 7) player.RpcSetColor(5);
        else if (colorchange is >= 7 and < 8) player.RpcSetColor(4);
        else if (colorchange is >= 8 and < 9) player.RpcSetColor(17);
        else if (colorchange is >= 9 and < 10) player.RpcSetColor(0);
        else if (colorchange is >= 10 and < 11) player.RpcSetColor(3);
        else if (colorchange is >= 11 and < 12) player.RpcSetColor(13);
        else if (colorchange is >= 12 and < 13) player.RpcSetColor(7);
        else if (colorchange is >= 13 and < 14) player.RpcSetColor(15);
        else if (colorchange is >= 14 and < 15) player.RpcSetColor(6);
        else if (colorchange is >= 15 and < 16) player.RpcSetColor(12);
        else if (colorchange is >= 16 and < 17) player.RpcSetColor(9);
        else if (colorchange is >= 17 and < 18) player.RpcSetColor(16);
        colorchange += Time.fixedDeltaTime;
    }

    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, bool isMeeting ,ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (!(!isMeeting && DontSeeTaskTurn)) enabled = true;
        if (!isMeeting) roleText = Translator.GetString("RainbowResize");
    }
}