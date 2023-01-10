using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using UnityEngine;

namespace TownOfHost.Roles;

public class SerialKiller : Impostor
{
    private bool paused = true;
    // TODO: move to shapeshift button or possible hns meter
    public Cooldown DeathTimer;
    private float killCooldown;
    private HideAndSeekTimerBar timerBar;

    [DynElement(UI.Counter)]
    private string CustomCooldown() => DeathTimer.IsReady() ? "" : Color.white.Colorize(DeathTimer + "s");

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
    }

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        bool success = base.TryKill(target);
        if (success) DeathTimer.Start();
        return success;
    }

    [RoleAction(RoleActionType.FixedUpdate)]
    private void CheckForSuicide()
    {
        if (!paused && DeathTimer.IsReady() && !MyPlayer.Data.IsDead)
            MyPlayer.RpcMurderPlayer(MyPlayer);
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void SetupSuicideTimer()
    {
        paused = false;
        DeathTimer.Start();
    }

    [RoleAction(RoleActionType.RoundEnd)]
    private void StopDeathTimer() => paused = true;

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Kill Cooldown")
                .Bind(v => killCooldown = (float)v)
                .AddFloatRangeValues(0, 90, 0.5f, 30, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Time Until Suicide")
                .Bind(v => DeathTimer.Duration = (float)v)
                .AddFloatRangeValues(5, 120, 2.5f, 30, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).OptionOverride(Override.KillCooldown, () => killCooldown);

}