using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using TownOfHost.Roles.Internals.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles;

public partial class SerialKiller : Impostor, IModdable
{
    private bool paused = true;
    public Cooldown DeathTimer;
    private float killCooldown;

    [DynElement(UI.Counter)]
    private string CustomCooldown() => DeathTimer.IsReady() ? "" : Color.white.Colorize(DeathTimer + "s");

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