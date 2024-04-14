using System;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;
using static TownOfHostForE.Translator;
using static TownOfHostForE.Options;

namespace TownOfHostForE.Roles.Impostor;
public sealed class EvilNekomata : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(EvilNekomata),
            player => new EvilNekomata(player),
            CustomRoles.EvilNekomata,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            20000,
            null,
            "イビル猫又"
        );
    public EvilNekomata(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
}