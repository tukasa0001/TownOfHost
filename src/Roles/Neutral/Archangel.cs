using TownOfHost.Extensions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using UnityEngine;
using TownOfHost.Options;
using System.Linq;

namespace TownOfHost.Roles;

public class Archangel : CustomRole
{
    private Cooldown protectTimer;
    private float protectCD;
    private float protectDur;
    public bool TargetKnowsGaExists;
    public bool GaKnowsTargetRole;
    public int roleChangeWhenTargetDies;
    public PlayerControl? target;

    [DynElement(UI.Counter)]
    private string CustomCooldown() => protectTimer.ToString() == "0" ? "" : Color.white.Colorize(protectTimer + "s");
    [DynElement(UI.Misc)]
    private string TargetDisplay() => target == null ? "" : RoleColor.Colorize("Target: ") + Color.white.Colorize(target.GetRawName());

    [RoleAction(RoleActionType.RoundStart)]
    public void Restart(bool gameStart)
    {
        if (gameStart)
        {
            target = Game.GetAllPlayers().Where(p =>
            {
                if (p.PlayerId == MyPlayer.PlayerId) return false;
                if (p.GetCustomRole() == CustomRoleManager.Static.Archangel) return false;
                return true;
            }).ToList().GetRandom();
            protectTimer.Duration = 10f;
            protectTimer.Start();
        }
        else
        {
            protectTimer.Duration = protectCD;
            protectTimer.Start();
        }
    }

    [RoleAction(RoleActionType.AnyDeath)]
    public void Death(PlayerControl killed, PlayerControl killer)
    {
        if (roleChangeWhenTargetDies == 0 || target == null || target.PlayerId != killed.PlayerId) return;
        switch ((GARoleChange)roleChangeWhenTargetDies)
        {
            case GARoleChange.Jester:
                MyPlayer.RpcSetCustomRole(CustomRoleManager.Static.Jester);
                break;
            case GARoleChange.Opportunist:
                MyPlayer.RpcSetCustomRole(CustomRoleManager.Static.Opportunist);
                break;
            case GARoleChange.SchrodingerCat:
                MyPlayer.RpcSetCustomRole(CustomRoleManager.Static.SchrodingerCat);
                break;
            case GARoleChange.Crewmate:
                MyPlayer.RpcSetCustomRole(CustomRoleManager.Static.Crewmate);
                break;
            case GARoleChange.None:
            default:
                break;
        }

        target = null;
        MyPlayer.GetDynamicName().Render();
    }

    [RoleAction(RoleActionType.OnPet)]
    public void OnPet()
    {
        if (protectTimer.NotReady()) return;
        protectTimer.Duration = protectCD + protectDur;
        protectTimer.Start();
    }

    // Imma have to ask Tealeaf about this :sweat_smile:
    public bool TargetCanBeKilled() => protectTimer.TimeRemaining() <= (protectCD + protectDur) - protectDur || protectTimer.IsReady();
    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .AddSubOption(sub => sub
                .Name("Protect Duration")
                .Bind(v => protectDur = (float)v)
                .AddFloatRangeValues(2.5f, 180f, 2.5f, 11, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Protect Cooldown")
                .Bind(v => protectCD = (float)v)
                .AddFloatRangeValues(2.5f, 180f, 2.5f, 5, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Target Knows They have A GA")
                .Bind(v => TargetKnowsGaExists = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(sub => sub
                .Name("GA Knows Target Role")
                .Bind(v => GaKnowsTargetRole = (bool)v)
                .AddOnOffValues()
                .Build())
            .AddSubOption(sub => sub
                .Name("Role Change When Target Dies")
                .Bind(v => roleChangeWhenTargetDies = (int)v)
                .AddValue(v => v.Text("Jester").Value(1).Color(new Color(0.93f, 0.38f, 0.65f)).Build())
                .AddValue(v => v.Text("Opportunist").Value(2).Color(Color.green).Build())
                .AddValue(v => v.Text("Schrodinger's Cat").Value(3).Color(Color.black).Build())
                .AddValue(v => v.Text("Crewmate").Value(4).Color(new Color(0.71f, 0.94f, 1f)).Build())
                .AddValue(v => v.Text("Off").Value(0).Color(Color.red).Build())
                .Build());
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
            .SpecialType(SpecialType.Neutral)
            .RoleColor(Utils.ConvertHexToColor("#B3FFFF"));
    }

    private enum GARoleChange
    {
        None,
        Jester,
        Opportunist,
        SchrodingerCat,
        Crewmate
    }
}