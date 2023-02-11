using System;
using TownOfHost.API;
using TownOfHost.Extensions;
using VentLib.Options;
using TownOfHost.Roles;
using TownOfHost.Roles.Internals.Attributes;
using static TownOfHost.Roles.SerialKiller;

namespace TownOfHost.Gamemodes.Colorwars;

public class CwPainter: SerialKillerModifier
{
    internal CwPainter(SerialKiller role) : base(role) { }
    public override void OnLink() { }

    [ModifiedAction(RoleActionType.FixedUpdate)]
    public void FixedUpdate()
    {
        double timeElapsed = ((DateTime.Now - Game.StartTime).TotalSeconds);
        if (timeElapsed < ColorwarsGamemode.GracePeriod) {
            this.DeathTimer.Start();
            return;
        }
        CheckForSuicide();
    }

    [ModifiedAction(RoleActionType.AttemptKill)]
    public void ColorwarsKill(PlayerControl target)
    {
        double timeElapsed = ((DateTime.Now - Game.StartTime).TotalSeconds);
        if (timeElapsed < ColorwarsGamemode.GracePeriod) return;
        if (ColorwarsGamemode.ConvertColorMode) SplatoonConvert(target);
        else {
            MyPlayer.RpcMurderPlayer(target);
            this.DeathTimer.Start();
        }
    }

    private void SplatoonConvert(PlayerControl target)
    {
        int killerColor = MyPlayer.cosmetics.bodyMatProperties.ColorId;
        if (killerColor == target.cosmetics.bodyMatProperties.ColorId) return;

        target.RpcSetColor((byte)killerColor);
        MyPlayer.RpcGuardAndKill(target);
        this.DeathTimer.Start();
    }

    public override OptionBuilder HookOptions(OptionBuilder optionStream) =>
        base.HookOptions(optionStream)
            .Tab(ColorwarsGamemode.ColorwarsTab);

    public override AbstractBaseRole.RoleModifier HookModifier(AbstractBaseRole.RoleModifier modifier) =>
        base.HookModifier(modifier)
            .RoleName("Painter");
}



