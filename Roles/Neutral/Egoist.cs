using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral;
public sealed class Egoist : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Egoist),
            player => new Egoist(player),
            CustomRoles.Egoist,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Neutral,
            50600,
            SetupOptionItem,
            "#5600ff"
        );
    public Egoist(PlayerControl player)
    : base(
        RoleInfo,
        player,
        countType: CountTypes.Impostor
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        CanCreateMadmate = OptionCanCreateMadmate.GetBool();
    }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionCanCreateMadmate;
    enum OptionName
    {
        KillCooldown,
        CanCreateMadmate
    }

    private static float KillCooldown;
    public static bool CanCreateMadmate;

    public static List<PlayerControl> Egoists = new(3);
    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanCreateMadmate = BooleanOptionItem.Create(RoleInfo, 11, OptionName.CanCreateMadmate, false, false);
    }
    public override void Add()
    {
        foreach (var impostor in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
        {
            NameColorManager.Add(impostor.PlayerId, Player.PlayerId);
        }
        Egoists.Add(Player);
    }
    public override void OnDestroy()
    {
        Egoists.Clear();
    }
    public override float SetKillCooldown() => KillCooldown;

    public static bool CheckWin()
    {
        var impostorsDead = !Main.AllAlivePlayerControls.Any(p => p.Is(RoleTypes.Impostor));
        var aliveEgoists = Egoists.Any(p => p.IsAlive());

        if (impostorsDead && aliveEgoists) //インポスター全滅でエゴイストが生存
        {
            Win();
            return true;
        }

        return false;
    }
    private static void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.EgoSchrodingerCat);
    }
}