using System.Collections.Generic;
using AmongUs.GameOptions;


using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Neutral;

public sealed class PlatonicLover : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(PlatonicLover),
            player => new PlatonicLover(player),
            CustomRoles.PlatonicLover,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            60400,
            SetupOptionItem,
            "純愛者",
            "#ff6be4",
            true,
            countType: CountTypes.Crew,
            assignInfo: new RoleAssignInfo(CustomRoles.PlatonicLover, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public PlatonicLover(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AddWin = OptionAddWin.GetBool();
    }
    public static OptionItem OptionAddWin;
    enum OptionName
    {
        LoversAddWin,
    }
    public bool isMadeLover;
    public static bool AddWin;

    private static void SetupOptionItem()
    {
        OptionAddWin = BooleanOptionItem.Create(RoleInfo, 10, OptionName.LoversAddWin, false, false);
    }

    public override void Add()
    {
        var playerId = Player.PlayerId;
        isMadeLover = false;
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? 0.1f : 0f;
    public bool CanUseKillButton() => Player.IsAlive() && !isMadeLover;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        isMadeLover = true;
        info.DoKill = false;
        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        Logger.Info($"{killer.GetNameWithRole()} : 恋人を作った", "PlatonicLover");

        //Main.LoversPlayers.Clear();
        //Main.isLoversDead = false;
        killer.RpcSetCustomRole(CustomRoles.Lovers);
        target.RpcSetCustomRole(CustomRoles.Lovers);

        List<byte> playerIds = new ();
        playerIds.Add(killer.PlayerId);
        Main.isLoversLeaders.Add(killer.PlayerId);

        if (CheckOtherLovers(target.PlayerId,out byte teamLeaderId))
        {
            Main.LoversPlayersV2[teamLeaderId].Remove(target.PlayerId);
        }

        playerIds.Add(target.PlayerId);
        Main.LoversPlayersV2.Add(killer.PlayerId,playerIds);
        Main.isLoversDeadV2.Add(killer.PlayerId,false);

        RPC.SyncLoversPlayers();

        Utils.NotifyRoles();
    }

    private bool CheckOtherLovers(byte targetId,out byte leader)
    {
        leader = byte.MaxValue;

        foreach (var list in Main.LoversPlayersV2)
        {
            if (list.Value.Contains(targetId))
            {
                leader = list.Key;
                return true;
            }
        }

        return false;
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("PlatonicLoverButtonText");
        return true;
    }
}
