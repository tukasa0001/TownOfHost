//using AmongUs.GameOptions;

//using TownOfHostForE.Roles.Core;
//using TownOfHostForE.Roles.Core.Interfaces;

//namespace TownOfHostForE.Roles.Impostor;
//public sealed class NormalImpostor : RoleBase, IImpostor
//{
//    public static readonly SimpleRoleInfo RoleInfo =
//         SimpleRoleInfo.Create(
//            typeof(NormalImpostor),
//            player => new NormalImpostor(player),
//            CustomRoles.NormalImpostor,
//            () => RoleTypes.Impostor,
//            CustomRoleTypes.Impostor,
//            1000,
//            null,
//            "インポスター"
//        );
//    public NormalImpostor(PlayerControl player)
//    : base(
//        RoleInfo,
//        player
//    )
//    {
//    }
//}