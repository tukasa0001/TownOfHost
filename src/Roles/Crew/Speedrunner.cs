using TownOfHost.Extensions;
using TownOfHost.Options;
using VentLib.Options;
using TownOfHost.Roles.Internals;
using UnityEngine;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class Speedrunner : Crewmate
{
    private bool speedBoostOnTaskComplete;
    private float smallRewardBoost;
    private float smalRewardDuration;

    private int tasksUntilSpeedBoost;
    private bool slowlyAcquireSpeedBoost;
    private float speedBoostGain;

    private float totalSpeedBoost;

    private float currentSpeedBoost = DesyncOptions.OriginalHostOptions?.AsNormalOptions()?.CrewLightMod ?? 1;

    protected override void OnTaskComplete()
    {
        if (slowlyAcquireSpeedBoost)
            currentSpeedBoost = Mathf.Clamp(currentSpeedBoost + speedBoostGain, 0, totalSpeedBoost);
        if (TasksComplete >= tasksUntilSpeedBoost)
            currentSpeedBoost = totalSpeedBoost;
        if (speedBoostOnTaskComplete)
        {
            currentSpeedBoost += smallRewardBoost;
            Async.Schedule(() =>
            {
                currentSpeedBoost -= smallRewardBoost;
                this.SyncOptions();
            }, smalRewardDuration);
        }

        SyncOptions();
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Small Boost When Finishing a Task")
                .Bind(v => speedBoostOnTaskComplete = (bool)v)
                .ShowSubOptionPredicate(v => (bool)v)
                .AddOnOffValues(false)
                .SubOption(sub2 => sub2
                    .Name("Temporary Speed Boost")
                    .Bind(v => smallRewardBoost = (float)v)
                    .AddFloatRange(0.1f, 1f, 0.05f, 1, "x")
                    .Build())
                .SubOption(sub2 => sub2
                    .Name("Temporary Boost Duration")
                    .Bind(v => smalRewardDuration = (float)v)
                    .AddFloatRange(2f, 12f, 0.5f, 2, "s")
                    .Build())
                .Build())
            .SubOption(sub => sub
                .Name("Tasks Until Speed Boost")
                .Bind(v => tasksUntilSpeedBoost = (int)v)
                .AddIntRange(1, 20, 1, 5)
                .Build())
            .SubOption(sub => sub
                .Name("Slowly Gain Speed Boost")
                .Bind(v => slowlyAcquireSpeedBoost = (bool)v)
                .ShowSubOptionPredicate(v => (bool)v)
                .AddOnOffValues(false)
                .SubOption(sub2 => sub2
                    .Name("Permanent Gain")
                    .Bind(v => speedBoostGain = (float)v)
                    .AddFloatRange(0.1f, 1f, 0.1f, 1, "x")
                    .Build())
                .Build())
            .SubOption(sub => sub
                .Name("Final Speed Boost")
                .Bind(v => totalSpeedBoost = (float)v)
                .AddFloatRange(0.5f, 3f, 0.25f, 7, "x")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor("#00ffff").OptionOverride(Override.PlayerSpeedMod, () => currentSpeedBoost);
}

