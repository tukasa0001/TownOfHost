using TownOfHost.Options;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

public class Phantom : Crewmate
{
    private int phantomClickAmt;
    private int phantomAlertAmt;
    public bool CanKill;
    public bool IsAlerted;
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
        .RoleColor("#662962")
        .SpecialType(SpecialType.Neutral);
    }

    [RoleAction(RoleActionType.RoundStart)]
    public void Reset()
    {
        CanKill = false;
        IsAlerted = false;
    }

    // I ASSUME TO MAKE IT NOT KILL THEM YOU GOT TO PUT IT IN EVERY ROLE FILE BUT IM TOO LAZY 4 THAT
    [RoleAction(RoleActionType.MyDeath)]
    public void PhantomDeath()
    {
        if (!CanKill)
        {
            // TODO: make it do something lol
        }
    }

    [RoleAction(RoleActionType.TaskComplete)]
    public void TaskComplete()
    {
        var taskState = TOHPlugin.PlayerStates?[MyPlayer.PlayerId].GetTaskState();
        if (taskState.CompletedTasksCount == taskState.AllTasksCount)
        {
            // PHANTOM WIN
        }
        else if (taskState.RemainingTasksCount <= phantomClickAmt && !CanKill)
        {
            CanKill = true;
        }
        else if (taskState.RemainingTasksCount <= phantomAlertAmt && !IsAlerted)
        {
            IsAlerted = true;
        }
    }

    public override bool CanBeKilled() => CanKill;

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
         base.RegisterOptions(optionStream)
             .Tab(DefaultTabs.NeutralTab)
             .AddSubOption(opt =>
                opt.Name("Tasks Remaining for Phantom Click")
                .BindInt(v => phantomClickAmt = v)
                .AddIntRangeValues(1, 10, 1)
                .Build())
            .AddSubOption(opt =>
                opt.Name("Tasks Remaining for Phantom Alert")
                .BindInt(v => phantomAlertAmt = v)
                .AddIntRangeValues(1, 5, 1)
                .Build());
}