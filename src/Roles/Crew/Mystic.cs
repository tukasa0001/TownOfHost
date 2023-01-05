using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;
using TownOfHost.RPC;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Patches.Systems;
using TownOfHost.Roles;

namespace TownOfHost.Roles;

public class Mystic : Crewmate
{
    private float flashDuration;
    private bool sendAudioAlert;

    [RoleAction(RoleActionType.AnyDeath)]
    private void MysticAnyDeath()
    {
        if (MyPlayer.Data.IsDead) return;
        if (MyPlayer.IsModClient()) Utils.FlashColor(new Color(1f, 0f, 0f, 0.5f));
        else
        {
            GameOptionOverride[] overrides = { new(Override.CrewLightMod, 0f) };
            SyncOptions(overrides);
        }

        bool didReactorAlert = false;
        if (sendAudioAlert && SabotagePatch.CurrentSabotage is not SabotageType.Reactor)
        {
            RoleUtils.PlayReactorsForPlayer(MyPlayer);
            didReactorAlert = true;
        }

        DTask.Schedule(() => MysticRevertAlert(didReactorAlert), flashDuration);

    }

    private void MysticRevertAlert(bool didReactorAlert)
    {
        SyncOptions();
        if (!didReactorAlert) return;
        RoleUtils.EndReactorsForPlayer(MyPlayer);
    }


    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Flash Duration")
                .Bind(v => flashDuration = (float)v)
                .AddFloatRangeValues(0, 1.5f, 0.1f, 4, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Send Audio Alert")
                .BindBool(v => sendAudioAlert = v)
                .AddOnOffValues()
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.3f, 0.6f, 0.9f));
}