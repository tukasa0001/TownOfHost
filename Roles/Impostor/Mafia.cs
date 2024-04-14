using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Impostor;
public sealed class Mafia : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mafia),
            player => new Mafia(player),
            CustomRoles.Mafia,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            10700,
            SetUpOptionItem,
            "マフィア"
        );
    public Mafia(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        CanKillImpostorCount = OptionCanKillImpostorCount.GetInt();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionCanKillImpostorCount;
    enum OptionName
    {
        MafiaCanKillImpostorCount
    }
    private static float KillCooldown;
    int CanKillImpostorCount;
    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanKillImpostorCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.MafiaCanKillImpostorCount, new(1, 2, 1), 1, false)
            .SetValueFormat(OptionFormat.Players);
    }

    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseKillButton()
    {
        if (PlayerState.AllPlayerStates == null) return false;
        //マフィアを除いた生きているインポスターの人数  Number of Living Impostors excluding mafia
        int livingImpostorsNum = 0;
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            var role = pc.GetCustomRole();
            if (role != CustomRoles.Mafia && role.IsImpostor()) livingImpostorsNum++;
        }

        return livingImpostorsNum < CanKillImpostorCount;
    }
}