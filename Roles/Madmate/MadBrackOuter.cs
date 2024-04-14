using AmongUs.GameOptions;
using Hazel;
using InnerNet;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Madmate;
public sealed class MadBrackOuter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadBrackOuter),
            player => new MadBrackOuter(player),
            CustomRoles.MadBrackOuter,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            5500,
            SetupOptionItem,
            "マッドブラックアウター",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadBrackOuter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public static void SetupOptionItem()
    {
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 10, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!AmongUsClient.Instance.AmHost) return true;

        MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, Player.GetClientId());
        SabotageFixWriter.Write((byte)SystemTypes.Electrical);
        SabotageFixWriter.WriteNetObject(Player);
        AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);

        foreach (var target in Main.AllPlayerControls)
        {
            if (target == Player || target.Data.Disconnected) continue;
            SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.Reliable, target.GetClientId());
            SabotageFixWriter.Write((byte)SystemTypes.Electrical);
            SabotageFixWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
        }
        return true;
    }
}
