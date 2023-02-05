using TownOfHost.Factions;
using TownOfHost.Options;
using UnityEngine;
using VentLib.Options;
using TownOfHost.Roles.Internals.Attributes;

namespace TownOfHost.Roles;

// Inherits from crewmate because crewmate has task setup
public class Terrorist : Crewmate
{
    private bool canWinBySuicide;

    [RoleAction(RoleActionType.MyDeath)]
    private void OnTerroristDeath() => TerroristWinCheck();


    //   [RoleAction(RoleActionType.SelfExiled)]
    //  private void OnTerroristExiled() => TerroristWinCheck();

    private void TerroristWinCheck()
    {
        if (this.HasAllTasksDone)
        {
            // I know we are going to redo death reasons but I will still like it here for reasons.
            /*if (canWinBySuicide || TOHPlugin.PlayerStates[MyPlayer.PlayerId].deathReason != (PlayerStateOLD.DeathReason.Suicide | PlayerStateOLD.DeathReason.FollowingSuicide))
            {
                // TERRORIST WIN
            }*/
        }
        //OldRPC.TerroristWin(MyPlayer.PlayerId);
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub
                .Name("Can Win By Suicide")
                .Bind(v => canWinBySuicide = (bool)v)
                .AddOnOffValues(false).Build())
            .SubOption(sub => sub
                .Name("Override Terrorist's Tasks")
                .Bind(v => HasOverridenTasks = (bool)v)
                .ShowSubOptionPredicate(v => (bool)v)
                .AddOnOffValues(false)
                .SubOption(sub2 => sub2
                    .Name("Allow Common Tasks")
                    .Bind(v => HasCommonTasks = (bool)v)
                    .AddOnOffValues()
                    .Build())
                .SubOption(sub2 => sub2
                    .Name("Terrorist Long Tasks")
                    .Bind(v => LongTasks = (int)v)
                    .AddIntRange(0, 20, 1, 5)
                    .Build())
                .SubOption(sub2 => sub2
                    .Name("Terrorist Short Tasks")
                    .Bind(v => ShortTasks = (int)v)
                    .AddIntRange(1, 20, 1, 5)
                    .Build())
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(Color.green).Factions(Faction.Solo);
}