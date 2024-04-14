using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Impostor;
public sealed class Ambitioner : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Ambitioner),
            player => new Ambitioner(player),
            CustomRoles.Ambitioner,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            20400,
            SetupOptionItem,
            "アンビショナー"
        );
    public Ambitioner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        KillCoolDecreaseRate = OptionKillCoolDecreaseRate.GetFloat();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionKillCoolDecreaseRate;
    enum OptionName
    {
        AmbitionerKillCoolDecreaseRate,
    }
    private static float KillCooldown;
    private static float KillCoolDecreaseRate;
    int KillCount = 0;

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillCoolDecreaseRate = FloatOptionItem.Create(RoleInfo, 11, OptionName.AmbitionerKillCoolDecreaseRate, new(0.1f, 1f, 0.1f), 0.5f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        //Main.AllPlayerKillCooldown[playerId] = KillCooldown;
        KillCount = 0;
    }
    public float CalculateKillCooldown() => KillCooldown;

    public override void OnStartMeeting()
    {
        KillCount = 0;
        Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            Logger.Info($"{killer?.Data?.PlayerName}:キルクール減少", "Ambitioner");
            KillCount++;
            Main.AllPlayerKillCooldown[killer.PlayerId] *= (float)System.Math.Pow(KillCoolDecreaseRate, KillCount);
            killer.SyncSettings();//キルクール処理を同期
        }
    }
}