using TOHTOR.GUI;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.Internals.Interfaces;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities;

namespace TOHTOR.Roles;

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

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.GeneralTab)
            .SubOption(sub => sub
                .Name("Kill Cooldown")
                .Bind(v => killCooldown = (float)v)
                .AddFloatRange(0, 90, 0.5f, 30, "s")
                .Build())
            .SubOption(sub => sub
                .Name("Time Until Suicide")
                .Bind(v => DeathTimer.Duration = (float)v)
                .AddFloatRange(5, 120, 2.5f, 30, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).OptionOverride(Override.KillCooldown, () => killCooldown);

}