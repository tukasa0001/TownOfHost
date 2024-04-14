using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Roles.Impostor;
public sealed class StrayWolf : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(StrayWolf),
            player => new StrayWolf(player),
            CustomRoles.StrayWolf,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            20900,
            SetupOptionItem,
            "はぐれ狼",
            isDesyncImpostor:true,
            assignInfo: new RoleAssignInfo(CustomRoles.StrayWolf, CustomRoleTypes.Impostor)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public StrayWolf(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        useGuard = new();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionKillByImpostor;

    public static readonly string[] KillModes =
{
            "StrayWolfKillMode.Always", "StrayWolfKillMode.AfterCheck", "StrayWolfKillMode.None"
        };
    public static KillMode GetKillModes() => (KillMode)OptionKillByImpostor.GetValue();
    public enum KillMode
    {
        Always,
        AfterCheck,
        None
    }

    enum OptionName
    {
        StrayWolfKillByImpostor,
    }
    private static float KillCooldown;
    List<byte> useGuard = new();

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillByImpostor = StringOptionItem.Create(RoleInfo, 11, OptionName.StrayWolfKillByImpostor, KillModes, 0, false);
    }
    public float CalculateKillCooldown() => KillCooldown;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide || !info.CanKill) return;

        (var killer, var target) = info.AttemptTuple;
        if (!target.Is(CustomRoleTypes.Impostor)) return;   //インポスターじゃないならそのままtrueで返す
        if (GetKillModes() == KillMode.Always) return;  //ガードしないのでそのままtrueで返す
        if (useGuard.Contains(target.PlayerId))         //視認後
        {
            if (GetKillModes() == KillMode.None)
            {
                info.DoKill = false; return;    //キルが起こらない
            }
            else   //視認後
            {
                return;     //ガードしないのでそのままtrueで返す
            }
        }

        // ガード
        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        NameColorManager.Add(killer.PlayerId, target.PlayerId);
        NameColorManager.Add(target.PlayerId, killer.PlayerId);

        useGuard.Add(target.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()} : インポスター({target.GetNameWithRole()})からのキルガード", "StrayWolf");
        Utils.NotifyRoles();

        // 相手は斬られない
        info.CanKill = false;
        return;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;
        if (!killer.Is(CustomRoleTypes.Impostor)) return true;  //インポスターじゃないならそのままtrueで返す
        if (GetKillModes() == KillMode.Always) return true;  //ガードしないのでそのままtrueで返す
        if (useGuard.Contains(killer.PlayerId))         //視認後
        {
            if (GetKillModes() == KillMode.None)
            {
                info.DoKill = false; return false;    //キルが起こらない
            }
            else   //視認後
            {
                return true;     //ガードしないのでそのままtrueで返す
            }
        }

        // ガード
        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        NameColorManager.Add(killer.PlayerId, target.PlayerId);
        NameColorManager.Add(target.PlayerId, killer.PlayerId);

        useGuard.Add(killer.PlayerId);
        Logger.Info($"{target.GetNameWithRole()} : インポスター({killer.GetNameWithRole()})へのキルガード", "StrayWolf");
        Utils.NotifyRoles();

        // 自身は斬られない
        info.CanKill = false;
        return true;
    }
}