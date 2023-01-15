using TownOfHost.Extensions;
using TownOfHost.Options;
using UnityEngine;

namespace TownOfHost.Roles;

public class Observer: Crewmate
{
    private bool slowlyGainsVision;
    private float visionGain;
    private bool overrideStartingVision;
    private float startingVision;
    private float totalVisionMod;

    private float currentVisionMod;

    // int because I'm lazy... if 0 then no immunity if 1 immunity but not active if 2 immunity and currently active
    private int sabotageImmunity;

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        currentVisionMod = overrideStartingVision ? startingVision : DesyncOptions.OriginalHostOptions.AsNormalOptions().CrewLightMod;
    }

    protected override void OnTaskComplete()
    {
        if (slowlyGainsVision)
            currentVisionMod = Mathf.Clamp(currentVisionMod + visionGain, 0, totalVisionMod);
        if (HasAllTasksDone)
            currentVisionMod = totalVisionMod;

        SyncOptions();
    }

    [RoleAction(RoleActionType.SabotageStarted)]
    private void IgnoreSabotageEffect()
    {
        if (sabotageImmunity != 1 || !HasAllTasksDone) return;
        sabotageImmunity = 2;
        currentVisionMod *= 5;
        SyncOptions();
    }

    [RoleAction(RoleActionType.SabotageFixed)]
    private void ClearIgnoreSabotageEffect()
    {
        if (sabotageImmunity != 2 || !HasAllTasksDone) return;
        sabotageImmunity = 1;
        currentVisionMod /= 5;
        SyncOptions();
    }


    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Slowly Gains Vision")
                .Bind(v => slowlyGainsVision = (bool)v)
                .AddOnOffValues(false)
                .ShowSubOptionsWhen(v => (bool)v)
                .AddSubOption(sub2 => sub2
                    .Name("Vision Gain On Task Complete")
                    .Bind(v => visionGain = (float)v)
                    .AddFloatRangeValues(0.05f, 1, 0.05f, 2, "x").Build())
                .Build())
            .AddSubOption(sub => sub
                .Name("Override Starting Vision")
                .Bind(v => overrideStartingVision = (bool)v)
                .ShowSubOptionsWhen(v => (bool)v)
                .AddOnOffValues(false)
                .AddSubOption(sub2 => sub2
                    .Name("Starting Vision Modifier")
                    .Bind(v => startingVision = (float)v)
                    .AddFloatRangeValues(0.25f, 2, 0.25f, 0, "x").Build())
                .Build())
            .AddSubOption(sub => sub
                .Name("Finished Tasks Vision")
                .Bind(v => totalVisionMod = (float)v)
                .AddFloatRangeValues(0.25f, 5f, 0.25f, 8, "x").Build())
            .AddSubOption(sub => sub
                .Name("Lights Immunity If Tasks Finished")
                .Bind(v => sabotageImmunity = ((bool)v) ? 1 : 0)
                .AddOnOffValues().Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .RoleColor("#eee5be")
            .OptionOverride(Override.CrewLightMod, () => currentVisionMod);
}