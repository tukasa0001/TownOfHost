using TownOfHost.Extensions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.ReduxOptions;
using UnityEngine;

namespace TownOfHost.Roles;

public class SerialKiller: Impostor
{
    private bool paused = true;
    // TODO: move to shapeshift button or possible hns meter
    private Cooldown deathTimer;
    private float killCooldown;

    [DynElement(UI.Counter)]
    private string CustomCooldown() => deathTimer.ToString() == "0" ? "" : Color.white.Colorize(deathTimer + "s");

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        bool success = base.TryKill(target);
        if (success) deathTimer.Start();
        return success;
    }

    [RoleAction(RoleActionType.FixedUpdate)]
    private void CheckForSuicide()
    {
        Logger.Msg($"SerialKiller {MyPlayer.GetDynamicName().RawName} Commiting Suicide", "SKSuicide");
        if (!paused && deathTimer.IsReady() && !MyPlayer.Data.IsDead)
            MyPlayer.RpcMurderPlayer(MyPlayer);
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void SetupSuicideTimer()
    {
        paused = false;
        deathTimer.Start();
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
                .Bind(v => deathTimer.Duration = (float)v)
                .AddFloatRangeValues(5, 120, 2.5f, 30, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).OptionOverride(Override.KillCooldown, () => killCooldown);

}