using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

public sealed class Warlock : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Warlock),
            player => new Warlock(player),
            CustomRoles.Warlock,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1400,
            null,
            "wa"
        );
    public Warlock(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    public override void OnDestroy()
    {
        CursedPlayer = null;
    }

    PlayerControl CursedPlayer;
    bool IsCursed;
    bool Shapeshifting;
    public override void Add()
    {
        CursedPlayer = null;
        IsCursed = false;
        Shapeshifting = false;
    }
    public bool OverrideKillButtonText(out string text)
    {
        if (!Shapeshifting)
        {
            text = GetString("WarlockCurseButtonText");
            return true;
        }
        else
        {
            text = default;
            return false;
        }
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = IsCursed ? 1f : Options.DefaultKillCooldown;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        //自殺なら関係ない
        if (info.IsSuicide) return;

        var (killer, target) = info.AttemptTuple;
        if (!Shapeshifting)
        {//変身してない
            if (!IsCursed)
            {//まだ呪っていない
                IsCursed = true;
                CursedPlayer = target;
                //呪える相手は一人だけなのでキルボタン無効化
                killer.SetKillCooldown(255f);
                killer.RpcResetAbilityCooldown();
            }
            //どちらにしてもキルは無効
            info.DoKill = false;
        }
        //変身中は通常キル
    }
    public override void OnShapeshift(PlayerControl target)
    {
        Shapeshifting = !Is(target);

        if (!AmongUsClient.Instance.AmHost) return;

        if (Shapeshifting)
        {///変身時
            if (CursedPlayer != null && CursedPlayer.IsAlive())
            {//呪っていて対象がまだ生きていたら
                Vector2 cpPos = CursedPlayer.transform.position;
                Dictionary<PlayerControl, float> candidateList = new();
                float distance;
                foreach (PlayerControl candidatePC in Main.AllAlivePlayerControls)
                {
                    if (candidatePC != CursedPlayer)
                    {
                        distance = Vector2.Distance(cpPos, candidatePC.transform.position);
                        candidateList.Add(candidatePC, distance);
                        Logger.Info($"{candidatePC?.Data?.PlayerName}の位置{distance}", "Warlock");
                    }
                }
                var nearest = candidateList.OrderBy(c => c.Value).FirstOrDefault();
                var killTarget = nearest.Key;
                killTarget.SetRealKiller(Player);
                Logger.Info($"{killTarget.GetNameWithRole()}was killed", "Warlock");
                CursedPlayer.RpcMurderPlayerV2(killTarget);
                Player.SetKillCooldown();
                CursedPlayer = null;
            }
        }
        else
        {
            if (IsCursed)
            {
                //ShapeshifterCooldownを通常に戻す
                IsCursed = false;
                Player.SyncSettings();
                Player.RpcResetAbilityCooldown();
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        CursedPlayer = null;
        IsCursed = false;
    }
}